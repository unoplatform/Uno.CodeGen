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
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.Helpers;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;

namespace Uno
{
	[GenerateAfter("Uno.ImmutableGenerator")]
	public class EqualityGenerator : SourceGenerator
	{
		private INamedTypeSymbol _objectSymbol;
		private INamedTypeSymbol _valueTypeSymbol;
		private INamedTypeSymbol _boolSymbol;
		private INamedTypeSymbol _intSymbol;
		private INamedTypeSymbol _arraySymbol;
		private INamedTypeSymbol _collectionSymbol;
		private INamedTypeSymbol _collectionGenericSymbol;
		private INamedTypeSymbol _iEquatableSymbol;
		private INamedTypeSymbol _iKeyEquatableSymbol;
		private INamedTypeSymbol _iKeyEquatableGenericSymbol;
		private INamedTypeSymbol _generatedEqualityAttributeSymbol;
		private INamedTypeSymbol _ignoreForEqualityAttributeSymbol;
		private INamedTypeSymbol _equalityHashCodeAttributeSymbol;
		private INamedTypeSymbol _equalityKeyCodeAttributeSymbol;
		private INamedTypeSymbol _dataAnnonationsKeyAttributeSymbol;
		private SourceGeneratorContext _context;

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

		public override void Execute(SourceGeneratorContext context)
		{
			 _context = context;
			_objectSymbol = context.Compilation.GetTypeByMetadataName("System.Object");
			_valueTypeSymbol = context.Compilation.GetTypeByMetadataName("System.ValueType");
			_boolSymbol = context.Compilation.GetTypeByMetadataName("System.Bool");
			_intSymbol = context.Compilation.GetTypeByMetadataName("System.Int32");
			_arraySymbol = context.Compilation.GetTypeByMetadataName("System.Array");
			_collectionSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.ICollection");
			_collectionGenericSymbol = context.Compilation.GetTypeByMetadataName("System.Collections.Generic.ICollection`1");
			_iEquatableSymbol = context.Compilation.GetTypeByMetadataName("System.IEquatable`1");
			_iKeyEquatableSymbol = context.Compilation.GetTypeByMetadataName("Uno.Equality.IKeyEquatable");
			_iKeyEquatableGenericSymbol = context.Compilation.GetTypeByMetadataName("Uno.Equality.IKeyEquatable`1");
			_generatedEqualityAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.GeneratedEqualityAttribute");
			_ignoreForEqualityAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.EqualityIgnoreAttribute");
			_equalityHashCodeAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.EqualityHashAttribute");
			_equalityKeyCodeAttributeSymbol = context.Compilation.GetTypeByMetadataName("Uno.EqualityKeyAttribute");
			_dataAnnonationsKeyAttributeSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.DataAnnotations.KeyAttribute");

			foreach (var type in EnumerateEqualityTypesToGenerate())
			{
				GenerateEquality(type);
			}
		}

		private void GenerateEquality(INamedTypeSymbol typeSymbol)
		{
			var builder = new IndentedStringBuilder();

			var (symbolName, symbolNameWithGenerics, symbolNameForXml, symbolNameDefinition, resultFileName) = typeSymbol.GetSymbolNames();
			var (equalityMembers, hashMembers, keyEqualityMembers) = GetEqualityMembers(typeSymbol);
			var baseTypeInfo = GetBaseTypeInfo(typeSymbol);
			var generateKeyEquals = baseTypeInfo.baseImplementsKeyEquals || baseTypeInfo.baseImplementsKeyEqualsT || keyEqualityMembers.Any();

			builder.AppendLine("using System;");
			builder.AppendLine();
			builder.AppendLineInvariant("// <autogenerated>");
			builder.AppendLineInvariant("// ****************************************************************************************************************");
			builder.AppendLineInvariant("// This has been generated by Uno.CodeGen (EqualityGenerator), available at https://github.com/nventive/Uno.CodeGen");
			builder.AppendLineInvariant("// ****************************************************************************************************************");
			builder.AppendLineInvariant("// </autogenerated>");
			builder.AppendLine();

			using (builder.BlockInvariant($"namespace {typeSymbol.ContainingNamespace}"))
			{
				if (!IsFromPartialDeclaration(typeSymbol))
				{
					builder.AppendLineInvariant($"#warning {nameof(EqualityGenerator)}: you should add the partial modifier to the class {symbolNameWithGenerics}.");
				}

				if (baseTypeInfo.isBaseType && !baseTypeInfo.baseOverridesEquals)
				{
					builder.AppendLineInvariant($"#warning {nameof(EqualityGenerator)}: base type {typeSymbol.BaseType} does not override .Equals() method. It could lead to erroneous results.");
				}
				if (baseTypeInfo.isBaseType && !baseTypeInfo.baseOverridesGetHashCode)
				{
					builder.AppendLineInvariant($"#warning {nameof(EqualityGenerator)}: base type {typeSymbol.BaseType} does not override .GetHashCode() method. It could lead to erroneous results.");
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
					builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");

					using (builder.BlockInvariant($"public static bool Equals({symbolNameWithGenerics} a, {symbolNameWithGenerics} b)"))
					{
						if (typeSymbol.IsReferenceType)
						{
							builder.AppendLineInvariant("if (ReferenceEquals(a, b)) return true; // Same instance");
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
					builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
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
					builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
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
						builder.AppendLineInvariant("if (other.GetHashCode() != GetHashCode()) return false;");

						var baseCall = baseTypeInfo.baseOverridesEquals
							? "base.KeyEquals(other)"
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
					builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
					using (builder.BlockInvariant("public override int GetHashCode()"))
					{
						builder.AppendLineInvariant("return _computedHashCode ?? (int)(_computedHashCode = ComputeHashCode());");
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
						builder.AppendLine();
						builder.AppendLineInvariant("#region \"Key Equality\" Section -- THIS IS WHERE KEY EQUALS IS DONE + KEY HASH CODE IS COMPUTED");

						builder.AppendLineInvariant("/// <inheritdoc />");
						builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
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
						builder.AppendLineInvariant("[global::System.Diagnostics.Contracts.Pure]");
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

		private void GenerateEqualLogic(INamedTypeSymbol typeSymbol, IndentedStringBuilder builder, ISymbol[] equalityMembers, string baseCall)
		{
			if (baseCall == null && equalityMembers.Length == 0)
			{
				builder.AppendLineInvariant("#warning No fields or properties used for equality check.");
			}

			foreach (var member in equalityMembers)
			{
				var type = (member as IFieldSymbol)?.Type ?? (member as IPropertySymbol)?.Type;
				if (type == null)
				{
					continue;
				}
				var (_, customComparerProperty) = GetCustomsForMembers(typeSymbol, member, type);

				builder.AppendLine();

				if (customComparerProperty == null)
				{
					builder.AppendLineInvariant($"// **{member.Name}** You can define a custom comparer for {member.Name} and it will be used.");
					builder.AppendLineInvariant($"// CUSTOM COMPARER>> private static IEqualityComparer<{type}> {GetCustomComparerPropertyName(member)} => <custom comparer>;");

					using (builder.BlockInvariant(
						$"if(!System.Collections.Generic.EqualityComparer<{type}>.Default.Equals({member.Name}, other.{member.Name}))"))
					{
						builder.AppendLineInvariant($"return false; // {member.Name} not equal");
					}
				}
				else
				{
					using (builder.BlockInvariant($"if(!{customComparerProperty.Name}.Equals({member.Name}, other.{member.Name})) // Using custom comparer provided by `{customComparerProperty.Name}()`."))
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

		private void GenerateHashLogic(INamedTypeSymbol typeSymbol, IndentedStringBuilder builder, ISymbol[] hashMembers, string baseCall)
		{
			if (baseCall == null && hashMembers.Length == 0)
			{
				builder.AppendLineInvariant("#warning There is no members marked with [Uno.EqualityHash] or [Uno.EqualityKey]. You should add at least one. Documentation: https://github.com/nventive/Uno.CodeGen/blob/master/doc/Equality%20Generation.md");
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
						var member = hashMembers[i];
						var primeNumber = PrimeNumbers[i % PrimeNumbers.Length];

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

						var definition = type;

						while (definition is INamedTypeSymbol nts && !nts.ConstructedFrom.Equals(definition))
						{
							definition = nts.ConstructedFrom;
						}

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
						else if (definition.Equals(_collectionSymbol)
							|| definition.DerivesFromType(_collectionSymbol))
						{
							getHashCode = $"((global::System.Collections.ICollection){member.Name}).Count";
						}
						else if (definition.Equals(_collectionGenericSymbol)
							|| definition.DerivesFromType(_collectionGenericSymbol))
						{
							getHashCode = $"((global::System.Collections.Generic.ICollection<{type.GetTypeArgumentNames().FirstOrDefault()}>){member.Name}).Count";
						}
						else
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
								builder.AppendLineInvariant(
									$"#warning Type `{type.GetDisplayFriendlyName()}` of member `{member.Name}` " +
									"doesn't implements .GetHashCode(): it won't be used for hash computation. " +
									$"If you can change the type {type}, you should use a custom hash method or " +
									"a custom comparer.");
								continue;
							}

							getHashCode = $"{member.Name}.GetHashCode()";
						}

						if (type.IsReferenceType)
						{
							using (builder.BlockInvariant($"if ({member.Name} != null)"))
							{
								builder.AppendLineInvariant($"hash = ({getHashCode} * {primeNumber}) ^ hash;");
							}
						}
						else
						{
							builder.AppendLineInvariant($"hash = ({getHashCode} * {primeNumber}) ^ hash;");
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

		private (ISymbol[] equalitymembers, ISymbol[] hashMembers, ISymbol[] keyEqualityMembers) GetEqualityMembers(INamedTypeSymbol typeSymbol)
		{
			var properties =
				from property in typeSymbol.GetProperties()
				where !property.IsWriteOnly
				where !property.IsStatic
				where !property.IsImplicitlyDeclared
				where property.GetMethod.DeclaredAccessibility > Accessibility.Private
				select (symbol: (ISymbol)property, type: property.Type);

			var fields =
				from field in typeSymbol.GetFields()
				where !field.IsStatic
				where !field.IsImplicitlyDeclared
				where field.DeclaredAccessibility > Accessibility.Private
				select (symbol: (ISymbol)field, type: field.Type);

			var equalityMembers = new List<ISymbol>();
			var hashMembers = new List<ISymbol>();
			var keyEqualityMembers = new List<ISymbol>();

			foreach (var (symbol, type) in properties.Concat(fields))
			{
				var symbolAttributes = symbol.GetAttributes();
				var typeAttributes = type.GetAttributes();

				if (symbolAttributes.Any(a => a.AttributeClass.Equals(_ignoreForEqualityAttributeSymbol))
					|| typeAttributes.Any(a => a.AttributeClass.Equals(_ignoreForEqualityAttributeSymbol)))
				{
					continue; // [EqualityIgnore] on the member or the type itself: this member is ignored
				}

				equalityMembers.Add(symbol);

				if (symbolAttributes.Any(a =>
					a.AttributeClass.Equals(_equalityKeyCodeAttributeSymbol)
					|| a.AttributeClass.Equals(_dataAnnonationsKeyAttributeSymbol)))
				{
					// [EqualityKey] on the member: this member is used for both key & hash
					hashMembers.Add(symbol);
					keyEqualityMembers.Add(symbol);
				}
				else if (symbolAttributes.Any(a => a.AttributeClass.Equals(_equalityHashCodeAttributeSymbol)))
				{
					// [EqualityHash] on the member: this member is used for hash computation
					hashMembers.Add(symbol);
				}
			}

			return (equalityMembers.ToArray(), hashMembers.ToArray(), keyEqualityMembers.ToArray());
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
	}
}
