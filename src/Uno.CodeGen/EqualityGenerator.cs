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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Equality;
using Uno.Helpers;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;

namespace Uno
{
	/// <summary>
	/// Responsible to generate equality members.
	/// </summary>
	/// <remarks>
	/// The trigger for this generator is <see cref="GeneratedEqualityAttribute"/>.
	/// </remarks>
	[GenerateAfter("Uno.ImmutableGenerator")]
	public class EqualityGenerator : SourceGenerator
	{
		private const int CollectionModeSorted = (int)CollectionComparerMode.Sorted; // this reference won't survive compilation
		private const int CollectionModeUnsorted = (int)CollectionComparerMode.Unsorted; // this reference won't survive compilation

		private const byte StringModeIgnoreCase = (byte)StringComparerMode.IgnoreCase; // this reference won't survive compilation
		private const byte StringModeEmptyEqualsNull = (byte)StringComparerMode.EmptyEqualsNull; // this reference won't survive compilation

		private const byte KeyEqualityAuto = (byte) KeyEqualityMode.Auto; // this reference won't survive compilation
		private const byte KeyEqualityUseEquality = (byte)KeyEqualityMode.UseEquality; // this reference won't survive compilation
		private const byte KeyEqualityUseKeyEquality = (byte)KeyEqualityMode.UseKeyEquality; // this reference won't survive compilation

		private SourceGeneratorContext _context;
		private ISourceGeneratorLogger _logger;

		private INamedTypeSymbol _objectSymbol;
		private INamedTypeSymbol _valueTypeSymbol;
		private INamedTypeSymbol _boolSymbol;
		private INamedTypeSymbol _intSymbol;
		private INamedTypeSymbol _enumSymbol;
		private INamedTypeSymbol _arraySymbol;
		private INamedTypeSymbol _collectionSymbol;
		private INamedTypeSymbol _iEquatableSymbol;
		private INamedTypeSymbol _iKeyEquatableSymbol;
		private INamedTypeSymbol _iKeyEquatableGenericSymbol;
		private INamedTypeSymbol _generatedEqualityAttributeSymbol;
		private INamedTypeSymbol _ignoreForEqualityAttributeSymbol;
		private INamedTypeSymbol _equalityHashAttributeSymbol;
		private INamedTypeSymbol _equalityKeyAttributeSymbol;
		private INamedTypeSymbol _equalityComparerOptionsAttributeSymbol;
		private INamedTypeSymbol _dataAnnonationsKeyAttributeSymbol;
		private bool _isPureAttributePresent;

		private bool _generateKeyEqualityCode;

		private static readonly int[] PrimeNumbers =
		{
			223469, // 19901st prime number
			224743, // 20001st prime number
			225961, // 20101st prime number
			227251, // 20201st prime number
			228479, // 20301st prime number
			229613, // 20401st prime number
			230767, // 20501st prime number
			232007, // 20601st prime number
			233347, // 20701st prime number
			234653, // 20801st prime number
			235919, // 20901st prime number
			237217, // 21001st prime number
			238477, // 21101st prime number
			239737, // 21201st prime number
			240997, // 21301st prime number
			242129, // 21401st prime number
			243437, // 21501st prime number
			244603, // 21601st prime number
			245851, // 21701st prime number
			247067, // 21801st prime number
			248267, // 21901st prime number
			249449, // 22001st prime number
		};

		private string _currentType = "unknown";

		private INamedTypeSymbol GetMandatoryTypeSymbol(string name)
		{
			var s = _context.Compilation.GetTypeByMetadataName(name);
			if (s == null)
				throw new InvalidOperationException($"Invalid type symbol '{name}'");
			return s;
		}

		/// <inheritdoc />
		public override void Execute(SourceGeneratorContext context)
		{
			_context = context;
			_logger = context.GetLogger();

			_objectSymbol = GetMandatoryTypeSymbol("System.Object");
			_valueTypeSymbol = GetMandatoryTypeSymbol("System.ValueType");
			_boolSymbol = GetMandatoryTypeSymbol("System.Boolean");
			_intSymbol = GetMandatoryTypeSymbol("System.Int32");
			_enumSymbol = GetMandatoryTypeSymbol("System.Enum");
			_arraySymbol = GetMandatoryTypeSymbol("System.Array");
			_collectionSymbol = GetMandatoryTypeSymbol("System.Collections.ICollection");
			_iEquatableSymbol = GetMandatoryTypeSymbol("System.IEquatable`1");
			_iKeyEquatableSymbol = context.Compilation.GetTypeByMetadataName("Uno.Equality.IKeyEquatable");
			_iKeyEquatableGenericSymbol = context.Compilation.GetTypeByMetadataName("Uno.Equality.IKeyEquatable`1");
			_generatedEqualityAttributeSymbol = GetMandatoryTypeSymbol("Uno.GeneratedEqualityAttribute");
			_ignoreForEqualityAttributeSymbol = GetMandatoryTypeSymbol("Uno.EqualityIgnoreAttribute");
			_equalityHashAttributeSymbol = GetMandatoryTypeSymbol("Uno.EqualityHashAttribute");
			_equalityKeyAttributeSymbol = GetMandatoryTypeSymbol("Uno.EqualityKeyAttribute");
			_equalityComparerOptionsAttributeSymbol = GetMandatoryTypeSymbol("Uno.Equality.EqualityComparerOptionsAttribute");
			_dataAnnonationsKeyAttributeSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.KeyAttribute");
			_isPureAttributePresent = context.Compilation.GetTypeByMetadataName("System.Diagnostics.Contracts.Pure") != null;

			_generateKeyEqualityCode = _iKeyEquatableSymbol != null;

			foreach (var type in EnumerateEqualityTypesToGenerate())
			{
				GenerateEquality(type);
			}
		}

		private void GenerateEquality(INamedTypeSymbol typeSymbol)
		{
			var builder = new IndentedStringBuilder();

			var symbolNames = typeSymbol.GetSymbolNames();
			var (symbolName, genericArguments, symbolNameWithGenerics, symbolNameForXml, symbolNameDefinition, resultFileName, _) = symbolNames;
			var (equalityMembers, hashMembers, keyEqualityMembers) = GetEqualityMembers(typeSymbol);
			var baseTypeInfo = GetBaseTypeInfo(typeSymbol);
			var generateKeyEquals = baseTypeInfo.baseImplementsKeyEquals || baseTypeInfo.baseImplementsKeyEqualsT || keyEqualityMembers.Any();

			_currentType = symbolNames.GetSymbolFullNameWithGenerics();

			builder.AppendLineInvariant("// <auto-generated>");
			builder.AppendLineInvariant("// **********************************************************************************************************************");
			builder.AppendLineInvariant("// This file has been generated by Uno.CodeGen (ImmutableGenerator), available at https://github.com/unoplatform/Uno.CodeGen");
			builder.AppendLineInvariant("// **********************************************************************************************************************");
			builder.AppendLineInvariant("// </auto-generated>");
			builder.AppendLineInvariant("#pragma warning disable");
			builder.AppendLine();
			builder.AppendLine("using System;");
			builder.AppendLine();

			using (builder.BlockInvariant($"namespace {typeSymbol.ContainingNamespace}"))
			{
				if (!IsFromPartialDeclaration(typeSymbol))
				{
					Warning(builder, $"You should add the partial modifier to the class {symbolNameWithGenerics}.");
				}

				if (baseTypeInfo.isBaseType && !baseTypeInfo.baseOverridesEquals)
				{
					Warning(builder, $"Base type {typeSymbol.BaseType} does not override .Equals() method. It could lead to erroneous results.");
				}
				if (baseTypeInfo.isBaseType && !baseTypeInfo.baseOverridesGetHashCode)
				{
					Warning(builder, $"Base type {typeSymbol.BaseType} does not override .GetHashCode() method. It could lead to erroneous results.");
				}

				if (generateKeyEquals && !_generateKeyEqualityCode)
				{
					Warning(builder, "To use the `KeyEquality` features, you need to add a reference to `Uno.Core` package. https://www.nuget.org/packages/Uno.Core/");
					generateKeyEquals = false;
				}

				var classOrStruct = typeSymbol.IsReferenceType ? "class" : "struct";

				var keyEqualsInterfaces = generateKeyEquals
					? $", global::Uno.Equality.IKeyEquatable<{symbolNameWithGenerics}>, global::Uno.Equality.IKeyEquatable"
					: "";

				using (builder.BlockInvariant($"{typeSymbol.GetAccessibilityAsCSharpCodeString()} partial {classOrStruct} {symbolNameWithGenerics} : IEquatable<{symbolNameWithGenerics}>{keyEqualsInterfaces}"))
				{
					builder.AppendLineInvariant("/// <summary>");
					builder.AppendLineInvariant($"/// Checks two instances of {symbolNameForXml} for equality.");
					builder.AppendLineInvariant("/// </summary>");
					builder.AppendLineInvariant("/// <remarks>");
					builder.AppendLineInvariant("/// You can also simply use the overriden '==' and '!=' operators.");
					builder.AppendLineInvariant("/// </remarks>");
					if (_isPureAttributePresent)
					{
						builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
					}

					using (builder.BlockInvariant($"public static bool Equals({symbolNameWithGenerics} a, {symbolNameWithGenerics} b)"))
					{
						if (typeSymbol.IsReferenceType)
						{
							builder.AppendLineInvariant("if (ReferenceEquals(a, b)) return true; // Same instance or both null");
							using (builder.BlockInvariant("if (ReferenceEquals(null, a))"))
							{
								builder.AppendLineInvariant("return ReferenceEquals(null, b);");
							}
							builder.AppendLineInvariant("return !ReferenceEquals(null, b) && a.InnerEquals(b);");
						}
						else
						{
							builder.AppendLineInvariant("return a.InnerEquals(b);");
						}
					}

					builder.AppendLine();

					builder.AppendLineInvariant("/// <inheritdoc />");
					if (_isPureAttributePresent)
					{
						builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
					}
					using (builder.BlockInvariant($"public bool Equals({symbolNameWithGenerics} other) // Implementation of `IEquatable<{symbolNameWithGenerics}>.Equals()`"))
					{
						if (typeSymbol.IsReferenceType)
						{
							builder.AppendLineInvariant("if (ReferenceEquals(this, other)) return true;");
							builder.AppendLineInvariant("if (ReferenceEquals(null, other)) return false;");
						}
						builder.AppendLineInvariant("return InnerEquals(other);");
					}

					builder.AppendLine();

					builder.AppendLineInvariant("/// <inheritdoc />");
					if (_isPureAttributePresent)
					{
						builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
					}
					using (builder.BlockInvariant("public override bool Equals(object other)  // This one from `System.Object.Equals()`"))
					{
						if (typeSymbol.IsReferenceType)
						{
							builder.AppendLineInvariant($"return Equals(other as {symbolNameWithGenerics});");
						}
						else
						{
							builder.AppendLineInvariant($"return other is {symbolNameWithGenerics} ? Equals(({symbolNameWithGenerics})other) : false;");
						}
					}

					builder.AppendLine();

					builder.AppendLineInvariant("#region \"InnerEquals\" Method -- THIS IS WHERE EQUALITY IS CHECKED");

					builder.AppendLineInvariant("// private method doing the real .Equals() job");
					using (builder.BlockInvariant($"private bool InnerEquals({symbolNameWithGenerics} other)"))
					{
						builder.AppendLineInvariant("if (other.GetType() != GetType()) return false;");
						builder.AppendLineInvariant("if (other.GetHashCode() != GetHashCode()) return false;");

						var baseCall = baseTypeInfo.baseOverridesEquals
							? "base.Equals(other)"
							: null;

						GenerateEqualLogic(typeSymbol, builder, equalityMembers, baseCall);
					}

					builder.AppendLineInvariant("#endregion");
					builder.AppendLine();

					builder.AppendLineInvariant("/// <inheritdoc />");
					using (builder.BlockInvariant($"public static bool operator ==({symbolNameWithGenerics} a, {symbolNameWithGenerics} b)"))
					{
						builder.AppendLineInvariant("return Equals(a, b);");
					}

					builder.AppendLine();

					builder.AppendLineInvariant("/// <inheritdoc />");
					using (builder.BlockInvariant($"public static bool operator !=({symbolNameWithGenerics} a, {symbolNameWithGenerics} b)"))
					{
						builder.AppendLineInvariant("return !Equals(a, b);");
					}

					builder.AppendLine();
					builder.AppendLineInvariant("#region \".GetHashCode()\" Section -- THIS IS WHERE HASH CODE IS COMPUTED");

					builder.AppendLineInvariant("/// <inheritdoc />");
					if (_isPureAttributePresent)
					{
						builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
					}
					using (builder.BlockInvariant("public override int GetHashCode()"))
					{
						builder.AppendLineInvariant("#pragma warning disable CS0171");
						builder.AppendLineInvariant("return _computedHashCode ?? (int)(_computedHashCode = ComputeHashCode());");
						builder.AppendLineInvariant("#pragma warning restore CS0171");
					}

					builder.AppendLine();

					builder.AppendLineInvariant("private int? _computedHashCode;");

					builder.AppendLine();

					using (builder.BlockInvariant("private int ComputeHashCode()"))
					{
						var baseCall = baseTypeInfo.baseOverridesGetHashCode
							? "base.GetHashCode()"
							: null;
						GenerateHashLogic(typeSymbol, builder, hashMembers, baseCall);
					}

					builder.AppendLineInvariant("#endregion");
					builder.AppendLine();

					if (generateKeyEquals)
					{
						builder.AppendLineInvariant("#region \"Key Equality\" Section -- THIS IS WHERE KEY EQUALS IS DONE + KEY HASH CODE IS COMPUTED");

						builder.AppendLineInvariant("/// <inheritdoc />");
						if (_isPureAttributePresent)
						{
							builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
						}
						using (builder.BlockInvariant("public bool KeyEquals(object other)"))
						{
							if (typeSymbol.IsReferenceType)
							{
								builder.AppendLineInvariant($"return KeyEquals(other as {symbolNameWithGenerics});");
							}
							else
							{
								builder.AppendLineInvariant($"return other is {symbolNameWithGenerics} ? KeyEquals(({symbolNameWithGenerics})other) : false;");
							}
						}
						builder.AppendLine();
						using (builder.BlockInvariant($"public bool KeyEquals({symbolNameWithGenerics} other)"))
						{
							if (typeSymbol.IsReferenceType)
							{
								builder.AppendLineInvariant("if (ReferenceEquals(this, other)) return true;");
								builder.AppendLineInvariant("if (ReferenceEquals(null, other)) return false;");
							}

							builder.AppendLineInvariant("return InnerKeyEquals(other);");
						}

						builder.AppendLine();

						builder.AppendLineInvariant("// private method doing the real .KeyEquals() job");
						using (builder.BlockInvariant($"private bool InnerKeyEquals({symbolNameWithGenerics} other)"))
						{
							builder.AppendLineInvariant("if (other.GetKeyHashCode() != GetKeyHashCode()) return false;");

							var baseCall = baseTypeInfo.baseImplementsKeyEquals || baseTypeInfo.baseImplementsKeyEqualsT
								? "base.KeyEquals(other)"
								: null;

							GenerateEqualLogic(typeSymbol, builder, keyEqualityMembers, baseCall);
						}

						builder.AppendLine();

						builder.AppendLineInvariant("/// <inheritdoc />");
						if (_isPureAttributePresent)
						{
							builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
						}
						using (builder.BlockInvariant("public int GetKeyHashCode()"))
						{
							builder.AppendLineInvariant("return _computedKeyHashCode ?? (int)(_computedKeyHashCode = ComputeKeyHashCode());");
						}

						builder.AppendLine();

						using (builder.BlockInvariant("private int ComputeKeyHashCode()"))
						{
							var baseCall = baseTypeInfo.baseImplementsKeyEquals || baseTypeInfo.baseImplementsKeyEqualsT
								? "base.GetKeyHashCode()"
								: null;
							GenerateHashLogic(typeSymbol, builder, keyEqualityMembers, baseCall);
						}

						builder.AppendLine();

						builder.AppendLineInvariant("private int? _computedKeyHashCode;");

						builder.AppendLineInvariant("#endregion");
						builder.AppendLine();
					}
				}
			}

			_context.AddCompilationUnit(resultFileName, builder.ToString());
		}

		private (bool isBaseType, bool baseOverridesGetHashCode, bool baseOverridesEquals, bool baseImplementsIEquatable, bool baseImplementsKeyEquals, bool baseImplementsKeyEqualsT)
			GetBaseTypeInfo(INamedTypeSymbol typeSymbol)
		{
			var baseType = typeSymbol.BaseType;
			if (baseType.Equals(_objectSymbol) || baseType.Equals(_valueTypeSymbol))
			{
				return (false, false, false, false, false, false);
			}

			var isBaseTypeInSources = baseType.Locations.Any(l => l.IsInSource);

			var baseTypeWillBeGenerated = isBaseTypeInSources
				&& baseType.FindAttributeFlattened(_generatedEqualityAttributeSymbol) != null;

			var baseOverridesGetHashCode = baseTypeWillBeGenerated
				|| baseType.GetMethods()
					.Any(m => m.Name.Equals("GetHashCode")
						&& !m.IsStatic
						&& m.IsOverride
						&& m.ReturnType.Equals(_intSymbol)
						&& m.Parameters.Length == 0);

			var baseOverridesEquals = baseTypeWillBeGenerated
				|| baseType.GetMethods()
					.Any(m => m.Name.Equals("Equals")
						&& !m.IsStatic
						&& m.IsOverride
						&& m.ReturnType.Equals(_boolSymbol)
						&& m.Parameters.Length == 1
						&& m.Parameters[0].Type.Equals(_objectSymbol));

			var baseImplementsIEquatable = baseTypeWillBeGenerated
				|| baseType
					.Interfaces
					.Any(i => i.OriginalDefinition.Equals(_iEquatableSymbol));

			var baseImplementsKeyEquals = baseType
				.Interfaces
				.Any(i => i.OriginalDefinition.Equals(_iKeyEquatableSymbol));

			var baseImplementsKeyEqualsT = baseType
				.Interfaces
				.Any(i => i.OriginalDefinition.Equals(_iKeyEquatableGenericSymbol));

			return (true, baseOverridesGetHashCode, baseOverridesEquals, baseImplementsIEquatable, baseImplementsKeyEquals, baseImplementsKeyEqualsT);
		}

		private void GenerateEqualLogic(INamedTypeSymbol typeSymbol, IndentedStringBuilder builder, (ISymbol, byte)[] equalityMembers, string baseCall)
		{
			if (baseCall == null && equalityMembers.Length == 0)
			{
				Warning(builder, "No fields or properties used for equality check.");
			}

			foreach (var (member, equalityMode) in equalityMembers)
			{
				var type = (member as IFieldSymbol)?.Type ?? (member as IPropertySymbol)?.Type;
				if (type == null)
				{
					continue;
				}
				var (_, customComparerProperty) = GetCustomsForMembers(typeSymbol, member, type);

				var typeFullName = type.GetSymbolNames()?.GetSymbolFullNameWithGenerics() ?? type.ToString();

				builder.AppendLine();

				if (customComparerProperty == null)
				{
					if (type.IsDictionary(out var keyType, out var valueType, out var isReadonlyDictionary))
					{
						var comparer = isReadonlyDictionary
							? "ReadonlyDictionaryEqualityComparer"
							: "DictionaryEqualityComparer";

						var keyTypeFullName = keyType.GetSymbolNames()?.GetSymbolFullNameWithGenerics() ?? keyType.ToString();
						var valueTypeFullName = valueType.GetSymbolNames()?.GetSymbolFullNameWithGenerics() ?? valueType.ToString();

						using (builder.BlockInvariant($"if(!global::Uno.Equality.{comparer}<{typeFullName}, {keyTypeFullName}, {valueTypeFullName}>.Default.Equals({member.Name}, other.{member.Name}))"))
						{
							builder.AppendLineInvariant($"return false; // {member.Name} not equal");
						}
					}
					else if (type.IsCollection(out var elementType, out var isReadonlyCollection, out var isOrdered))
					{
						var elementTypeFullName = elementType.GetSymbolNames()?.GetSymbolFullNameWithGenerics() ?? elementType.ToString();

						// Extract mode from attribute, or default following the type of the collection (if ordered)
						var optionsAttribute = member.FindAttribute(_equalityComparerOptionsAttributeSymbol);
						var mode = (int)(optionsAttribute
								?.NamedArguments
								.FirstOrDefault(na=>na.Key.Equals(nameof(EqualityComparerOptionsAttribute.CollectionMode)))
								.Value.Value
							?? (isOrdered ? CollectionModeSorted : CollectionModeUnsorted));

						var comparer = (mode & CollectionModeSorted) == CollectionModeSorted
							? (isReadonlyCollection ? "SortedReadonlyCollectionEqualityComparer" : "SortedCollectionEqualityComparer")
							: (isReadonlyCollection ? "UnsortedReadonlyCollectionEqualityComparer" : "UnsortedCollectionEqualityComparer");

						if (optionsAttribute == null && mode == CollectionModeSorted)
						{
							// We just show that if the collection is "sorted", because "unsorted" will always work and we don't want
							// to do a "SequenceEquals" on an non-ordered structure.
							builder.AppendLineInvariant($"// **{member.Name}** To use an _unsorted_ comparer, add the following attribute to your member:");
							builder.AppendLineInvariant($"// [{nameof(EqualityComparerOptionsAttribute)}({nameof(CollectionComparerMode)}.{nameof(CollectionComparerMode.Unsorted)})]");
						}

						using (builder.BlockInvariant($"if(!global::Uno.Equality.{comparer}<{typeFullName}, {elementTypeFullName}>.Default.Equals({member.Name}, other.{member.Name}))"))
						{
							builder.AppendLineInvariant($"return false; // {member.Name} not equal");
						}
					}
					else
					{
						builder.AppendLineInvariant($"// **{member.Name}** You can define a custom comparer for {member.Name} and it will be used.");
						builder.AppendLineInvariant($"// CUSTOM COMPARER>> private static IEqualityComparer<{type}> {GetCustomComparerPropertyName(member)} => <custom comparer>;");
						builder.AppendLineInvariant($"// If you don't want {member.Name} to be used for equality check, add the [Uno.EqualityIgnore] attribute on it.");

						if (type.SpecialType == SpecialType.System_String)
						{
							// Extract mode from attribute, or default following the type of the collection (if ordered)
							var mode = GetStringMode(member);

							var comparer = (mode & StringModeIgnoreCase) == StringModeIgnoreCase
								? "global::System.StringComparer.OrdinalIgnoreCase"
								: "global::System.StringComparer.Ordinal";

							var emptyEqualsNull = (mode & StringModeEmptyEqualsNull) == StringModeEmptyEqualsNull;
							var emptyCheck = emptyEqualsNull
								? $"(string.IsNullOrWhiteSpace({member.Name}) != string.IsNullOrWhiteSpace(other.{member.Name})) || !string.IsNullOrWhiteSpace({member.Name}) && "
								: "";
							var nullCoalescing = emptyEqualsNull ? " ?? \"\" " : "";

							builder.AppendLineInvariant("// STRING>> You can fine-tune the string equality by adding the following attribute on your member:");
							builder.AppendLineInvariant("//   [EqualityComparerOptions(StringMode=<flags>)]");
							builder.AppendLineInvariant($"//   Available flags: {nameof(StringComparerMode)}.IgnoreCase and {nameof(StringComparerMode)}.EmptyEqualsNull");

							using (builder.BlockInvariant($"if({emptyCheck}!{comparer}.Equals({member.Name}{nullCoalescing}, other.{member.Name}{nullCoalescing}))"))
							{
								builder.AppendLineInvariant($"return false; // {member.Name} not equal");
							}
						}
						else if (type.IsPrimitive())
						{
							using (builder.BlockInvariant($"if({member.Name} != other.{member.Name})"))
							{
								builder.AppendLineInvariant($"return false; // {member.Name} not equal");
							}
						}
						else
						{
							if (equalityMode == KeyEqualityUseEquality)
							{
								using (builder.BlockInvariant($"if(!global::System.Collections.Generic.EqualityComparer<{typeFullName}>.Default.Equals({member.Name}, other.{member.Name}))"))
								{
									builder.AppendLineInvariant($"return false; // {member.Name} not equal");
								}
							}
							else
							{
								if(type.IsReferenceType)
								{ 
									using (builder.BlockInvariant($"if(!((global::Uno.Equality.IKeyEquatable){member.Name})?.KeyEquals(other.{member.Name}) ?? other.{member.Name} != null)"))
									{
										builder.AppendLineInvariant($"return false; // {member.Name} not equal");
									}
								}
								else
								{
									using (builder.BlockInvariant($"if(!((global::Uno.Equality.IKeyEquatable){member.Name}).KeyEquals(other.{member.Name}))"))
									{
										builder.AppendLineInvariant($"return false; // {member.Name} not equal");
									}
								}
							}
						}

					}
				}
				else
				{
					builder.AppendLineInvariant($"// **{member.Name}** using custom comparer provided by `{customComparerProperty.Name}()` {customComparerProperty.Locations.FirstOrDefault()}");
					using (builder.BlockInvariant($"if(!{customComparerProperty.Name}.Equals({member.Name}, other.{member.Name}))"))
					{
						builder.AppendLineInvariant($"return false; // {member.Name} not equal");
					}
				}
			}
			if (baseCall != null)
			{
				builder.AppendLineInvariant($"return {baseCall}; // no differences found, check with base");
			}
			else
			{
				builder.AppendLineInvariant("return true; // no differences found");
			}
		}

		private byte GetStringMode(ISymbol member)
		{
			var optionsAttribute = member.FindAttribute(_equalityComparerOptionsAttributeSymbol);
			var mode = (byte) (optionsAttribute
				                   ?.NamedArguments
				                   .FirstOrDefault(na => na.Key.Equals(nameof(EqualityComparerOptionsAttribute.StringMode)))
				                   .Value.Value
			                   ?? default(byte));
			return mode;
		}

		private void GenerateHashLogic(INamedTypeSymbol typeSymbol, IndentedStringBuilder builder, (ISymbol, byte)[] hashMembers, string baseCall)
		{
			if (baseCall == null && hashMembers.Length == 0)
			{
				Warning(
					builder,
					"There is no members marked with [Uno.EqualityHash] or [Uno.EqualityKey]. " +
					"You should add at least one. Documentation: https://github.com/unoplatform/Uno.CodeGen/blob/master/doc/Equality%20Generation.md");

				builder.AppendLineInvariant("return 0; // no members to compute hash");
			}
			else
			{
				if (baseCall != null)
				{
					builder.AppendLineInvariant($"int hash = {baseCall}; // start with hash from base");
				}
				else
				{
					builder.AppendLineInvariant("int hash = 104729; // 10 000th prime number");
				}
				using (builder.BlockInvariant("unchecked"))
				{
					for (var i = 0; i < hashMembers.Length; i++)
					{
						var (member, equalityMode) = hashMembers[i];
						var primeNumber = PrimeNumbers[i % PrimeNumbers.Length];
						var primeNumberRank = 19900 + i;

						var type = (member as IFieldSymbol)?.Type ?? (member as IPropertySymbol)?.Type;
						if (type == null)
						{
							continue;
						}

						builder.AppendLine();
						builder.AppendLineInvariant($"// ***** Computation for {member.Name} ({type}) *****");

						var (customHashMethod, customComparerProperty) = GetCustomsForMembers(typeSymbol, member, type);

						if (customHashMethod == null)
						{
							builder.AppendLineInvariant($"// **{member.Name}** You can define a custom hash computation by creating a method with the following signature:");
							builder.AppendLineInvariant($"// CUSTOM HASH METHOD>> private static int {GetCustomHashMethodName(member)}({type} value) => <custom code>;");
							if (customComparerProperty == null)
							{
								builder.AppendLineInvariant($"// ** You can also define a custom comparer for {member.Name} and it will be used to compute the hash:");
								builder.AppendLineInvariant($"// CUSTOM COMPARER>> private static IEqualityComparer<{type}> {GetCustomComparerPropertyName(member)} => <custom comparer>;");
							}
						}
						else if(customComparerProperty == null)
						{
							builder.AppendLineInvariant($"// **{member.Name}** You can define a custom comparer for {member.Name} and it will be used to compute the hash.");
							builder.AppendLineInvariant($"// CUSTOM COMPARER>> private static IEqualityComparer<{type}> {GetCustomComparerPropertyName(member)} => <custom comparer>;");
						}

						var definition = type.GetDefinitionType();

						string getHashCode;
						if (customHashMethod != null)
						{
							getHashCode = $"{customHashMethod.Name}({member.Name})";
						}
						else if (customComparerProperty != null)
						{
							getHashCode = $"{customComparerProperty.Name}.GetHashCode({member.Name})";
						}
						else if (definition.Equals(_boolSymbol))
						{
							getHashCode = $"({member.Name} ? 1 : 0)";
						}
						else if (definition.Equals(_intSymbol))
						{
							getHashCode = $"{member.Name}";
						}
						else if (definition.DerivesFromType(_arraySymbol))
						{
							getHashCode = $"{member.Name}.Length";
						}
						else if (type.IsDictionary(out var dictionaryKeyType, out var dictionaryValueType, out var isReadonlyDictionary))
						{
							getHashCode = isReadonlyDictionary
								? $"((global::System.Collections.Generic.IReadOnlyDictionary<{dictionaryKeyType}, {dictionaryValueType}>){member.Name}).Count"
								: $"((global::System.Collections.Generic.IDictionary<{dictionaryKeyType}, {dictionaryValueType}>){member.Name}).Count";
						}
						else if (type.IsCollection(out var collectionElementType, out var isReadonlyCollection, out var _))
						{
							getHashCode = isReadonlyCollection
								? $"((global::System.Collections.Generic.IReadOnlyCollection<{collectionElementType}>){member.Name}).Count"
								: $"((global::System.Collections.Generic.ICollection<{collectionElementType}>){member.Name}).Count";
						}
						else if (definition.Equals(_collectionSymbol)
							|| definition.DerivesFromType(_collectionSymbol))
						{
							getHashCode = $"((global::System.Collections.ICollection){member.Name}).Count";
						}
						else if (equalityMode == KeyEqualityUseKeyEquality)
						{
							getHashCode = $"((global::Uno.Equality.IKeyEquatable){member.Name}).GetKeyHashCode()";
						}
						else if (type.BaseType == _enumSymbol)
						{
							getHashCode = $"{member.Name}.GetHashCode()";
						}
						else if (type.SpecialType == SpecialType.System_String)
						{
							var mode = GetStringMode(member);

							getHashCode = (mode & StringModeIgnoreCase) == StringModeIgnoreCase
								? $"global::System.StringComparer.OrdinalIgnoreCase.GetHashCode({member.Name})"
								: $"global::System.StringComparer.Ordinal.GetHashCode({member.Name})";
						}
						else
						{
							var isGeneratedEquality = type.FindAttribute(_generatedEqualityAttributeSymbol) != null;
							if (!isGeneratedEquality)
							{
								var getHashCodeMember = definition
									.GetMembers("GetHashCode")
									.OfType<IMethodSymbol>()
									.Where(m => !m.IsStatic)
									.Where(m => m.IsOverride)
									.Where(m => !m.IsAbstract)
									.FirstOrDefault();

								if (getHashCodeMember == null)
								{
									Warning(
										builder,
										$"Type `{type.GetDisplayFriendlyName()}` of member `{member.Name}` " +
										"doesn't implements .GetHashCode(): it won't be used for hash computation. " +
										$"If you can't change the type {type}, you should use a custom hash method or " +
										"a custom comparer. Alternatively, use something else for hash computation.");
									continue;
								}
							}

							getHashCode = $"{member.Name}.GetHashCode()";
						}

						if (type.IsReferenceType)
						{
							var emptyEqualsNull = false;

							if (type.SpecialType == SpecialType.System_String)
							{
								var mode = GetStringMode(member);
								emptyEqualsNull = (mode & StringModeEmptyEqualsNull) == StringModeEmptyEqualsNull;
							}

							var nullCheckCode = emptyEqualsNull
								? $"!string.IsNullOrWhiteSpace({member.Name})"
								: $"!ReferenceEquals({member.Name}, null)";

							using (builder.BlockInvariant($"if ({nullCheckCode})"))
							{
								builder.AppendLineInvariant($"hash = ({getHashCode} * {primeNumber}) ^ hash; // {primeNumber} is the {primeNumberRank}th prime number");
							}
						}
						else
						{
							builder.AppendLineInvariant($"hash = ({getHashCode} * {primeNumber}) ^ hash; // {primeNumber} is the {primeNumberRank}th prime number");
						}
					}
				}
				builder.AppendLineInvariant("return hash;");
			}
		}

		private (IMethodSymbol customHashMethod, IPropertySymbol customComparer) GetCustomsForMembers(INamedTypeSymbol typeSymbol, ISymbol forSymbol, ITypeSymbol symbolType)
		{
			var customHashMethodName = GetCustomHashMethodName(forSymbol);
			var customHashMethod = typeSymbol
				.GetMethods()
				.FirstOrDefault(m => m.Name.Equals(customHashMethodName)
					&& m.IsStatic
					&& m.DeclaredAccessibility == Accessibility.Private
					&& m.ReturnType.Equals(_intSymbol)
					&& m.Parameters.Length == 1
					&& m.Parameters[0].Type.Equals(symbolType));

			var customComparerPropertyName = GetCustomComparerPropertyName(forSymbol);
			var customComparerProperty = typeSymbol
				.GetProperties()
				.FirstOrDefault(p => p.Name.Equals(customComparerPropertyName)
					&& p.IsStatic
					&& p.DeclaredAccessibility == Accessibility.Private
					&& p.IsReadOnly
					&& p.Type.Name.Equals("IEqualityComparer"));

			return (customHashMethod, customComparerProperty);
		}

		private static string GetCustomHashMethodName(ISymbol forSymbol) => $"GetHash_{forSymbol.Name}";

		private static string GetCustomComparerPropertyName(ISymbol forSymbol) => $"{forSymbol.Name}_CustomComparer";

		private IEnumerable<INamedTypeSymbol> EnumerateEqualityTypesToGenerate()
			=> from type in _context.Compilation.SourceModule.GlobalNamespace.GetNamespaceTypes()
				let moduleAttribute = type.FindAttributeFlattened(_generatedEqualityAttributeSymbol)
				where moduleAttribute != null
				select type;

		private ((ISymbol, byte)[] equalitymembers, (ISymbol, byte)[] hashMembers, (ISymbol, byte)[] keyEqualityMembers) GetEqualityMembers(INamedTypeSymbol typeSymbol)
		{
			var properties =
				from property in typeSymbol.GetProperties()
				where !property.IsWriteOnly
				where !property.IsStatic
				where !property.IsImplicitlyDeclared
				where property.GetMethod.DeclaredAccessibility > Accessibility.Private
				where !property.IsIndexer
				select (symbol: (ISymbol)property, type: property.Type);

			var fields =
				from field in typeSymbol.GetFields()
				where !field.IsStatic
				where !field.IsImplicitlyDeclared
				where field.DeclaredAccessibility > Accessibility.Private
				select (symbol: (ISymbol)field, type: field.Type);

			var equalityMembers = new List<(ISymbol, byte)>();
			var hashMembers = new List<(ISymbol, byte)>();
			var keyEqualityMembers = new List<(ISymbol, byte)>();

			foreach (var (symbol, type) in properties.Concat(fields))
			{
				var symbolAttributes = symbol.GetAttributes();
				var typeAttributes = type.GetAttributes();

				if (symbolAttributes.Any(a => a.AttributeClass.Equals(_ignoreForEqualityAttributeSymbol))
					|| typeAttributes.Any(a => a.AttributeClass.Equals(_ignoreForEqualityAttributeSymbol)))
				{
					continue; // [EqualityIgnore] on the member or the type itself: this member is ignored
				}

				equalityMembers.Add((symbol, KeyEqualityUseEquality));

				if (symbolAttributes.Any(a =>
					a.AttributeClass.Equals(_equalityKeyAttributeSymbol)
					|| a.AttributeClass.Equals(_dataAnnonationsKeyAttributeSymbol)))
				{
					var mode = KeyEqualityAuto;
					var equalityKeyAttr = symbolAttributes.FirstOrDefault(a => a.AttributeClass.Equals(_equalityKeyAttributeSymbol));
					if (equalityKeyAttr != null)
					{
						mode = Convert.ToByte(equalityKeyAttr.ConstructorArguments[0].Value);
					}

					if (mode == KeyEqualityAuto)
					{
						mode = IsTypeKeyEquatable(type) ? KeyEqualityUseKeyEquality : KeyEqualityUseEquality;
					}

					// [EqualityKey] on the member: this member is used for both key & hash
					hashMembers.Add((symbol, KeyEqualityUseEquality));
					keyEqualityMembers.Add((symbol, mode));
				}
				else if (symbolAttributes.Any(a => a.AttributeClass.Equals(_equalityHashAttributeSymbol)))
				{
					// [EqualityHash] on the member: this member is used for hash computation
					hashMembers.Add((symbol, KeyEqualityUseEquality));
				}
			}

			return (equalityMembers.ToArray(), hashMembers.ToArray(), keyEqualityMembers.ToArray());
		}

		private bool IsTypeKeyEquatable(ITypeSymbol type)
		{
			foreach (var i in type.Interfaces)
			{
				if (i.Equals(_iKeyEquatableSymbol) || i.GetDefinitionType().Equals(_iKeyEquatableGenericSymbol))
				{
					return true;
				}
			}

			foreach (var m in type.GetMembers())
			{
				if (m is IPropertySymbol p)
				{
					if (p.GetAttributes().Any(a => a.AttributeClass.Equals(_equalityKeyAttributeSymbol) || a.AttributeClass.Equals(_dataAnnonationsKeyAttributeSymbol)))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsFromPartialDeclaration(INamedTypeSymbol symbol)
		{
			if(symbol.IsReferenceType)
			{
				return symbol
					.DeclaringSyntaxReferences
					.Select(reference => reference.GetSyntax(CancellationToken.None))
					.OfType<ClassDeclarationSyntax>()
					.Any(node => node.Modifiers.Any(SyntaxKind.PartialKeyword));
			}
			else
			{
				return symbol
					.DeclaringSyntaxReferences
					.Select(reference => reference.GetSyntax(CancellationToken.None))
					.OfType<StructDeclarationSyntax>()
					.Any(node => node.Modifiers.Any(SyntaxKind.PartialKeyword));
			}
		}

		private void Warning(IIndentedStringBuilder builder, string warningMsg)
		{
			var msg = $"{nameof(EqualityGenerator)}/{_currentType}: {warningMsg}";
			builder.AppendLineInvariant("#warning " + msg.Replace('\n', ' ').Replace('\r', ' '));
			_logger.Warn(msg);
		}
	}
}
