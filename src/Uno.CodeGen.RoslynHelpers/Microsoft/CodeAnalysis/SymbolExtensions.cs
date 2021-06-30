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
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.CodeAnalysis
{
	/// <summary>
	/// Roslyn symbol extensions
	/// </summary>
	internal static class SymbolExtensions
	{
		public static IEnumerable<IPropertySymbol> GetProperties(this INamedTypeSymbol symbol) => symbol.GetMembers().OfType<IPropertySymbol>();

		public static IEnumerable<IEventSymbol> GetAllEvents(this INamedTypeSymbol symbol)
		{
			do
			{
				foreach (var member in GetEvents(symbol))
				{
					yield return member;
				}

				symbol = symbol.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.Name != "Object");
		}

		public static IEnumerable<IEventSymbol> GetEvents(INamedTypeSymbol symbol) => symbol.GetMembers().OfType<IEventSymbol>();

		/// <summary>
		/// Determines if the symbol inherits from the specified type.
		/// </summary>
		/// <param name="symbol">The current symbol</param>
		/// <param name="typeName">A potential base class.</param>
		public static bool Is(this INamedTypeSymbol symbol, string typeName)
		{
			do
			{
				if (symbol.ToDisplayString() == typeName)
				{
					return true;
				}

				symbol = symbol.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.Name != "Object");

			return false;
		}

		/// <summary>
		/// Determines if the symbol inherits from the specified type.
		/// </summary>
		/// <param name="symbol">The current symbol</param>
		/// <param name="other">A potential base class.</param>
		public static bool Is(this INamedTypeSymbol symbol, INamedTypeSymbol other)
		{
			do
			{
				if (symbol == other)
				{
					return true;
				}

				symbol = symbol.BaseType;

				if (symbol == null)
				{
					break;
				}

			} while (symbol.Name != "Object");

			return false;
		}

		public static bool IsPublic(this ISymbol symbol) => symbol.DeclaredAccessibility == Accessibility.Public;

		/// <summary>
		/// Returns true if the symbol can be accessed from the current module
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static bool IsLocallyPublic(this ISymbol symbol, IModuleSymbol currentSymbol) =>
			symbol.DeclaredAccessibility == Accessibility.Public 
			||
			(
				symbol.Locations.Any(l => l.MetadataModule == currentSymbol)
				&& symbol.DeclaredAccessibility == Accessibility.Internal
			);

		public static IEnumerable<IMethodSymbol> GetMethods(this INamedTypeSymbol resolvedType)
		{
			return resolvedType.GetMembers().OfType<IMethodSymbol>();
		}

		public static IEnumerable<IFieldSymbol> GetFields(this INamedTypeSymbol resolvedType)
		{
			return resolvedType.GetMembers().OfType<IFieldSymbol>();
		}

		public static IEnumerable<IFieldSymbol> GetFieldsWithAttribute(this ITypeSymbol resolvedType, string name)
		{
			return resolvedType
				.GetMembers()
				.OfType<IFieldSymbol>()
				.Where(f => f.FindAttribute(name) != null);
		}

		public static AttributeData FindAttribute(this ISymbol property, string attributeClassFullName)
		{
			return property.GetAttributes().FirstOrDefault(a => a.AttributeClass.ToDisplayString() == attributeClassFullName);
		}

		public static AttributeData FindAttribute(this ISymbol property, INamedTypeSymbol attributeClassSymbol)
		{
			return property.GetAttributes().FirstOrDefault(a => a.AttributeClass == attributeClassSymbol);
		}

		public static AttributeData FindAttributeFlattened(this ISymbol property, INamedTypeSymbol attributeClassSymbol)
		{
			return property.GetAllAttributes().FirstOrDefault(a => a.AttributeClass == attributeClassSymbol);
		}

		/// <summary>
		/// Returns the element type of the IEnumerable, if any.
		/// </summary>
		/// <param name="resolvedType"></param>
		/// <returns></returns>
		public static ITypeSymbol EnumerableOf(this ITypeSymbol resolvedType)
		{
			var intf = resolvedType
				.GetAllInterfaces(includeCurrent: true)
				.FirstOrDefault(i => i.ToDisplayString().StartsWith("System.Collections.Generic.IEnumerable", StringComparison.OrdinalIgnoreCase));

			return intf?.TypeArguments.First();
		}

		public static IEnumerable<INamedTypeSymbol> GetAllInterfaces(this ITypeSymbol symbol, bool includeCurrent = true)
		{
			if (symbol != null)
			{
				if (includeCurrent && symbol.TypeKind == TypeKind.Interface)
				{
					yield return (INamedTypeSymbol)symbol;
				}

				do
				{
					foreach (var intf in symbol.Interfaces)
					{
						yield return intf;

						foreach (var innerInterface in intf.GetAllInterfaces())
						{
							yield return innerInterface;
						}
					}

					symbol = symbol.BaseType;

					if (symbol == null)
					{
						break;
					}

				} while (symbol.Name != "Object");
			}
		}

		public static bool IsNullable(this ITypeSymbol type)
		{
			return ((type as INamedTypeSymbol)?.IsGenericType ?? false)
				&& type.OriginalDefinition.ToDisplayString().Equals("System.Nullable<T>", StringComparison.OrdinalIgnoreCase);
		}

		public static bool IsNullable(this ITypeSymbol type, out ITypeSymbol nullableType)
		{
			if (type.IsNullable())
			{
				nullableType = ((INamedTypeSymbol)type).TypeArguments.First();
				return true;
			}
			else
			{
				nullableType = null;
				return false;
			}
		}

		public static ITypeSymbol NullableOf(this ITypeSymbol type)
		{
			return type.IsNullable()
				? ((INamedTypeSymbol)type).TypeArguments.First()
				: null;
		}

		public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol sym)
		{
			foreach (var child in sym.GetTypeMembers())
			{
				yield return child;
			}

			foreach (var ns in sym.GetNamespaceMembers())
			{
				foreach (var child2 in GetNamespaceTypes(ns))
				{
					yield return child2;
				}
			}
		}
		private static readonly Dictionary<string, string> _fullNamesMaping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{"string",     typeof(string).ToString()},
			{"long",       typeof(long).ToString()},
			{"int",        typeof(int).ToString()},
			{"short",      typeof(short).ToString()},
			{"ulong",      typeof(ulong).ToString()},
			{"uint",       typeof(uint).ToString()},
			{"ushort",     typeof(ushort).ToString()},
			{"byte",       typeof(byte).ToString()},
			{"double",     typeof(double).ToString()},
			{"float",      typeof(float).ToString()},
			{"decimal",    typeof(decimal).ToString()},
			{"bool",       typeof(bool).ToString()},
		};

		public static string GetFullName(this INamespaceOrTypeSymbol type)
		{
			IArrayTypeSymbol arrayType = type as IArrayTypeSymbol;
			if (arrayType != null)
			{
				return $"{arrayType.ElementType.GetFullName()}[]";
			}

			ITypeSymbol t;
			if ((type as ITypeSymbol).IsNullable(out t))
			{
				return $"System.Nullable`1[{t.GetFullName()}]";
			}

			var name = type.ToDisplayString();

			string output;

			if (_fullNamesMaping.TryGetValue(name, out output))
			{
				output = name;
			}

			return output;
		}

		public static string GetFullMetadataName(this INamespaceOrTypeSymbol symbol)
		{
			ISymbol s = symbol;
			var sb = new StringBuilder(s.MetadataName);

			var last = s;
			s = s.ContainingSymbol;

			if (s == null)
			{
				return symbol.GetFullName();
			}

			while (!IsRootNamespace(s))
			{
				if (s is ITypeSymbol && last is ITypeSymbol)
				{
					sb.Insert(0, '+');
				}
				else
				{
					sb.Insert(0, '.');
				}
				sb.Insert(0, s.MetadataName);

				s = s.ContainingSymbol;
			}

			var namedType = symbol as INamedTypeSymbol;

			if (namedType?.TypeArguments.Any() ?? false)
			{
				var genericArgs = string.Join(",", namedType.TypeArguments.Select(GetFullMetadataName));
				sb.Append($"[{ genericArgs }]");
			}

			return sb.ToString();
		}

		private static bool IsRootNamespace(ISymbol s)
		{
			return s is INamespaceSymbol && ((INamespaceSymbol)s).IsGlobalNamespace;
		}
		/// <summary>
		/// Return attributes on the current type and all its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<AttributeData> GetAllAttributes(this ISymbol symbol)
		{
			while (symbol != null)
			{
				foreach (var attribute in symbol.GetAttributes())
				{
					yield return attribute;
				}

				symbol = (symbol as INamedTypeSymbol)?.BaseType;
			}
		}

		/// <summary>
		/// Return properties of the current type and all of its ancestors
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static IEnumerable<IPropertySymbol> GetAllProperties(this INamedTypeSymbol symbol)
		{
			while (symbol != null)
			{
				foreach (var property in symbol.GetMembers().OfType<IPropertySymbol>())
				{
					yield return property;
				}

				symbol = symbol.BaseType;
			}
		}

		/// <summary>
		/// Converts declared accessibility on a symbol to a string usable in generated code.
		/// </summary>
		/// <param name="symbol">The symbol to get an accessibility string for.</param>
		/// <returns>Accessibility in format "public", "protected internal", etc.</returns>
		public static string GetAccessibilityAsCSharpCodeString(this ISymbol symbol)
		{
			switch (symbol.DeclaredAccessibility)
			{
				case Accessibility.Private:
					return "private";
				case Accessibility.ProtectedOrInternal:
					return "protected internal";
				case Accessibility.Protected:
					return "protected";
				case Accessibility.Internal:
					return "internal";
				case Accessibility.Public:
					return "public";
			}

			throw new ArgumentOutOfRangeException($"{symbol.DeclaredAccessibility} is not supported.");
		}

		/// <summary>
		/// Returns a boolean value indicating whether the symbol is decorated with all the given attributes
		/// </summary>
		/// <param name="symbol">The extended symbol</param>
		/// <param name="attributes">The given attributes</param>
		/// <returns></returns>
		public static bool HasAttributes(this ISymbol symbol, params INamedTypeSymbol[] attributes)
		{
			var currentSymbolAttributes = symbol.GetAttributes();
			return currentSymbolAttributes.Any() && currentSymbolAttributes.All(x => attributes.Contains(x.AttributeClass));
		}
	}
}
