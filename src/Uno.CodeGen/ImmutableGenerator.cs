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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Uno.Helpers;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;
using TypeSymbolExtensions = Uno.Helpers.TypeSymbolExtensions;

namespace Uno
{
	public class ImmutableGenerator : SourceGenerator
	{
		private SourceGeneratorContext _context;
		private INamedTypeSymbol _systemObject;
		private INamedTypeSymbol _immutableAttributeSymbol;
		private INamedTypeSymbol _generatedImmutableAttributeSymbol;
		private INamedTypeSymbol _immutableBuilderAttributeSymbol;
		private INamedTypeSymbol _immutableAttributeCopyIgnoreAttributeSymbol;
		private INamedTypeSymbol _immutableGenerationOptionsAttributeSymbol;

		private (bool generateOptionCode, bool treatArrayAsImmutable, bool generateEqualityByDefault, bool generateJsonNet) _generationOptions;
		private bool _generateOptionCode = true;
		private bool _generateJsonNet = true;

		private Regex[] _copyIgnoreAttributeRegexes;

		public override void Execute(SourceGeneratorContext context)
		{
			_context = context;
			_systemObject = context.Compilation.GetTypeByMetadataName("System.Object");
			_immutableAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.ImmutableAttribute");
			_generatedImmutableAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.GeneratedImmutableAttribute");
			_immutableBuilderAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.ImmutableBuilderAttribute");
			_immutableAttributeCopyIgnoreAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.ImmutableAttributeCopyIgnoreAttribute");
			_immutableGenerationOptionsAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.ImmutableGenerationOptionsAttribute");

			var generationData = EnumerateImmutableGeneratedEntities()
				.OrderBy(x => x.symbol.Name)
				.ToArray();

			var immutableEntitiesToGenerate = generationData.Select(x => x.Item1).ToArray();

			_copyIgnoreAttributeRegexes =
				ExtractCopyIgnoreAttributes(context.Compilation.Assembly)
					.Concat(new[] {new Regex(@"^Uno\.Immutable"), new Regex(@"^Uno\.Equality")})
					.ToArray();

			_generationOptions = ExtractGenerationOptions(context.Compilation.Assembly);

			_generateOptionCode = _generationOptions.generateOptionCode && context.Compilation.GetTypeByMetadataName("Uno.Option") != null;

			_generateJsonNet = _generationOptions.generateOptionCode && context.Compilation.GetTypeByMetadataName("Newtonsoft.Json.JsonConvert") != null;

			foreach ((var type, var moduleAttribute) in generationData)
			{
				var baseTypeInfo = GetTypeInfo(context, type, immutableEntitiesToGenerate);

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

		private (bool generateOptionCode, bool treatArrayAsImmutable, bool generateEqualityByDefault, bool generateJsonNet) ExtractGenerationOptions(IAssemblySymbol assembly)
		{
			var generateOptionCode = true;
			var treatArrayAsImmutable = false;
			var generateEqualityByDefault = true;
			var generateJsonNet = true;

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
					}
				}
			}

			return (generateOptionCode, treatArrayAsImmutable, generateEqualityByDefault, generateJsonNet);
		}

		private bool GetShouldGenerateEquality(AttributeData attribute)
		{
			var shouldGenerateEquality = attribute.NamedArguments
				.Where(na => na.Key.Equals(nameof(GeneratedImmutableAttribute.GenerateEquality)))
				.Select(na => (bool?) na.Value.Value)
				.FirstOrDefault();

			return shouldGenerateEquality ?? _generationOptions.generateEqualityByDefault;
		}

		private (bool isBaseType, string baseType, string builderBaseType, bool isImmutablePresent) GetTypeInfo(
			SourceGeneratorContext context,
			INamedTypeSymbol type, INamedTypeSymbol[] immutableEntitiesToGenerate)
		{
			var baseType = type.BaseType;
			if (baseType == null || baseType.Equals(_systemObject))
			{
				return (false, null, null, false); // no base type
			}

			// Check if [Immutable] is present on the non-generated partial
			var isImmutablePresent = baseType.FindAttributeFlattened(_immutableAttributeSymbol) != null;

			// Is Builder already compiled ? (in another project/assembly)
			var builderAttribute = baseType.FindAttributeFlattened(_immutableBuilderAttributeSymbol);
			if (builderAttribute != null)
			{
				return (true, null, null, true); // no relevant basetype
			}

			var baseTypeDefinition = baseType.ConstructedFrom ?? baseType;

			// Builder is to be generated (no attribute yet for reaching the builder)
			if (immutableEntitiesToGenerate.Contains(baseTypeDefinition))
			{
				var names = baseType.GetSymbolNames();

				var isSameNamespace = baseType.ContainingNamespace.Equals(type.ContainingNamespace);
				var baseTypeName = isSameNamespace ? names.SymbolNameWithGenerics : baseType.ToDisplayString();
				var builderBaseType = baseTypeName + ".Builder";

				return (true, baseTypeName, builderBaseType, isImmutablePresent);
			}

			return (true, null, null, false); // no relevant basetype
		}

		private void GenerateImmutable(INamedTypeSymbol typeSymbol,
			(bool isBaseType, string baseType, string builderBaseType, bool isImmutablePresent) baseTypeInfo,
			bool generateEquality)
		{
			var defaultMemberName = "Default";

			var generateOption = _generateOptionCode && !typeSymbol.IsAbstract;
			var generateJsonNet = _generateJsonNet && !typeSymbol.IsAbstract;

			var classCopyIgnoreRegexes = ExtractCopyIgnoreAttributes(typeSymbol).ToArray();

			(IPropertySymbol property, bool isNew)[] properties;

			var typeProperties = typeSymbol.GetProperties().ToArray();

			if (baseTypeInfo.isBaseType)
			{
				var baseProperties = typeSymbol.BaseType.GetProperties()
					.Where(x => x.IsReadOnly && IsAutoProperty(x))
					.Select(x => x.Name)
					.ToArray();

				properties = typeProperties
					.Where(x => x.IsReadOnly && IsAutoProperty(x))
					.Select(x => (x, baseProperties.Contains(x.Name))) // remove properties already present in base class
					.ToArray();
			}
			else
			{
				properties = typeProperties
					.Where(x => x.IsReadOnly && IsAutoProperty(x))
					.Select(x => (x, false))
					.ToArray();
			}

			var builder = new IndentedStringBuilder();

			var symbolNames = typeSymbol.GetSymbolNames();
			var (symbolName, genericArguments, symbolNameWithGenerics, symbolNameForXml, symbolNameDefinition, resultFileName, genericConstraints) = symbolNames;

			ValidateType(builder, typeSymbol, baseTypeInfo, symbolNames, typeProperties);

			var newModifier = baseTypeInfo.isBaseType ? "new " : "";

			builder.AppendLineInvariant("using System;");
			builder.AppendLine();
			builder.AppendLineInvariant("// <auto-generated>");
			builder.AppendLineInvariant("// *****************************************************************************************************************");
			builder.AppendLineInvariant("// This has been generated by Uno.CodeGen (ImmutableGenerator), available at https://github.com/nventive/Uno.CodeGen");
			builder.AppendLineInvariant("// *****************************************************************************************************************");
			builder.AppendLineInvariant("// </auto-generated>");
			builder.AppendLine();

			using (builder.BlockInvariant($"namespace {typeSymbol.ContainingNamespace}"))
			{
				string builderTypeNameAndBaseClass;
				if (typeSymbol.IsAbstract)
				{
					builderTypeNameAndBaseClass = baseTypeInfo.isBaseType ? $"Builder : {baseTypeInfo.builderBaseType}" : $"Builder";
				}
				else
				{
					builderTypeNameAndBaseClass = baseTypeInfo.isBaseType
					? $"Builder : {baseTypeInfo.builderBaseType}, Uno.IImmutableBuilder<{symbolNameWithGenerics}>"
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
					using (builder.BlockInvariant(
						$"{typeSymbol.GetAccessibilityAsCSharpCodeString()} {newModifier}partial class {builderTypeNameAndBaseClass}"))
					{
						if (!baseTypeInfo.isBaseType)
						{
							// _isDirty only on base builder (will be reused in derived builders)
							builder.AppendLineInvariant("// Dirty means there's a difference from `_original`.");
							builder.AppendLineInvariant("protected bool _isDirty = false;");
							builder.AppendLine();
							builder.AppendLineInvariant("// This is the original entity, if any (could be null))");
							builder.AppendLineInvariant("{0}", $"internal readonly {symbolNameWithGenerics} _original;");
							builder.AppendLine();
							builder.AppendLineInvariant("// Cached version of generated entity (flushed when the builder is updated)");
							builder.AppendLineInvariant($"protected {symbolNameWithGenerics} _cachedResult = default({symbolNameWithGenerics});");
							builder.AppendLine();

							if (typeSymbol.IsAbstract)
							{
								using (builder.BlockInvariant($"public Builder({symbolNameWithGenerics} original)"))
								{
									builder.AppendLineInvariant($"_original = original ?? throw new ArgumentNullException(nameof(original));");
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
							using (builder.BlockInvariant($"public Builder({symbolNameWithGenerics} original) : base(original ?? {symbolNameWithGenerics}.Default)"))
							{
								builder.AppendLineInvariant($"// Default constructor, the _original field is assigned in base constructor.");
							}

							using (builder.BlockInvariant($"public Builder() : base({symbolNameWithGenerics}.Default)"))
							{
								builder.AppendLineInvariant($"// Default constructor, the _original field is assigned in base constructor.");
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

							builder.AppendLine($@"
// Backing field for property {prop.Name}.
private {prop.Type} _{prop.Name};

// If the property {prop.Name} has been set in the builder.
// `false` means the property hasn't been set or has been reverted to original value `{typeSymbol.Name}.Default.{
									prop.Name
								}`.
private bool _is{prop.Name}Set = false;

/// <summary>
/// Get/Set the current builder value for {prop.Name}.
/// </summary>
/// <remarks>
/// When nothing is set in the builder, the value is `default({prop.Type})`.
/// </remarks>
{attributes}public {newPropertyModifier}{prop.Type} {prop.Name}
{{
	get => _is{prop.Name}Set ? _{prop.Name} : (_original as {symbolNameWithGenerics}).{prop.Name};
	set
	{{
		var originalValue = (({symbolNameWithGenerics})_original).{prop.Name};
		var isSameAsOriginal = global::System.Collections.Generic.EqualityComparer<{
									prop.Type
								}>.Default.Equals(originalValue, value);
		if(isSameAsOriginal)
		{{
			// Property {prop.Name} has been set back to original value
			_is{prop.Name}Set = false;
			_{prop.Name} = default({prop.Type}); // dereference to prevent any leak (when it's a reference type)
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

						if (properties.Any())
						{
							builder.AppendLineInvariant("{0}", $"#region .WithXXX() methods on {symbolNameWithGenerics}.Builder");
							foreach (var (prop, isNew) in properties)
							{
								var newPropertyModifier = isNew ? "new " : "";

								builder.AppendLineInvariant($"/// <summary>");
								builder.AppendLineInvariant($"/// Set property {prop.Name} in a fluent declaration.");
								builder.AppendLineInvariant($"/// </summary>");
								builder.AppendLineInvariant($"/// <remarks>");
								builder.AppendLineInvariant($"/// **THIS METHOD IS NOT THREAD-SAFE** (it shouldn't be accessed concurrently from many threads)");
								builder.AppendLineInvariant($"/// </remarks>");
								builder.AppendLineInvariant("/// <example>");
								builder.AppendLineInvariant("{0}", $"/// var builder = new {symbolNameForXml}.Builder {{ {prop1Name} = xxx, ... }}; // create a builder instance");
								builder.AppendLineInvariant($"/// {prop.Type} new{prop.Name}Value = [...];");
								builder.AppendLineInvariant($"/// {symbolNameForXml} instance = builder.With{prop.Name}(new{prop.Name}Value); // create an immutable instance");
								builder.AppendLineInvariant("/// </example>");
								using (builder.BlockInvariant($"public {newPropertyModifier}Builder With{prop.Name}({prop.Type} value)"))
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
								builder.AppendLineInvariant($"/// {symbolNameForXml} instance = builder.With{prop.Name}(previous{prop.Name}Value => new {prop.Type}(...)); // create an immutable instance");
								builder.AppendLineInvariant("/// </example>");
								using (builder.BlockInvariant($"public {newPropertyModifier}Builder With{prop.Name}(Func<{prop.Type}, {prop.Type}> valueSelector)"))
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

public static Builder FromOption(Uno.Option<{symbolNameWithGenerics}> original)
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
					builder.AppendLineInvariant(
						"/// Application code should prefer the usage of implicit casting which is calling this constructor.");
					builder.AppendLineInvariant("/// </remarks>");
					builder.AppendLineInvariant($"/// <param name=\"builder\">The builder for {symbolNameForXml}.</param>");

					var baseConstructorChaining = baseTypeInfo.isBaseType
						? " : base(builder)"
						: "";

					using (builder.BlockInvariant($"public {symbolName}(Builder builder){baseConstructorChaining}"))
					{
						builder.AppendLineInvariant("if(builder == null) throw new ArgumentNullException(nameof(builder));");

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
				using (builder.BlockInvariant($"{typeSymbol.GetAccessibilityAsCSharpCodeString()} static partial class {symbolName}Extensions"))
				{
					if (properties.Any() && !typeSymbol.IsAbstract)
					{
						var builderName = $"{symbolNameWithGenerics}.Builder";

						builder.AppendLine();
						builder.AppendLineInvariant($"#region .WithXXX() methods on {symbolNameWithGenerics}");
						foreach (var (prop, isNew) in properties)
						{
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
							builder.AppendLineInvariant($"/// {prop.Type} new{prop.Name}Value = [...];");
							builder.AppendLineInvariant($"/// {symbolNameForXml} modified = original.With{prop.Name}(new{prop.Name}Value); // create a new modified immutable instance");
							builder.AppendLineInvariant("/// </example>");
							builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
							using (builder.BlockInvariant($"public static {builderName} With{prop.Name}{genericArguments}(this {symbolNameWithGenerics} entity, {prop.Type} value){genericConstraints}"))
							{
								builder.AppendLineInvariant($"return new {builderName}(entity).With{prop.Name}(value);");
							}

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
							builder.AppendLineInvariant($"/// {symbolNameForXml} modified = original.With{prop.Name}(previous{prop.Name}Value => new {prop.Type}(...)); // create a new modified immutable instance");
							builder.AppendLineInvariant("/// </example>");
							builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
							using (builder.BlockInvariant($"public static {builderName} With{prop.Name}{genericArguments}(this {symbolNameWithGenerics} entity, Func<{prop.Type}, {prop.Type}> valueSelector){genericConstraints}"))
							{
								builder.AppendLineInvariant($"return new {builderName}(entity).With{prop.Name}(valueSelector);");
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
								builder.AppendLineInvariant($"/// {prop.Type} new{prop.Name}Value = [...];");
								builder.AppendLineInvariant($"/// Option&lt;{symbolNameForXml}&gt; modified = original.With{prop.Name}(new{prop.Name}Value); // result type is Option.Some");
								builder.AppendLineInvariant("/// </example>");
								builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
								using (builder.BlockInvariant($"public static {builderName} With{prop.Name}{genericArguments}(this global::Uno.Option<{symbolNameWithGenerics}> optionEntity, {prop.Type} value){genericConstraints}"))
								{
									builder.AppendLineInvariant($"return {builderName}.FromOption(optionEntity).With{prop.Name}(value);");
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
								builder.AppendLineInvariant($"/// {symbolNameForXml} modified = original.With{prop.Name}(previous{prop.Name}Value => new {prop.Type}(...)); // create a new modified immutable instance");
								builder.AppendLineInvariant("/// </example>");
								builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
								using (builder.BlockInvariant($"public static {builderName} With{prop.Name}{genericArguments}(this global::Uno.Option<{symbolNameWithGenerics}> optionEntity, Func<{prop.Type}, {prop.Type}> valueSelector){genericConstraints}"))
								{
									builder.AppendLineInvariant($"return {builderName}.FromOption(optionEntity).With{prop.Name}(valueSelector);");
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
		return ({symbolNameWithGenerics})o;
	}}

	public override bool CanConvert(Type objectType)
	{{
		return objectType == typeof({symbolNameWithGenerics}) || objectType == typeof({symbolNameWithGenerics}.Builder);
	}}
}}");

					builder.AppendLine();
				}
			}

			_context.AddCompilationUnit(resultFileName, builder.ToString());
		}

		private void ValidateType(
			IIndentedStringBuilder builder,
			INamedTypeSymbol typeSymbol,
			(bool isBaseType, string baseType, string builderBaseType, bool isImmutablePresent) baseTypeInfo,
			SymbolNames symbolNames, IPropertySymbol[] typeProperties)
		{
			if (!IsFromPartialDeclaration(typeSymbol))
			{
				builder.AppendLineInvariant(
					$"#warning {nameof(ImmutableGenerator)}: you should add the partial modifier to the class {symbolNames.SymbolNameWithGenerics}.");
			}

			if (typeSymbol.IsValueType)
			{
				builder.AppendLineInvariant(
					$"#error {nameof(ImmutableGenerator)}: Type {symbolNames.SymbolNameWithGenerics} **MUST** be a class, not a struct.");
			}

			if (baseTypeInfo.isBaseType && baseTypeInfo.baseType == null)
			{
				builder.AppendLineInvariant(
					$"#error {nameof(ImmutableGenerator)}: Type {symbolNames.SymbolNameWithGenerics} **MUST** derive from an immutable class.");
			}

			void CheckTypeImmutable(ITypeSymbol type, string typeSource)
			{
				if (type.IsGenericArgument())
				{
					var typeParameter = type as ITypeParameterSymbol;
					if (typeParameter?.ConstraintTypes.Any() ?? false)
					{
						foreach (var constraintType in typeParameter.ConstraintTypes)
						{
							CheckTypeImmutable(constraintType, $"{typeSymbol} / Generic type {typeParameter}:{constraintType}");
						}
					}
					else
					{
						builder.AppendLineInvariant(
							$"#error {nameof(ImmutableGenerator)}: {typeSource} is of generic type {type} which isn't restricted to immutable. You can also make your class abstract.");
					}

					return; // ok
				}

				if (type.FindAttribute(_immutableBuilderAttributeSymbol) != null)
				{
					builder.AppendLineInvariant(
						$"#error {nameof(ImmutableGenerator)}: {typeSource} type {type} IS A BUILDER! It cannot be used in an immutable entity.");
				}
				else if (!type.IsImmutable(_generationOptions.treatArrayAsImmutable))
				{
					if (type is IArrayTypeSymbol)
					{
						builder.AppendLineInvariant(
							$"#error {nameof(ImmutableGenerator)}: {typeSource} type {type} is an array, which is not immutable. You can treat arrays as immutable by setting a global attribute [assembly: Uno.ImmutableGenerationOptions(TreatArrayAsImmutable = true)].");
					}
					else
					{
						builder.AppendLineInvariant(
							$"#error {nameof(ImmutableGenerator)}: {typeSource} type {type} is not immutable. It cannot be used in an immutable entity.");
					}
				}

				if(type is INamedTypeSymbol namedType)
				{
					foreach (var typeArgument in namedType.TypeArguments)
					{
						CheckTypeImmutable(typeArgument, $"{typeSource} (argument type {typeArgument})");
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
					builder.AppendLineInvariant(
						$"#error {nameof(ImmutableGenerator)}: Non-static property {symbolNames.SymbolNameWithGenerics}.{prop.Name} cannot have a setter, even a private one. You must remove it for immutable generation.");
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
					builder.AppendLineInvariant(
						$"#error {nameof(ImmutableGenerator)}: Immutable type {symbolNames.SymbolNameWithGenerics} cannot have a non-static field {field.Name}. You must remove it for immutable generation.");
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

					yield return $"[{attrResult}]{Environment.NewLine}";
				}
			}

			return string.Join("", Enumerate());
		}

		private static MethodInfo _isAutoPropertyGetter;

		private static bool IsAutoProperty(IPropertySymbol symbol)
		{
			if (symbol.IsWithEvents || symbol.IsIndexer || !symbol.IsReadOnly)
			{
				return false;
			}

			while (!Equals(symbol.OriginalDefinition, symbol))
			{
				// In some cases we're dealing with a derived type of `WrappedPropertySymbol`.
				// This code needs to deal with the SourcePropertySymbol from Roslyn,
				// the type containing the `IsAutoProperty` internal member.
				symbol = symbol.OriginalDefinition;
			}

			if (_isAutoPropertyGetter == null)
			{
				var type = symbol.GetType();
				var propertyInfo = type.GetProperty("IsAutoProperty", BindingFlags.Instance | BindingFlags.NonPublic);
				if (propertyInfo == null)
				{
					throw new InvalidOperationException(
						"Unable to find the internal property `IsAutoProperty` on implementation of `IPropertySymbol`. " +
						"Should be on internal class `PropertySymbol`. Maybe you are using an incompatible version of Roslyn.");
				}

				_isAutoPropertyGetter = propertyInfo?.GetMethod;
			}

			var isAuto = _isAutoPropertyGetter.Invoke(symbol, new object[] { });
			return (bool) isAuto;
		}

		private IEnumerable<(INamedTypeSymbol symbol, AttributeData attribute)> EnumerateImmutableGeneratedEntities()
			=> from type in _context.Compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
				let moduleAttribute = type.FindAttributeFlattened(_generatedImmutableAttributeSymbol)
				where moduleAttribute != null
				//where (bool) moduleAttribute.ConstructorArguments[0].Value
				select (type, moduleAttribute);

		private static bool IsFromPartialDeclaration(ISymbol symbol)
		{
			return symbol
				.DeclaringSyntaxReferences
				.Select(reference => reference.GetSyntax(CancellationToken.None))
				.OfType<ClassDeclarationSyntax>()
				.Any(node => node.Modifiers.Any(SyntaxKind.PartialKeyword));
		}
	}
}