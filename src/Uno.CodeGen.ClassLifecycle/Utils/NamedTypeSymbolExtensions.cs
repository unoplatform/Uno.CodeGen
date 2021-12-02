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
using Microsoft.CodeAnalysis;

namespace Uno.CodeGen.ClassLifecycle.Utils
{
	internal static class NamedTypeSymbolExtensions
	{
		public static string SimpleLocationText(this ISymbol symbol)
		{
			var location = symbol.Locations.First(); // SimpleLocation does not take care of partial stuff
			return $"'{symbol.Name}' on line {location.GetLineSpan().StartLinePosition.Line} of file '{location.SourceTree.FilePath}'";
		}

		public static string GetInvocationText(this IMethodSymbol method, string target = "this", params string[] parameters)
		{
			var explicitInterface = method.ExplicitInterfaceImplementations.FirstOrDefault();
			return explicitInterface == null
				? $"{target}.{method.Name}({parameters.JoinBy(", ")});"
				: $"(({explicitInterface.ContainingSymbol}){target}).{explicitInterface.Name}({parameters.JoinBy(", ")});";
		}

		public static string GetInvocationText(this IMethodSymbol method, IEnumerable<IParameterSymbol> parameters)
			=> method.GetInvocationText(parameters: parameters.Select(p => p.Name).ToArray());

		public static string GlobalTypeDefinitionText(this ITypeSymbol type)
		{
			if (type.IsNullable(out var nullable))
			{
				return $"{nullable.GlobalTypeDefinitionText()}?";
			}

			switch (type.SpecialType)
			{
				case SpecialType.System_Boolean:
				case SpecialType.System_Byte:
				case SpecialType.System_Char:
				case SpecialType.System_Decimal:
				case SpecialType.System_Double:
				case SpecialType.System_Int16:
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
				case SpecialType.System_Object:
				case SpecialType.System_SByte:
				case SpecialType.System_String:
				case SpecialType.System_UInt16:
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
				case SpecialType.System_Void:
					return type.ToString();

				default:
					return $"global::{type}";
			}
		}

		public static IEnumerable<INamedTypeSymbol> GetNamedTypes(this INamespaceOrTypeSymbol sym)
		{
			if (sym is INamedTypeSymbol type)
			{
				yield return type;
			}

			foreach (var child in sym.GetMembers().OfType<INamespaceOrTypeSymbol>().SelectMany(GetNamedTypes))
			{
				yield return child;
			}
		}

		public static bool HasAttribute(this IMethodSymbol type, INamedTypeSymbol attribute)
			=> type.FindAttribute(attribute) != null;

		public static AttributeData FindAttribute(this INamedTypeSymbol type, INamedTypeSymbol attributeType)
			=> type.GetAttributes().FirstOrDefault(attribute => Equals(attribute.AttributeClass, attributeType));

		public static SymbolNames GetSymbolNames(this INamedTypeSymbol typeSymbol)
		{
			var namespaceParts = typeSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).Split('.');
			var parentParts = typeSymbol.GetContainingTypes().Reverse().Select(parent => new[] { parent.Name }.Concat(parent.GetTypeArgumentNames()).JoinBy("_"));
			var fullNameSpace = namespaceParts.Concat(parentParts).ToArray();

			var symbolName = typeSymbol.Name;
			if (typeSymbol.TypeArguments.Length == 0) // not a generic type
			{
				return new SymbolNames(symbolName, "", symbolName, symbolName, symbolName, symbolName, $"{fullNameSpace.JoinBy("/")}/{symbolName}");
			}

			var argumentNames = typeSymbol.GetTypeArgumentNames();
			var genericArguments = string.Join(", ", argumentNames);

			// symbolNameWithGenerics: MyType<T1, T2>
			var symbolNameWithGenerics = $"{symbolName}<{genericArguments}>";

			// symbolNameWithGenerics: MyType&lt;T1, T2&gt;
			var symbolForXml = $"{symbolName}&lt;{genericArguments}&gt;";

			// symbolNameDefinition: MyType<,>
			var symbolNameDefinition = $"{symbolName}<{string.Join(",", typeSymbol.TypeArguments.Select(ta => ""))}>";

			// symbolNameWithGenerics: MyType_T1_T2
			var symbolFilename = $"{symbolName}_{string.Join("_", argumentNames)}";

			// filePath: MyNamespace/MyParentClass_T/MyType_T1_T2
			var filePath = $"{fullNameSpace.JoinBy("/")}/{symbolFilename}";

			return new SymbolNames(symbolName, $"<{genericArguments}>", symbolNameWithGenerics, symbolForXml, symbolNameDefinition, symbolFilename, filePath);
		}

		public static string[] GetTypeArgumentNames(this ITypeSymbol typeSymbol)
		{
			return (typeSymbol as INamedTypeSymbol)?.TypeArguments.Select(ta => ta.MetadataName).ToArray() ?? new string[0];
		}

		public static string GetAvailableTypeArgument(this INamedTypeSymbol type, params string[] possibilities)
		{
			var alreadyUsed = type.GetTypeArgumentNames();

			var value = possibilities.FirstOrDefault(p => !alreadyUsed.Contains(p));
			if (value != null)
			{
				return value;
			}

			var fallback = possibilities.Last();
			for (var i = 0; ; i++)
			{
				value = fallback + i;
				if (!alreadyUsed.Contains(value))
				{
					return value;
				}
			}
		}

		public static (DisposePatternImplementationKind, IMethodSymbol) GetDisposablePatternImplementation(this INamedTypeSymbol sourceType)
		{
			var type = sourceType;

			while (type != null)
			{
				var disposePattern = type
					.GetMembers("Dispose")
					.OfType<IMethodSymbol>()
					.FirstOrDefault(method => method.Parameters.Length == 1 && method.Parameters.First().Type.SpecialType == SpecialType.System_Boolean);

				if (disposePattern == null)
				{
				}
				else if (Equals(type, sourceType))
				{
					return (DisposePatternImplementationKind.DisposePattern, disposePattern);
				}
				else if (disposePattern.IsVirtual || (disposePattern.IsOverride && !disposePattern.IsSealed))
				{
					return (DisposePatternImplementationKind.DisposePatternOnBase, disposePattern);
				}
				else if (disposePattern.IsSealed)
				{
					return (DisposePatternImplementationKind.SealedDisposePatternOnBase, disposePattern);
				}
				else // non overridable
				{
					return (DisposePatternImplementationKind.NonOverridableDisposePatternOnBase, disposePattern);
				}

				type = type.BaseType;
			}

			return (DisposePatternImplementationKind.None, null);
		}

		public static (DisposeImplementationKind, IMethodSymbol) GetDisposableImplementation(this INamedTypeSymbol sourceType)
		{
			var type = sourceType;

			while (type != null)
			{
				var dispose = type
					.GetMembers()
					.OfType<IMethodSymbol>()
					.FirstOrDefault(method => (method.Name == "Dispose" && method.Parameters.None()) || method.Name == "System.IDisposable.Dispose");

				if (dispose == null)
				{
				}
				else if (type == sourceType)
				{
					return (DisposeImplementationKind.Dispose, dispose);
				}
				else if (dispose.IsVirtual || (dispose.IsOverride && !dispose.IsSealed))
				{
					return (DisposeImplementationKind.OverridableDisposeOnBase, dispose);
				}
				else // sealed or non overridable
				{
					return (DisposeImplementationKind.DisposeOnBase, dispose);
				}

				type = type.BaseType;
			}

			return (DisposeImplementationKind.None, null);
		}
	}
}
