// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Helpers;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;
using TypeSymbolExtensions = Uno.Helpers.TypeSymbolExtensions;

namespace Uno
{
	/// <summary>
	/// Responsible for the code generation of immutable types.
	/// </summary>
	/// <remarks>
	/// The trigger for this generator is <see cref="GeneratedImmutableAttribute"/>.
	/// </remarks>
	public class ImmutableGenerator : SourceGenerator
	{
		private SourceGeneratorContext _context;
		private ISourceGeneratorLogger _logger;

		private INamedTypeSymbol _systemObject;
		private INamedTypeSymbol _immutableAttributeSymbol;
		private INamedTypeSymbol _generatedImmutableAttributeSymbol;
		private INamedTypeSymbol _immutableBuilderAttributeSymbol;
		private INamedTypeSymbol _immutableBuilderInterfaceSymbol;
		private INamedTypeSymbol _immutableAttributeCopyIgnoreAttributeSymbol;
		private INamedTypeSymbol _immutableGenerationOptionsAttributeSymbol;
		private INamedTypeSymbol _immutableTreatAsImmutableAttributeSymbol;

		private (bool generateOptionCode, bool treatArrayAsImmutable, bool generateEqualityByDefault, bool generateJsonNet, bool generateSystemTextJson) _generationOptions;
		private bool _generateOptionCode = true;
		private bool _generateJsonNet = true;
		private bool _generateSystemTextJson = true;

		private string _currentType;

		private Regex[] _copyIgnoreAttributeRegexes;

		private IReadOnlyList<ITypeSymbol> _knownAsImmutableTypes;

		/// <inheritdoc />
		public override void Execute(SourceGeneratorContext context)
		{
			_context = context;
			_logger = context.GetLogger();

			_systemObject = context.Compilation.GetTypeByMetadataName("System.Object");
			_immutableAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.ImmutableAttribute");
			_generatedImmutableAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.GeneratedImmutableAttribute");
			_immutableBuilderAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.ImmutableBuilderAttribute");
			_immutableBuilderInterfaceSymbol = context.Compilation.GetTypeByMetadataName("Uno.IImmutableBuilder`1");
			_immutableAttributeCopyIgnoreAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.ImmutableAttributeCopyIgnoreAttribute");
			_immutableGenerationOptionsAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.ImmutableGenerationOptionsAttribute");
			_immutableTreatAsImmutableAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.TreatAsImmutableAttribute");

			var generationData = EnumerateImmutableGeneratedEntities()
				.OrderBy(x => x.symbol.Name)
				.ToArray();

			var immutableEntitiesToGenerate = generationData.Select(x => x.Item1).ToArray();

			if (immutableEntitiesToGenerate.Length == 0)
			{
				return; // nothing to do
			}

			_knownAsImmutableTypes = EnumerateTreatAsImmutables();

			_copyIgnoreAttributeRegexes =
				ExtractCopyIgnoreAttributes(context.Compilation.Assembly)
					.Concat(new[] {new Regex(@"^Uno\.Immutable"), new Regex(@"^Uno\.Equality")})
					.ToArray();

			_generationOptions = ExtractGenerationOptions(context.Compilation.Assembly);

			_generateOptionCode = _generationOptions.generateOptionCode && context.Compilation.GetTypeByMetadataName("Uno.Option") != null;
			_generateJsonNet = _generationOptions.generateJsonNet && context.Compilation.GetTypeByMetadataName("Newtonsoft.Json.JsonConvert") != null;
			_generateSystemTextJson = _generationOptions.generateSystemTextJson && context.Compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonConverter") != null;

			foreach (var (type, moduleAttribute) in generationData)
			{
				var baseTypeInfo = GetTypeInfo(type, immutableEntitiesToGenerate);

				var generateEquality = GetShouldGenerateEquality(moduleAttribute);

				GenerateImmutable(type, baseTypeInfo, generateEquality);
			}
		}

		private IEnumerable<Regex> ExtractCopyIgnoreAttributes(ISymbol symbol)
		{
			return symbol.GetAttributes()
				.Where(a => a.AttributeClass.Equals(_immutableAttributeCopyIgnoreAttributeSymbol))
				.Select(a => new Regex(a.ConstructorArguments[0].Value.ToString()));
		}

		private (bool generateOptionCode, bool treatArrayAsImmutable, bool generateEqualityByDefault, bool generateJsonNet, bool generateSystemTextJson) ExtractGenerationOptions(IAssemblySymbol assembly)
		{
			var generateOptionCode = true;
			var treatArrayAsImmutable = false;
			var generateEqualityByDefault = true;
			var generateJsonNet = true;
			var generateSystemTextJson = true;

			var attribute = assembly
				.GetAttributes()
				.FirstOrDefault(a => a.AttributeClass.Equals(_immutableGenerationOptionsAttributeSymbol));

			if (attribute != null)
			{
				foreach (var argument in attribute.NamedArguments)
				{
					switch (argument.Key)
					{
						case nameof(ImmutableGenerationOptionsAttribute.GenerateOptionCode):
							generateOptionCode = (bool)argument.Value.Value;
							break;
						case nameof(ImmutableGenerationOptionsAttribute.TreatArrayAsImmutable):
							treatArrayAsImmutable = (bool) argument.Value.Value;
							break;
						case nameof(ImmutableGenerationOptionsAttribute.GenerateEqualityByDefault):
							generateEqualityByDefault = (bool)argument.Value.Value;
							break;
						case nameof(ImmutableGenerationOptionsAttribute.GenerateNewtownsoftJsonNetConverters):
							generateJsonNet = (bool)argument.Value.Value;
							break;
						case nameof(ImmutableGenerationOptionsAttribute.GenerateSystemTextJsonConverters):
							generateSystemTextJson = (bool)argument.Value.Value;
							break;
					}
				}
			}

			return (generateOptionCode, treatArrayAsImmutable, generateEqualityByDefault, generateJsonNet, generateSystemTextJson);
		}

		private bool GetShouldGenerateEquality(AttributeData attribute)
		{
			var shouldGenerateEquality = attribute.NamedArguments
				.Where(na => na.Key.Equals(nameof(GeneratedImmutableAttribute.GenerateEquality)))
				.Select(na => (bool?) na.Value.Value)
				.FirstOrDefault();

			return shouldGenerateEquality ?? _generationOptions.generateEqualityByDefault;
		}

		private (bool isBaseTypePresent, string baseType, string builderBaseType, bool isImmutablePresent, INamedTypeSymbol baseBuilderType) GetTypeInfo(
			INamedTypeSymbol type,
			INamedTypeSymbol[] immutableEntitiesToGenerate)
		{
			// Check if [Immutable] is present on the non-generated partial
			var isImmutablePresent = type.FindAttributeFlattened(_immutableAttributeSymbol) != null;

			var baseType = type.BaseType;
			if (baseType == null || baseType.Equals(_systemObject))
			{
				return (false, null, null, isImmutablePresent, null); // no base type
			}

			// Is Builder already compiled ? (in another project/assembly)
			var builderAttribute = baseType.FindAttribute(_immutableBuilderAttributeSymbol);
			if (builderAttribute != null)
			{
				var baseTypeBuilder = builderAttribute.ConstructorArguments[0].Value as INamedTypeSymbol;
				var baseTypeNames = baseType.GetSymbolNames();
				var baseTypeBuilderNames = baseTypeBuilder.GetSymbolNames(baseType);

				var resultBaseType = baseTypeNames.GetSymbolFullNameWithGenerics(baseType);
				var resultBuilderType = baseTypeBuilderNames.GetSymbolFullNameWithGenerics(baseType);
				return (true, resultBaseType, resultBuilderType, isImmutablePresent, baseTypeBuilder);
			}

			var baseTypeDefinition = baseType.ConstructedFrom ?? baseType;

			// Builder is to be generated (no attribute yet for reaching the builder)
			if (immutableEntitiesToGenerate.Contains(baseTypeDefinition))
			{
				var names = baseType.GetSymbolNames();

				var isSameNamespace = baseType.ContainingNamespace.Equals(type.ContainingNamespace);
				var baseTypeName = isSameNamespace ? names.SymbolNameWithGenerics : baseType.ToDisplayString();
				var builderBaseType = baseTypeName + ".Builder";

				return (true, baseTypeName, builderBaseType, isImmutablePresent, null);
			}

			return (false, null, null, isImmutablePresent, null); // no relevant basetype
		}

		private void GenerateImmutable(INamedTypeSymbol typeSymbol,
			(bool isBaseTypePresent, string baseType, string builderBaseType, bool isImmutablePresent, INamedTypeSymbol baseBuilderType) baseTypeInfo,
			bool generateEquality)
		{
			var defaultMemberName = "Default";

			var generateOption = _generateOptionCode && !typeSymbol.IsAbstract;
			var generateJsonNet = _generateJsonNet && !typeSymbol.IsAbstract;
			var generateSystemTextJson = _generateSystemTextJson && !typeSymbol.IsAbstract;

			var classCopyIgnoreRegexes = ExtractCopyIgnoreAttributes(typeSymbol).ToArray();

			(IPropertySymbol property, bool isNew)[] properties;

			var typeProperties = typeSymbol
				.GetProperties()
				.Where(p => !p.IsStatic)
				.ToArray();

			if (baseTypeInfo.isBaseTypePresent)
			{
				var baseProperties = typeSymbol.BaseType.GetProperties()
					.Where(x => x.IsReadOnly && x.IsAutoProperty())
					.Select(x => x.Name)
					.ToArray();

				properties = typeProperties
					.Where(x => x.IsReadOnly && x.IsAutoProperty())
					.Select(x => (x, baseProperties.Contains(x.Name))) // remove properties already present in base class
					.ToArray();
			}
			else
			{
				properties = typeProperties
					.Where(x => x.IsReadOnly && x.IsAutoProperty())
					.Select(x => (x, false))
					.ToArray();
			}

			var builder = new IndentedStringBuilder();

			var symbolNames = typeSymbol.GetSymbolNames();
			var (symbolName, genericArguments, symbolNameWithGenerics, symbolNameForXml, symbolNameDefinition, resultFileName, genericConstraints) = symbolNames;
			_currentType = symbolNameWithGenerics;

			ValidateType(builder, typeSymbol, baseTypeInfo, symbolNames, typeProperties);

			var newModifier = baseTypeInfo.isBaseTypePresent ? "new " : "";

			builder.AppendLineInvariant("// <auto-generated>");
			builder.AppendLineInvariant("// **********************************************************************************************************************");
			builder.AppendLineInvariant("// This file has been generated by Uno.CodeGen (ImmutableGenerator), available at https://github.com/unoplatform/Uno.CodeGen");
			builder.AppendLineInvariant("// **********************************************************************************************************************");
			builder.AppendLineInvariant("// </auto-generated>");
			builder.AppendLineInvariant("#pragma warning disable");
			builder.AppendLine();
			builder.AppendLineInvariant("using System;");
			builder.AppendLine();

			using (builder.BlockInvariant($"namespace {typeSymbol.ContainingNamespace}"))
			{
				string builderTypeNameAndBaseClass;
				if (typeSymbol.IsAbstract)
				{
					builderTypeNameAndBaseClass = baseTypeInfo.isBaseTypePresent ? $"Builder : {baseTypeInfo.builderBaseType}" : $"Builder";
				}
				else
				{
					builderTypeNameAndBaseClass = baseTypeInfo.isBaseTypePresent
					? $"Builder : {baseTypeInfo.builderBaseType}, global::Uno.IImmutableBuilder<{symbolNameWithGenerics}>"
					: $"Builder : global::Uno.IImmutableBuilder<{symbolNameWithGenerics}>";
				}

				if (baseTypeInfo.isImmutablePresent)
				{
					builder.AppendLineInvariant("// Note: The attribute [Uno.Immutable] is already present on the class");
				}
				else
				{
					builder.AppendLineInvariant("[global::Uno.Immutable] // Mark this class as Immutable for some analyzers requiring it.");
				}

				if (generateEquality)
				{
					builder.AppendLineInvariant("[global::Uno.GeneratedEquality] // Set [GeneratedImmutable(GeneratedEquality = false)] if you don't want this attribute.");
				}

				if (generateJsonNet)
				{
					builder.AppendLineInvariant($"[global::Newtonsoft.Json.JsonConverter(typeof({symbolName}BuilderJsonConverterTo{symbolNameDefinition}))]");
				}

				if (generateSystemTextJson)
				{
					builder.AppendLineInvariant($"[global::System.Text.Json.Serialization.JsonConverter(typeof({symbolName}BuilderSystemTextJsonConverterTo{symbolNameDefinition}))]");
				}

				builder.AppendLineInvariant($"[global::Uno.ImmutableBuilder(typeof({symbolNameDefinition}.Builder))] // Other generators can use this to find the builder.");

				var abstractClause = typeSymbol.IsAbstract ? "abstract " : "";

				using (builder.BlockInvariant($"{typeSymbol.GetAccessibilityAsCSharpCodeString()} {abstractClause}partial class {symbolNameWithGenerics}"))
				{
					if(!typeSymbol.IsAbstract)
					{
						builder.AppendLineInvariant($"/// <summary>");
						builder.AppendLineInvariant($"/// {defaultMemberName} instance with only property initializer set.");
						builder.AppendLineInvariant($"/// </summary>");
						builder.AppendLineInvariant($"public static readonly {newModifier}{symbolNameWithGenerics} {defaultMemberName} = new {symbolNameWithGenerics}();");
						builder.AppendLine();
					}

					var prop1Name = properties.Select(p => p.property.Name).FirstOrDefault() ?? symbolName + "Property";
					builder.AppendLineInvariant($"/// <summary>");
					builder.AppendLineInvariant($"/// Stateful builder to construct immutable instance(s) of {symbolNameForXml}.");
					builder.AppendLineInvariant($"/// </summary>");
					builder.AppendLineInvariant($"/// <remarks>");
					builder.AppendLineInvariant($"/// This builder is mutable. You can change their properties as you want or");
					builder.AppendLineInvariant($"/// use the .WithXXX() methods to do it in a fluent way. You can continue to");
					builder.AppendLineInvariant($"/// change it even after calling the `.ToImmutable()` method: it will simply");
					builder.AppendLineInvariant($"/// generate a new version from the current state.");
					builder.AppendLineInvariant($"/// **THE BUILDER IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
					builder.AppendLineInvariant($"/// </remarks>");
					builder.AppendLineInvariant("/// <example>");
					builder.AppendLineInvariant($"/// // The following code will create a builder using a .With{prop1Name}() method:");
					builder.AppendLineInvariant($"/// {symbolNameForXml}.Builder b = my{symbolName}Instance.With{prop1Name}([{prop1Name} value]);");
					builder.AppendLineInvariant("///");
					builder.AppendLineInvariant($"/// // The following code will use implicit cast to create a new {symbolNameForXml} immutable instance:");
					builder.AppendLineInvariant("{0}", $"/// {symbolNameForXml} my{symbolName}Instance = new {symbolNameForXml}.Builder {{ {prop1Name} = [{prop1Name} value], ... }};");
					builder.AppendLineInvariant("/// </example>");

					var abstractBuilder = typeSymbol.IsAbstract ? "abstract " : "";
					using (builder.BlockInvariant(
						$"{typeSymbol.GetAccessibilityAsCSharpCodeString()} {abstractBuilder}{newModifier}partial class {builderTypeNameAndBaseClass}"))
					{
						if (!baseTypeInfo.isBaseTypePresent)
						{
							// _isDirty only on base builder (will be reused in derived builders)
							builder.AppendLineInvariant("// Dirty means there's a difference from `_original`.");
							builder.AppendLineInvariant("protected bool _isDirty = false;");
							builder.AppendLine();
							builder.AppendLineInvariant("// This is the original entity, if any (could be null))");
							builder.AppendLineInvariant("{0}", $"protected readonly {symbolNameWithGenerics} _original;");
							builder.AppendLine();
							builder.AppendLineInvariant("// Cached version of generated entity (flushed when the builder is updated)");
							builder.AppendLineInvariant($"protected {symbolNameWithGenerics} _cachedResult = default({symbolNameWithGenerics});");
							builder.AppendLine();

							if (typeSymbol.IsAbstract)
							{
								using (builder.BlockInvariant($"public Builder({symbolNameWithGenerics} original)"))
								{
									builder.AppendLineInvariant($"_original = original ?? throw new global::System.ArgumentNullException(nameof(original));");
								}
							}
							else
							{
								using (builder.BlockInvariant($"public Builder({symbolNameWithGenerics} original)"))
								{
									builder.AppendLineInvariant($"_original = original ?? {symbolNameWithGenerics}.Default;");
								}

								builder.AppendLine();
								using (builder.BlockInvariant($"public Builder()"))
								{
									builder.AppendLineInvariant($"_original = {symbolNameWithGenerics}.Default;");
								}
							}
						}
						else
						{
							if (typeSymbol.IsAbstract)
							{
								using (builder.BlockInvariant($"protected Builder({symbolNameWithGenerics} original) : base(original)"))
								{
									builder.AppendLineInvariant($"// It's an abstract class, we can't create an instance of this builder directly.");
									builder.AppendLineInvariant($"// The _original field is assigned in base constructor.");
								}
							}
							else
							{
								using (builder.BlockInvariant($"public Builder({symbolNameWithGenerics} original) : base(original ?? {symbolNameWithGenerics}.Default)"))
								{
									builder.AppendLineInvariant($"// Default constructor, the _original field is assigned in base constructor.");
								}

								using (builder.BlockInvariant($"public Builder() : base({symbolNameWithGenerics}.Default)"))
								{
									builder.AppendLineInvariant($"// Default constructor, the _original field is assigned in base constructor.");
								}
							}
						}

						builder.AppendLine();

						var resetNone = generateOption
							? $"{Environment.NewLine}			_isNone = false;"
							: "";

						foreach (var (prop, isNew) in properties)
						{
							if (prop.IsIndexer)
							{
								builder.AppendLine(
									$"#error Indexer {prop.Name} not supported! You must remove it for this to compile correctly.");
								continue;
							}

							if (prop.IsWithEvents)
							{
								builder.AppendLine("#error Events properties not supported!");
							}

							var newPropertyModifier = isNew ? "new " : "";

							var attributes = GetAttributes(classCopyIgnoreRegexes, prop);

							var propType = GetPropType(prop);

							builder.AppendLine($@"
// Backing field for property {prop.Name}.
private {propType} _{prop.Name};

// If the property {prop.Name} has been set in the builder.
// `false` means the property hasn't been set or has been reverted to original value `{typeSymbol.Name}.Default.{
									prop.Name
								}`.
private bool _is{prop.Name}Set = false;

/// <summary>
/// Get/Set the current builder value for {prop.Name}.
/// </summary>
/// <remarks>
/// When nothing is set in the builder, the value is `default({propType})`.
/// </remarks>
{attributes}public {newPropertyModifier}{propType} {prop.Name}
{{
	get => _is{prop.Name}Set ? _{prop.Name} : (_original as {symbolNameWithGenerics}).{prop.Name};
	set
	{{
		var originalValue = (({symbolNameWithGenerics})_original).{prop.Name};
		var isSameAsOriginal = global::System.Collections.Generic.EqualityComparer<{propType}>.Default.Equals(originalValue, value);
		if(isSameAsOriginal)
		{{
			// Property {prop.Name} has been set back to original value
			_is{prop.Name}Set = false;
			_{prop.Name} = default({propType}); // dereference to prevent any leak (when it's a reference type)
		}}
		else
		{{
			_is{prop.Name}Set = true;
			_{prop.Name} = value;
			_isDirty = true;{resetNone}
		}}
		_cachedResult = null;
	}}
}}
");
							builder.AppendLine();
						}

						if(!typeSymbol.IsAbstract)
						{
							builder.AppendLineInvariant("/// <summary>");
							builder.AppendLineInvariant($"/// Create an immutable instance of {symbolNameForXml}.");
							builder.AppendLineInvariant("/// </summary>");
							builder.AppendLineInvariant("/// <remarks>");
							builder.AppendLineInvariant("/// Will return original if nothing changed in the builder (and an original was specified).");
							builder.AppendLineInvariant("/// Application code should prefer the usage of implicit casting which is calling this method.");
							builder.AppendLineInvariant($"/// **THIS METHOD IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
							builder.AppendLineInvariant("/// </remarks>");
							builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
							using (builder.BlockInvariant($"public {newModifier}{symbolNameWithGenerics} ToImmutable()"))
							{
								builder.AppendLine(
$@"var cachedResult = _cachedResult as {symbolNameWithGenerics};
if(!ReferenceEquals(cachedResult, null))
{{
	return cachedResult; // already computed, no need to redo this.
}}

if (_isDirty)
{{
	var new{symbolName} = new {symbolNameWithGenerics}(this);
	if (!Equals(new{symbolName}, _original))
	{{
		return ({symbolNameWithGenerics})(_cachedResult = new{symbolName});
	}}
}}
return ({symbolNameWithGenerics})(_cachedResult = _original);");
								builder.AppendLine();
							}

							builder.AppendLine();
						}
						else
						{
							builder.AppendLineInvariant($"// Since {symbolNameWithGenerics} is abstract, there is no .ToImmutable():");
							builder.AppendLineInvariant($"// you should call it on a derived class, which should implement it.");

							builder.AppendLine();
						}

						if (properties.Any())
						{
							builder.AppendLineInvariant("{0}", $"#region .WithXXX() methods on {symbolNameWithGenerics}.Builder");
							foreach (var (prop, isNew) in properties)
							{
								var propType = GetPropType(prop);

								var newPropertyModifier = isNew ? "new " : "";

								builder.AppendLineInvariant($"/// <summary>");
								builder.AppendLineInvariant($"/// Set property {prop.Name} in a fluent declaration.");
								builder.AppendLineInvariant($"/// </summary>");
								builder.AppendLineInvariant($"/// <remarks>");
								builder.AppendLineInvariant($"/// **THIS METHOD IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
								builder.AppendLineInvariant($"/// </remarks>");
								builder.AppendLineInvariant("/// <example>");
								builder.AppendLineInvariant("{0}", $"/// var builder = new {symbolNameForXml}.Builder {{ {prop1Name} = xxx, ... }}; // create a builder instance");
								builder.AppendLineInvariant($"/// {propType} new{prop.Name}Value = [...];");
								builder.AppendLineInvariant($"/// {symbolNameForXml} instance = builder.With{prop.Name}(new{prop.Name}Value); // create an immutable instance");
								builder.AppendLineInvariant("/// </example>");
								using (builder.BlockInvariant($"public {newPropertyModifier}Builder With{prop.Name}({propType} value)"))
								{
									builder.AppendLineInvariant($"{prop.Name} = value;");
									builder.AppendLineInvariant("return this;");
								}

								builder.AppendLine();

								builder.AppendLineInvariant($"/// <summary>");
								builder.AppendLineInvariant($"/// Set property {prop.Name} in a fluent declaration by projecting previous value.");
								builder.AppendLineInvariant($"/// </summary>");
								builder.AppendLineInvariant($"/// <remarks>");
								builder.AppendLineInvariant($"/// **THIS METHOD IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
								builder.AppendLineInvariant($"/// The selector will be called immediately. The main usage of this overload is to specify a _method group_.");
								builder.AppendLineInvariant($"/// </remarks>");
								builder.AppendLineInvariant("/// <example>");
								builder.AppendLineInvariant("{0}", $"/// var builder = new {symbolNameForXml}.Builder {{ {prop1Name} = xxx, ... }}; // create a builder instance");
								builder.AppendLineInvariant($"/// {symbolNameForXml} instance = builder.With{prop.Name}(previous{prop.Name}Value => new {propType}(...)); // create an immutable instance");
								builder.AppendLineInvariant("/// </example>");
								using (builder.BlockInvariant($"public {newPropertyModifier}Builder With{prop.Name}(Func<{propType}, {propType}> valueSelector)"))
								{
									builder.AppendLineInvariant($"{prop.Name} = valueSelector({prop.Name});");
									builder.AppendLineInvariant("return this;");
								}

								builder.AppendLine();
							}

							builder.AppendLineInvariant($"#endregion // .WithXXX() methods on {symbolNameWithGenerics}.Builder");
						}
					}

					if (generateOption)
					{
						builder.AppendLine();
						builder.AppendLineInvariant($"#region Uno.Option<{symbolNameWithGenerics}>'s specific code");
						builder.AppendLine();
						builder.AppendLineInvariant($"public static readonly {newModifier}global::Uno.Option<{symbolNameWithGenerics}> None = global::Uno.Option.None<{symbolNameWithGenerics}>();");
						builder.AppendLine();

						using (builder.BlockInvariant($"partial class Builder"))
						{
							builder.AppendLine(
								$@"private bool _isNone;

public static Builder FromOption(global::Uno.Option<{symbolNameWithGenerics}> original)
{{
	if(original.MatchSome(out var o))
	{{
		return new Builder(o);
	}}
	else
	{{
		return new Builder() {{ _isNone = true }};
	}}
}}

/// <summary>
/// Get an Option&lt;{symbolNameForXml}&gt; for the current state of the builder
/// </summary>
/// <remarks>
/// Will return {symbolNameForXml}.None or Option.Some(ToImmutable());
/// </remarks>
[global::System.Diagnostics.Contracts.Pure]
public {newModifier}global::Uno.Option<{symbolNameWithGenerics}> ToOptionImmutable()
{{
	return _isNone
		? {symbolNameWithGenerics}.None
		: global::Uno.Option.Some(ToImmutable());
}}

// Implicit cast from Option<{symbolNameWithGenerics}> to Builder
public static implicit operator Builder(global::Uno.Option<{symbolNameWithGenerics}> original)
{{
	return Builder.FromOption(original);
}}

// Implicit cast from Builder to Option<{symbolNameWithGenerics}>
public static implicit operator global::Uno.Option<{symbolNameWithGenerics}>(Builder builder)
{{
	return builder.ToOptionImmutable();
}}");
							builder.AppendLine();
						}

						builder.AppendLineInvariant($"#endregion // Uno.Option<{symbolNameWithGenerics}>'s specific code");
					}

					builder.AppendLine();

					builder.AppendLineInvariant($"// Default constructor - to ensure it's not defined by application code.");
					builder.AppendLineInvariant($"//");
					builder.AppendLineInvariant($"// \"error CS0111: Type '{symbolNameWithGenerics}' already defines a member called '.ctor' with the same parameter types\":");
					builder.AppendLineInvariant($"//  => You have this error? it's because you defined a default constructor on your class!");
					builder.AppendLineInvariant($"//");
					builder.AppendLineInvariant($"// New instances should use the builder instead.");
					builder.AppendLineInvariant($"//");
					builder.AppendLineInvariant($"// Send your complaints or inquiried to \"architecture [-@-] nventive [.] com\".");
					builder.AppendLineInvariant($"//");
					builder.AppendLineInvariant("{0}", $"protected {symbolName}() {{}} // see previous comments if you want this removed (TL;DR: you can't)");

					builder.AppendLine();

					builder.AppendLineInvariant($"/// <summary>");
					builder.AppendLineInvariant($"/// Construct a new immutable instance of {symbolNameForXml} from a builder.");
					builder.AppendLineInvariant($"/// </summary>");
					builder.AppendLineInvariant("/// <remarks>");
					builder.AppendLineInvariant("/// Application code should prefer the usage of implicit casting which is calling this constructor.");
					builder.AppendLineInvariant("/// </remarks>");
					builder.AppendLineInvariant($"/// <param name=\"builder\">The builder for {symbolNameForXml}.</param>");

					var baseConstructorChaining = baseTypeInfo.isBaseTypePresent
						? " : base(builder)"
						: "";

					using (builder.BlockInvariant($"public {symbolName}(Builder builder){baseConstructorChaining}"))
					{
						builder.AppendLineInvariant("if(builder == null) throw new global::System.ArgumentNullException(nameof(builder));");

						foreach (var (prop, _) in properties)
						{
							builder.AppendLineInvariant($"{prop.Name} = builder.{prop.Name};");
						}

						builder.AppendLine();
					}

					builder.AppendLine();

					if (!typeSymbol.IsAbstract)
					{
						builder.AppendLine(
$@"// Implicit cast from {symbolNameWithGenerics} to Builder: will simply create a new instance of the builder.
public static implicit operator Builder({symbolNameWithGenerics} original)
{{
	return new Builder(original);
}}

// Implicit cast from Builder to {symbolNameWithGenerics}: will simply create the immutable instance of the class.
public static implicit operator {symbolNameWithGenerics}(Builder builder)
{{
	return builder.ToImmutable();
}}");
						builder.AppendLine();
					}
				}

				builder.AppendLine();
				var propertiesForWithExtensions = GetPropertiesForWithExtensions(baseTypeInfo.baseBuilderType, properties);

				if (propertiesForWithExtensions.Any() && !typeSymbol.IsAbstract)
				{
					using (builder.BlockInvariant($"{typeSymbol.GetAccessibilityAsCSharpCodeString()} static partial class {symbolName}Extensions"))
					{
						var builderName = $"{symbolNameWithGenerics}.Builder";

						builder.AppendLine();
						builder.AppendLineInvariant($"#region .WithXXX() methods on {symbolNameWithGenerics}");
						foreach (var prop in propertiesForWithExtensions)
						{
							var propType = GetPropType(prop);

							builder.AppendLine();

							builder.AppendLineInvariant("/// <summary>");
							builder.AppendLineInvariant($"/// Set property {prop.Name} in a fluent declaration.");
							builder.AppendLineInvariant("/// </summary>");
							builder.AppendLineInvariant("/// <remarks>");
							builder.AppendLineInvariant($"/// The return value is a builder which can be casted implicitly to {symbolNameForXml} or used to make more changes.");
							builder.AppendLineInvariant("/// **THIS METHOD IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
							builder.AppendLineInvariant("/// </remarks>");
							builder.AppendLineInvariant("/// <example>");
							builder.AppendLineInvariant($"/// {symbolNameForXml} original = {symbolNameForXml}.{defaultMemberName}; // first immutable instance");
							builder.AppendLineInvariant($"/// {propType} new{prop.Name}Value = [...];");
							builder.AppendLineInvariant($"/// {symbolNameForXml} modified = original.With{prop.Name}(new{prop.Name}Value); // create a new modified immutable instance");
							builder.AppendLineInvariant("/// </example>");
							builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
							using (builder.BlockInvariant($"public static {builderName} With{prop.Name}{genericArguments}(this {symbolNameWithGenerics} entity, {propType} value){genericConstraints}"))
							{
								builder.AppendLineInvariant($"return ({builderName})(new {builderName}(entity).With{prop.Name}(value));");
							}

							builder.AppendLine();

							builder.AppendLineInvariant("/// <summary>");
							builder.AppendLineInvariant($"/// Set property {prop.Name} in a fluent declaration by projecting previous value.");
							builder.AppendLineInvariant("/// </summary>");
							builder.AppendLineInvariant("/// <remarks>");
							builder.AppendLineInvariant($"/// The return value is a builder which can be casted implicitly to {symbolNameForXml} or used to make more changes.");
							builder.AppendLineInvariant("/// **THIS METHOD IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
							builder.AppendLineInvariant($"/// The selector will be called immediately. The main usage of this overload is to specify a _method group_.");
							builder.AppendLineInvariant("/// </remarks>");
							builder.AppendLineInvariant("/// <example>");
							builder.AppendLineInvariant($"/// {symbolNameForXml} original = {symbolNameForXml}.{defaultMemberName}; // first immutable instance");
							builder.AppendLineInvariant($"/// {symbolNameForXml} modified = original.With{prop.Name}(previous{prop.Name}Value => new {propType}(...)); // create a new modified immutable instance");
							builder.AppendLineInvariant("/// </example>");
							builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
							using (builder.BlockInvariant($"public static {builderName} With{prop.Name}{genericArguments}(this {symbolNameWithGenerics} entity, Func<{propType}, {propType}> valueSelector){genericConstraints}"))
							{
								builder.AppendLineInvariant($"return ({builderName})(new {builderName}(entity).With{prop.Name}(valueSelector));");
							}

							builder.AppendLine();

							if (generateOption)
							{
								builder.AppendLineInvariant("/// <summary>");
								builder.AppendLineInvariant($"/// Set property {prop.Name} in a fluent declaration.");
								builder.AppendLineInvariant("/// </summary>");
								builder.AppendLineInvariant("/// <remarks>");
								builder.AppendLineInvariant($"/// The return value is a builder which can be casted implicitly to {symbolNameForXml} or used to make more changes.");
								builder.AppendLineInvariant($"/// IMPORTANT: When the entity is Option.None&lt;{symbolNameForXml}&gt;, the result will be identical as calling {symbolNameForXml}.Default.With{prop.Name}([value]).");
								builder.AppendLineInvariant("/// **THIS METHOD IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
								builder.AppendLineInvariant("/// </remarks>");
								builder.AppendLineInvariant("/// <example>");
								builder.AppendLineInvariant($"/// Option&lt;{symbolNameForXml}&gt; original = Option.Some({symbolNameForXml}.{defaultMemberName});");
								builder.AppendLineInvariant($"/// {propType} new{prop.Name}Value = [...];");
								builder.AppendLineInvariant($"/// Option&lt;{symbolNameForXml}&gt; modified = original.With{prop.Name}(new{prop.Name}Value); // result type is Option.Some");
								builder.AppendLineInvariant("/// </example>");
								builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
								using (builder.BlockInvariant($"public static {builderName} With{prop.Name}{genericArguments}(this global::Uno.Option<{symbolNameWithGenerics}> optionEntity, {propType} value){genericConstraints}"))
								{
									builder.AppendLineInvariant($"return ({builderName})({builderName}.FromOption(optionEntity).With{prop.Name}(value));");
								}

								builder.AppendLine();

								builder.AppendLineInvariant("/// <summary>");
								builder.AppendLineInvariant($"/// Set property {prop.Name} in a fluent declaration by projecting previous value.");
								builder.AppendLineInvariant("/// </summary>");
								builder.AppendLineInvariant("/// <remarks>");
								builder.AppendLineInvariant($"/// The return value is a builder which can be casted implicitly to {symbolNameForXml} or used to make more changes.");
								builder.AppendLineInvariant($"/// IMPORTANT: When the entity is Option.None&lt;{symbolNameForXml}&gt;, the result will be identical as calling {symbolNameForXml}.Default.With{prop.Name}([valueSelector]).");
								builder.AppendLineInvariant("/// **THIS METHOD IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
								builder.AppendLineInvariant($"/// The selector will be called immediately. The main usage of this overload is to specify a _method group_.");
								builder.AppendLineInvariant("/// </remarks>");
								builder.AppendLineInvariant("/// <example>");
								builder.AppendLineInvariant($"/// {symbolNameForXml} original = {symbolNameForXml}.{defaultMemberName}; // first immutable instance");
								builder.AppendLineInvariant($"/// {symbolNameForXml} modified = original.With{prop.Name}(previous{prop.Name}Value => new {propType}(...)); // create a new modified immutable instance");
								builder.AppendLineInvariant("/// </example>");
								builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
								using (builder.BlockInvariant($"public static {builderName} With{prop.Name}{genericArguments}(this global::Uno.Option<{symbolNameWithGenerics}> optionEntity, Func<{propType}, {propType}> valueSelector){genericConstraints}"))
								{
									builder.AppendLineInvariant($"return ({builderName})({builderName}.FromOption(optionEntity).With{prop.Name}(valueSelector));");
								}

								builder.AppendLine();
							}
						}

						builder.AppendLineInvariant($"#endregion // .WithXXX() methods on {symbolNameWithGenerics}");
					}

					builder.AppendLine();
				}

				if (generateJsonNet)
				{
					builder.AppendLine(
$@"public sealed class {symbolName}BuilderJsonConverterTo{symbolName}{genericArguments} : global::Newtonsoft.Json.JsonConverter{genericConstraints}
{{
	public override void WriteJson(global::Newtonsoft.Json.JsonWriter writer, object value, global::Newtonsoft.Json.JsonSerializer serializer)
	{{
		var v = ({symbolNameWithGenerics}.Builder)({symbolNameWithGenerics})value;
		serializer.Serialize(writer, v);
	}}

	public override object ReadJson(global::Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, global::Newtonsoft.Json.JsonSerializer serializer)
	{{
		var o = serializer.Deserialize<{symbolNameWithGenerics}.Builder>(reader);
		return o?.ToImmutable();
	}}

	public override bool CanConvert(Type objectType)
	{{
		return objectType == typeof({symbolNameWithGenerics}) || objectType == typeof({symbolNameWithGenerics}.Builder);
	}}
}}");

					builder.AppendLine();
				}

				if (generateSystemTextJson)
				{
					builder.AppendLine(
$@"{typeSymbol.GetAccessibilityAsCSharpCodeString()} sealed class {symbolName}BuilderSystemTextJsonConverterTo{symbolName}{genericArguments} : global::System.Text.Json.Serialization.JsonConverter<{symbolNameWithGenerics}>{genericConstraints}
{{
	public override void Write(global::System.Text.Json.Utf8JsonWriter writer, {symbolName}{genericArguments} value, global::System.Text.Json.JsonSerializerOptions options)
	{{
		global::System.Text.Json.JsonSerializer.Serialize<{symbolNameWithGenerics}.Builder>(writer, value, options);
	}}

	public override {symbolName}{genericArguments} Read(ref global::System.Text.Json.Utf8JsonReader reader, Type typeToConvert, global::System.Text.Json.JsonSerializerOptions options)
	{{
		var v = global::System.Text.Json.JsonSerializer.Deserialize<{symbolNameWithGenerics}.Builder>(ref reader, options);
		return v?.ToImmutable();
	}}
}}");

					builder.AppendLine();
				}
			}

			_context.AddCompilationUnit(resultFileName, builder.ToString());
		}

		private static string GetPropType(IPropertySymbol prop)
		{
			var propTypeNames = prop.Type.GetSymbolNames();
			var propType = propTypeNames?.GetSymbolFullNameWithGenerics() ?? prop.Type.ToString();
			return propType;
		}

		private IPropertySymbol[] GetPropertiesForWithExtensions(INamedTypeSymbol baseBuilderSymbol, (IPropertySymbol property, bool isNew)[] currentTypeProperties)
		{
			if (baseBuilderSymbol == null || baseBuilderSymbol.SpecialType == SpecialType.System_Object)
			{
				return currentTypeProperties.Select(x => x.property).ToArray();
			}

			var baseBuildeProperties = baseBuilderSymbol
				.GetProperties()
				.Where(p => !p.IsReadOnly && p.DeclaredAccessibility == Accessibility.Public);

			return currentTypeProperties
				.Select(x => x.property)
				.Concat(baseBuildeProperties)
				.ToArray();
		}

		private void ValidateType(
			IIndentedStringBuilder builder,
			INamedTypeSymbol typeSymbol,
			(bool isBaseType, string baseType, string builderBaseType, bool isImmutablePresent, INamedTypeSymbol baseBuilderType) baseTypeInfo,
			SymbolNames symbolNames, IPropertySymbol[] typeProperties)
		{
			if (!typeSymbol.IsFromPartialDeclaration())
			{
				Error(builder, $"You must add the partial modifier to the class {symbolNames.SymbolNameWithGenerics}.");
			}

			if (typeSymbol.IsValueType)
			{
				Error(builder, $"To generate code for type {symbolNames.SymbolNameWithGenerics}, it **MUST** be a class, not a struct.");
			}

			if (baseTypeInfo.isBaseType && baseTypeInfo.baseType == null)
			{
				Error(builder, $"To generate code for type {symbolNames.SymbolNameWithGenerics}, it **MUST** derive from an immutable class.");
			}

			void CheckTypeImmutable(ITypeSymbol type, string typeSource, bool constraintsChecked = false)
			{
				var typeName = (type as INamedTypeSymbol)?.GetSymbolNames().SymbolNameWithGenerics ?? type.ToString();

				if (type is ITypeParameterSymbol typeParameter)
				{
					if (typeParameter.ConstraintTypes.Any())
					{
						if (!constraintsChecked)
						{
							foreach (var constraintType in typeParameter.ConstraintTypes)
							{
								CheckTypeImmutable(constraintType, $"{typeSymbol} / Generic type {typeParameter}:{constraintType}", constraintsChecked: true);
							}
						}
					}
					else
					{
						Error(builder, $"{typeSource} is of generic type {typeName} which isn't restricted to immutable (with a \"where\" clause). You can also make your class abstract.");
					}

					return; // ok
				}

				if (type.DerivesFromType(_immutableBuilderInterfaceSymbol))
				{
					Error(builder, $"{typeSource} ({typeName}) is a builder. It cannot be used in an immutable entity.");
				}
				else if (!type.IsImmutable(_generationOptions.treatArrayAsImmutable, _knownAsImmutableTypes))
				{
					if (type is IArrayTypeSymbol)
					{
						Error(
							builder,
							$"{typeSource} ({typeName}) is an array, which is not immutable. "
							+ "You can treat arrays as immutable by setting a global attribute: " 
							+ "[assembly: global::Uno.ImmutableGenerationOptions(TreatArrayAsImmutable = true)].");

					}
					else
					{
						Error(
							builder,
							$"{typeSource} ({typeName}) not immutable. It cannot be used in an immutable entity. "
							+ "If you know the type can safely be used as immutable, add a global attribyte: "
							+ $"[assembly: global::Uno.TreatAsImmutable(typeof({typeName}))]");
					}
				}

				if(type is INamedTypeSymbol namedType)
				{
					foreach (var typeArgument in namedType.TypeArguments)
					{
						CheckTypeImmutable(typeArgument, $"{typeSource} (argument type {typeArgument})", constraintsChecked);
					}
				}
			}

			foreach (var prop in typeProperties)
			{
				if (prop.IsStatic)
				{
					continue; // we don't care about static stuff.
				}
				if (!prop.IsReadOnly)
				{
					Error(
						builder,
						$"Non-static property {symbolNames.SymbolNameWithGenerics}.{prop.Name} "
						+ "cannot have a setter, even a private one. You must remove it for immutable generation.");
				}

				if (!typeSymbol.IsAbstract)
				{
					CheckTypeImmutable(prop.Type, $"Property {symbolNames.SymbolNameWithGenerics}.{prop.Name}");
				}
			}

			foreach (var typeArgument in typeSymbol.BaseType.TypeArguments)
			{
				if (typeSymbol.IsAbstract && typeSymbol is ITypeParameterSymbol)
				{
					continue;
				}
				CheckTypeImmutable(typeArgument, $"Type Argument {typeArgument.Name}");
			}

			foreach (var field in typeSymbol.GetFields())
			{
				if (field.IsImplicitlyDeclared)
				{
					continue; // that's from the compiler, it's ok.
				}

				if (!field.IsStatic)
				{
					if (field.IsReadOnly)
					{
						CheckTypeImmutable(field.Type, $"Field {field.Name}");
					}
					else
					{
						Error(
							builder,
							$"Immutable type {symbolNames.SymbolNameWithGenerics} cannot "
							+ $"have the non-static field {field.Name}. You must either remove it, make it readonly or static to allow for immutable generation.");
					}
				}
			}
		}

		private string GetAttributes(Regex[] classCopyIgnoreRegexes, IPropertySymbol prop)
		{
			var allAttributesIgnores =
				_copyIgnoreAttributeRegexes
					.Concat(classCopyIgnoreRegexes)
					.Concat(ExtractCopyIgnoreAttributes(prop))
					.ToList();

			IEnumerable<string> Enumerate()
			{
				foreach (var attribute in prop.GetAttributes())
				{
					var attrResult = attribute.ToString();
					if (allAttributesIgnores.Any(r => r.IsMatch(attrResult)))
					{
						continue; // should be ignored
					}

					yield return $"[global::{attrResult}]{Environment.NewLine}";
				}
			}

			return string.Join("", Enumerate());
		}

		private IEnumerable<(INamedTypeSymbol symbol, AttributeData attribute)> EnumerateImmutableGeneratedEntities()
			=> from type in _context.Compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
				let moduleAttribute = type.FindAttributeFlattened(_generatedImmutableAttributeSymbol)
				where moduleAttribute != null
				//where (bool) moduleAttribute.ConstructorArguments[0].Value
				select (type, moduleAttribute);

		private IReadOnlyList<ITypeSymbol> EnumerateTreatAsImmutables()
		{
			var currentModuleAttributes = _context.Compilation.Assembly.GetAttributes();
			var referencedAssembliesAttributes =
				_context.Compilation.SourceModule.ReferencedAssemblySymbols.SelectMany(a => a.GetAttributes());

			var knownToBeImmutableTypes = currentModuleAttributes
				.Concat(referencedAssembliesAttributes)
				.Where(a => a.AttributeClass.Equals(_immutableTreatAsImmutableAttributeSymbol))
				.Select(a => a.ConstructorArguments[0].Value)
				.Cast<ITypeSymbol>()
				.Distinct()
				.ToImmutableArray();

			return knownToBeImmutableTypes;
		}

		private void Warning(IIndentedStringBuilder builder, string warningMsg)
		{
			var msg = $"{nameof(ImmutableGenerator)}/{_currentType}: {warningMsg}";
			_logger.Warn(msg);
			builder.AppendLineInvariant("#warning " + msg.Replace('\n', ' ').Replace('\r', ' '));
		}

		private void Error(IIndentedStringBuilder builder, string warningMsg)
		{
			var msg = $"{nameof(ImmutableGenerator)}/{_currentType}: {warningMsg}";
			_logger.Error(msg);
			builder.AppendLineInvariant("#error " + msg.Replace('\n', ' ').Replace('\r', ' '));
		}
	}
}
