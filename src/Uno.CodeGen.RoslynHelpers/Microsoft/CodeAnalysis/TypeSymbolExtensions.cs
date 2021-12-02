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
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
	public static class TypeSymbolExtensions
	{

		private const bool DoDefaultCompareUsingSubstitutedType = true;

		/// <summary>
		/// Returns true is the underlying type is an interface
		/// </summary>
		/// <param name="symbol">The type symbol</param>
		/// <returns>True if the type is an interface</returns>
		public static bool IsInterface(this ITypeSymbol symbol)
		{
			return symbol.TypeKind == TypeKind.Interface;
		}

		/// <summary>
		/// Returns all the interfaces this type inherits from, including the type itself if it is an interface
		/// </summary>
		/// <param name="type">The type symbol</param>
		/// <returns>All the interfaces this type inherits from, including itself</returns>
		public static IList<INamedTypeSymbol> GetAllInterfacesIncludingThis(this ITypeSymbol type)
		{
			var allInterfaces = type.AllInterfaces;
			var namedType = type as INamedTypeSymbol;
			if (namedType != null && namedType.TypeKind == TypeKind.Interface && !allInterfaces.Contains(namedType))
			{
				var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1);
				result.Add(namedType);
				result.AddRange(allInterfaces);
				return result;
			}

			return allInterfaces;
		}

		private static INamedTypeSymbol GetTypeSymbolFromMetadataName(string otherTypeFullName, SyntaxNodeAnalysisContext context)
		{
			return context.SemanticModel.Compilation.GetTypeByMetadataName(otherTypeFullName);
		}

		/// <summary>
		/// Compares two type symbols for equality
		/// </summary>
		/// <param name="current">The current type symbol</param>
		/// <param name="other">Another type sybol</param>
		/// <param name="compareUsingSubstitutedType">If false, the comparison will use the original definitions of both symbols (in case one or both are generic types)</param>
		/// <returns>If the symbols are equal</returns>
		public static bool EqualsType(this ITypeSymbol current, ITypeSymbol other, bool compareUsingSubstitutedType = DoDefaultCompareUsingSubstitutedType)
		{
			var currentTypeSymbol = compareUsingSubstitutedType ? current : current?.OriginalDefinition;
			var otherTypeSymbol = compareUsingSubstitutedType ? other : other?.OriginalDefinition;

			return currentTypeSymbol != null && currentTypeSymbol.OriginalDefinition.Equals(otherTypeSymbol);
		}

		/// <summary>
		/// If both current and other are ITypeParameterSymbols, checks if their constraints are equivalent. Otherwise, returns the result of EqualsType.
		/// </summary>
		/// <param name="current"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static bool IsEquivalentToType(this ITypeSymbol current, ITypeSymbol other)
		{
			if (current is ITypeParameterSymbol && other is ITypeParameterSymbol)
			{
				return (current as ITypeParameterSymbol).HasEquivalentConstraintsTo(other as ITypeParameterSymbol);
			}

			if (current is INamedTypeSymbol && other is INamedTypeSymbol)
			{
				return (current as INamedTypeSymbol).IsEquivalentToType(other as INamedTypeSymbol);
			}

			return current.EqualsType(other, compareUsingSubstitutedType: true);
		}

		/// <summary>
		/// Checks if the underlying type of the current type symbol derives from the type with the provided name  
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="otherTypeFullName">The full name for the System.Type instance the current type symbol will be checked against for inheritance/implementation</param>
		/// <param name="context">The analysis context</param>
		/// <returns>If the underlying type of the current type symbol implements or inherits the target type</returns>
		public static bool DerivesFromType(this ITypeSymbol symbol, string otherTypeFullName, SyntaxNodeAnalysisContext context)
		{
			var otherType = GetTypeSymbolFromMetadataName(otherTypeFullName, context);
			return DerivesFromType(symbol, otherType);
		}

		/// <summary>
		/// Checks if the underlying type of the current type symbol derives from the type with the provided name  
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="otherTypeFullName">The full name for the System.Type instance the current type symbol will be checked against for inheritance/implementation</param>
		/// <param name="context">The analysis context</param>
		/// <returns>If the underlying type of the current type symbol implements or inherits the target type</returns>
		public static bool DerivesFromType(this ITypeSymbol symbol, ITypeSymbol otherType)
		{
			var baseTypes = GetBaseTypesAndThis(symbol);
			var implementedInterfaces = GetAllInterfacesIncludingThis(symbol);

			return baseTypes.Any(baseType => baseType.Equals(otherType) ||
				   implementedInterfaces.Any(baseInterfaceType => baseInterfaceType.Equals(otherType)));
		}

		/// <summary>
		/// Checks if the underlying type of the current type symbol derives from the provided type
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="otherType">The System.Type instance that the type symbol will be checked against for inheritance/implementation</param>
		/// <param name="context">The analysis context</param>
		/// <returns>If the underlying type of the current type symbol implements or inherits the target type</returns>
		public static bool DerivesFromType(this ITypeSymbol symbol, Type otherType, SyntaxNodeAnalysisContext context)
		{
			return DerivesFromType(symbol, otherType?.FullName, context);
		}

		/// <summary>
		/// Checks if the underlying type of the current type symbol derives from the provided type
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="context">The analysis context</param>
		/// <param name="typeNames">The type names from which the symbol could inherit</param>
		/// <returns>If the underlying type of the current type symbol implements or inherits the target type</returns>
		public static bool DerivesFromAnyOfTheseTypes(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context, params string[] typeNames)
		{
			return typeNames.Any(typeName => DerivesFromType(symbol, typeName, context));
		}

		/// <summary>
		/// Checks if the underlying type of the current type symbol derives from the generic argument type
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="context">The analysis context</param>
		/// <returns>If the underlying type of the current type symbol implements or inherits the target type</returns>
		public static bool DerivesFromType<TSymbolType>(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
		{
			return DerivesFromType(symbol, typeof(TSymbolType), context);
		}

		/// <summary>
		/// Checks if the underlying type of the current type symbol is the same as the type with the provided name  
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="otherTypeFullName">The full name for the System.Type instance the current type symbol will be checked against for equality</param>
		/// <param name="context">The analysis context</param>
		/// <param name="compareUsingOriginalType">Determines if the comparison wil use the original definitions of both symbols (in case one or both are generic types)</param>
		/// <returns>If the underlying type of the current type symbol is equal to the target type</returns>
		public static bool IsOfType(this ITypeSymbol symbol, string otherTypeFullName, SyntaxNodeAnalysisContext context, bool compareUsingOriginalType = DoDefaultCompareUsingSubstitutedType)
		{
			var otherType = GetTypeSymbolFromMetadataName(otherTypeFullName, context);
			return EqualsType(symbol, otherType, compareUsingOriginalType);
		}

		/// <summary>
		/// Checks if the underlying type of the current type symbol is the same as the type with the provided type  
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="otherType">The System.Type instance that the provided type symbol will be checked against for equality</param>
		/// <param name="context">The analysis context</param>
		/// <param name="compareUsingOriginalType">Determines if the comparison wil use the original definitions of both symbols (in case one or both are generic types)</param>
		/// <returns>If the underlying type of the current type symbol is equal to the target type</returns>
		public static bool IsOfType(this ITypeSymbol symbol, Type otherType, SyntaxNodeAnalysisContext context, bool compareUsingOriginalType = DoDefaultCompareUsingSubstitutedType)
		{
			return IsOfType(symbol, otherType?.FullName, context, compareUsingOriginalType);
		}

		/// <summary>
		/// Checks if the underlying type of the current type symbol is the same as the type with the provided generic type
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="context">The analysis context</param>
		/// <param name="compareUsingOriginalType">Determines if the comparison wil use the original definitions of both symbols (in case one or both are generic types)</param>
		/// <returns>If the underlying type of the current type symbol is equal to the target type</returns>
		public static bool IsOfType<TSymbolType>(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context, bool compareUsingOriginalType = DoDefaultCompareUsingSubstitutedType)
		{
			return IsOfType(symbol, typeof(TSymbolType), context, compareUsingOriginalType);
		}

		/// <summary>
		/// Checks if one of the provided types matches the underlying type of the current type symbol
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="context">The analysis context</param>
		/// <param name="compareUsingOriginalType">Determines if the comparison wil use the original definitions of both symbols (in case one or both are generic types)</param>
		/// <param name="otherTypeNames">The full names for the System.Type instances that the provided type symbol will be checked against for equality</param>
		/// <returns>If the underlying type of the current type symbol is equal to one of the target types</returns>
		public static bool IsOneOfTheseTypes(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context, bool compareUsingOriginalType, params string[] otherTypeNames)
		{
			var otherTypes = otherTypeNames.Select(typeName => GetTypeSymbolFromMetadataName(typeName, context));
			return otherTypes.Any(type => EqualsType(symbol, type, compareUsingOriginalType));
		}

		/// <summary>
		/// Checks if one of the provided types matches the underlying type of the current type symbol. 
		/// Uses the default comparison strategy, defined in <see cref="DoDefaultCompareUsingSubstitutedType"/> 
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="otherTypeNames">The full names for the System.Type instances that the provided type symbol will be checked against for equality</param>
		/// <param name="context">The analysis context</param>
		/// <returns>If the underlying type of the current type symbol is equal to one of the target types</returns>
		public static bool IsOneOfTheseTypes(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context, params string[] otherTypeNames)
		{
			return IsOneOfTheseTypes(symbol, context, true, otherTypeNames);
		}

		/// <summary>
		/// Checks if one of the provided types matches the underlying type of the current type symbol
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="compareUsingOriginalType">Determines if the comparison wil use the original definitions of both symbols (in case one or both are generic types)</param>
		/// <param name="otherTypes">The  System.Type instances the current type symbol will be check against for equality</param>
		/// <param name="context">The analysis context</param>
		/// <returns>If the underlying type of the current type symbol is equal to one of the target types</returns>
		public static bool IsOneOfTheseTypes(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context, bool compareUsingOriginalType, params Type[] otherTypes)
		{
			return IsOneOfTheseTypes(symbol, context, compareUsingOriginalType, otherTypes.Select(t => t.FullName).ToArray());
		}

		/// <summary>
		/// Checks if one of the provided types matches the underlying type of the current type symbol. 
		/// Uses the default comparison strategy, defined in <see cref="DoDefaultCompareUsingSubstitutedType"/> 
		/// </summary>
		/// <param name="symbol">The current type symbol</param>
		/// <param name="otherTypes">The  System.Type instances the current type symbol will be check against for equality</param>
		/// <param name="context">The analysis context</param>
		/// <returns>If the underlying type of the current type symbol is equal to one of the target types</returns>
		public static bool IsOneOfTheseTypes(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context, params Type[] otherTypes)
		{
			return IsOneOfTheseTypes(symbol, context, true, otherTypes);
		}

		public static bool IsAnonymousType(this INamedTypeSymbol symbol)
		{
			return symbol?.IsAnonymousType == true;
		}

		/// <summary>
		/// True if type symbol represents void
		/// </summary>
		/// <param name="symbol"></param>
		/// <returns></returns>
		public static bool IsVoid(this ITypeSymbol symbol)
		{
			return symbol.SpecialType == SpecialType.System_Void;
		}

		/// <summary>
		/// True if type symbol is a primitive, DateTime, or DateTimeOffset
		/// </summary>
		/// <param name="symbol"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public static bool IsSimpleType(this ITypeSymbol symbol, SyntaxNodeAnalysisContext context)
		{
			return symbol.IsOfType<string>(context)
				|| symbol.IsOfType<double>(context)
				|| symbol.IsOfType<DateTime>(context)
				|| symbol.IsOfType<DateTimeOffset>(context)
				|| symbol.IsOfType<long>(context)
				|| symbol.IsOfType<uint>(context)
				|| symbol.IsOfType<ulong>(context)
				|| symbol.IsOfType<short>(context)
				|| symbol.IsOfType<ushort>(context)
				|| symbol.IsOfType<decimal>(context)
				|| symbol.IsOfType<char>(context)
				|| symbol.IsOfType<float>(context)
				|| symbol.IsOfType<int>(context);
		}

		/// <summary>
		/// Return all the symbols for the base types present in the current type symbol's underlying type inheritance 
		/// hierarchy, including the type from the symbol itself
		/// </summary>
		/// <param name="type">The type symbol to analyze</param>
		/// <returns>The symbols for all the base type for the current type symbol's underlying type, including the current stype</returns>
		public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
		{
			var current = type;
			while (current != null)
			{
				yield return current;
				current = current.BaseType;
			}
		}

		/// <summary>
		/// Return all the symbols for the base types present in the current type symbol's underlying type inheritance 
		/// hierarchy
		/// </summary>
		/// <param name="type">The type symbol to analyze</param>
		/// <returns>The symbols for all the base type for the current type symbol's underlying type</returns>
		public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
		{
			var current = type.BaseType;
			while (current != null)
			{
				yield return current;
				current = current.BaseType;
			}
		}

		/// <summary>
		/// Return the symbols representing all the classes in which the current type symbol's underlying type is contained,
		/// including the class of the current symbol's underlying type
		/// hierarchy
		/// </summary>
		/// <param name="type">The type symbol to analyze</param>
		/// <returns>The symbols for all the classes in which the current type symbol's underlying type is contained, including the current type</returns>
		public static IEnumerable<ITypeSymbol> GetContainingTypesAndThis(this ITypeSymbol type)
		{
			var current = type;
			while (current != null)
			{
				yield return current;
				current = current.ContainingType;
			}
		}

		/// <summary>
		/// Return the symbols representing all the classes in which the current type symbol's underlying type is contained
		/// hierarchy
		/// </summary>
		/// <param name="type">The type symbol to analyze</param>
		/// <returns>The symbols for all the classes in which the current type symbol's underlying type is containede</returns>
		public static IEnumerable<INamedTypeSymbol> GetContainingTypes(this ITypeSymbol type)
		{
			var current = type.ContainingType;
			while (current != null)
			{
				yield return current;
				current = current.ContainingType;
			}
		}

		/// <summary>
		/// Return all the attributes applied to the current type, include the ones inherited from inheritance ancestors
		/// </summary>
		/// <param name="type">The type symbol to analyze</param>
		/// <returns>All the attributes applied to the type, either directly or through inheritance ancestors that have those attributes</returns>
		public static IEnumerable<AttributeData> GetAllAppliedAttributes(this ITypeSymbol type)
		{
			return type
				.GetBaseTypesAndThis()
				.SelectMany(t => t.GetAttributes());
		}

		/// <summary>
		/// Returns an enumeration of the methods that are accessible from within a type, 
		/// including protected and private methods. 
		/// </summary>
		/// <param name="type">The type to explore</param>
		/// <param name="context">The analysis context</param>
		/// <param name="visitHierarchy">If true, the returned methods will included the accessible (public and protected) methods this type inherits from</param>
		/// <param name="includeObsoleteMethods">If true, this will include the methods marked as obsolete</param>
		/// <param name="methodName">Only targets methods with this specific name (optionnal)</param>
		/// <returns>The methods that are available from within the provided type (includes inherited methods if <see cref="visitHierarchy"/> is set to true)</returns>
		public static IEnumerable<IMethodSymbol> GetAllAccessibleMethodsFromWithinType(
			this ITypeSymbol type,
			SyntaxNodeAnalysisContext context,
			bool visitHierarchy = true,
			bool includeObsoleteMethods = false,
			string methodName = null)
		{
			return GetAllMethodsWithinType(type, context, visitHierarchy, includeObsoleteMethods, methodName)
				.Where(x => x.DeclaredAccessibility == Accessibility.Public || x.DeclaredAccessibility == Accessibility.ProtectedOrFriend);

		}

		/// <summary>
		/// Returns an enumeration of the methods that are publicly accessible for a given type
		/// </summary>
		/// <param name="type">The type to explore</param>
		/// <param name="context">The analysis context</param>
		/// <param name="visitHierarchy">If true, the returned methods will included the publicly accessible methods this type inherits from</param>
		/// <param name="includeObsoleteMethods">If true, this will include the methods marked as obsolete</param>
		/// <param name="methodName">Only targets methods with this specific name (optionnal)</param>
		/// <returns>The methods that are publicly accessible for a given type (includes inherited methods if <see cref="visitHierarchy"/> is set to true)</returns>
		public static IEnumerable<IMethodSymbol> GetAllPubliclyAccessibleMethodsFromType(
			this ITypeSymbol type,
			SyntaxNodeAnalysisContext context,
			bool visitHierarchy = false,
			bool includeObsoleteMethods = false,
			string methodName = null)
		{
			return GetAllMethodsWithinType(type, context, visitHierarchy, includeObsoleteMethods, methodName)
				.Where(m => m.DeclaredAccessibility == Accessibility.Public);
		}

		/// <summary>
		/// Returns am enmumeration of methods contained within a type
		/// </summary>
		/// <param name="currentType">The type to explore</param>
		/// <param name="context">The analysis context</param>
		/// <param name="visitHierarchy">If true, the returned methods will included the methods this type inherits from</param>
		/// <param name="includeObsoleteMethods">If true, this will include the methods marked as obsolete</param>
		/// <param name="methodName">Only targets methods with this specific name (optionnal)</param>
		/// <returns>The methods from a given type (includes inherited methods if <see cref="visitHierarchy"/> is set to true)</returns>
		private static IEnumerable<IMethodSymbol> GetAllMethodsWithinType(
			ITypeSymbol currentType,
			SyntaxNodeAnalysisContext context,
			bool visitHierarchy,
			bool includeObsoleteMethods,
			string methodName)
		{
			var obsoleteAttribute = context.SemanticModel.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute");

			while (currentType != null)
			{
				var members = methodName != null ? currentType.GetMembers(methodName) : currentType.GetMembers();
				var methods = members
					.OfType<IMethodSymbol>()
					.Where(x => !x.IsConstructorOrDestructor())
					.Where(x => !x.IsAbstract)
					.Where(x => includeObsoleteMethods || !x.HasAttributes(obsoleteAttribute));

				foreach (var method in methods)
				{
					yield return method;
				}

				if (!visitHierarchy)
				{
					break;
				}

				currentType = currentType.BaseType;
			}
		}

		/// <summary>
		/// Returns true if two sets of types are equivalent, ignoring order.
		/// </summary>
		/// <param name="current"></param>
		/// <param name="other"></param>
		/// <returns></returns>
		public static bool AreTypeSetsEquivalent(this IEnumerable<ITypeSymbol> current, IEnumerable<ITypeSymbol> other)
		{
			var otherList = other.ToList();
			foreach (var typeInCurrent in current)
			{
				int equivalentInOther = otherList.FindIndex(typeInOther => typeInCurrent.IsEquivalentToType(typeInOther));
				if (equivalentInOther == -1)
				{
					return false;
				}
				else
				{
					otherList.RemoveAt(equivalentInOther);
				}
			}
			//If we found a type in other for every type in current, and there are no remaining types in other, then the sets are an exact match
			return otherList.Count == 0;
		}

		/// <summary>
		/// Returns true if typeSymbol is a type parameter (eg 'TValue') or a generic type with only type parameters within its type arguments (eg Dictionary<TKey,T> or Dictionary<TKey,List<T>>, but not Dictionary<bool,int> or Dictionary<bool,List<string>>), false otherwise
		/// </summary>
		/// <param name="typeSymbol"></param>
		/// <returns></returns>
		public static bool IsTypeParameterOrGenericWithTypeParameterArguments(this ITypeSymbol typeSymbol)
		{
			if (typeSymbol is ITypeParameterSymbol)
			{
				return true;
			}
			var asNamedType = typeSymbol as INamedTypeSymbol;
			if (asNamedType?.IsGenericType ?? false)
			{
				return asNamedType.TypeArguments.All(typeArg => typeArg.IsTypeParameterOrGenericWithTypeParameterArguments());
			}
			return false;
		}

		/// <summary>
		/// Returns the given type as its most constrained/effective version according to its type and, possibly, its type constraints. 
		/// If no constrained version of the type is found, the type itself is returned.
		/// In the case of an array type, the method can also optionally provide the most constrained version of the array's underlying type
		/// </summary>
		/// <param name="typeSymbol">The type symbol which the method will attempt to resolve as a constrained/effective type</param>
		/// <param name="getReducedArrayType">If true, the logic will apply on the underlying type of the array (i.e 'A' is the underlying array type of 'A[]') in the case the provided type is an <see cref="IArrayTypeSymbol"/>. 
		/// If false, the method will simply return the array type in the case where in the case the provided type is an <see cref="IArrayTypeSymbol"/></param>
		/// <param name="tryResolvingGenericConstraint">If true, the given symbol, under its original or potentially reduced form, will be processed in order 
		/// to obtain a constrained type in the case where the symbol in question is a <see cref="ITypeParameterSymbol"/></param>
		/// <returns>The most constrained/effective version of the given symbol, or the symbol itself if no such constrained/effective type was found</returns>
		public static ITypeSymbol GetUnderlyingTypeOrSelf(this ITypeSymbol typeSymbol, bool getReducedArrayType = true, bool tryResolvingGenericConstraint = true)
		{
			// Array type
			var typeSymbolAsArrayType = typeSymbol as IArrayTypeSymbol;
			if (typeSymbolAsArrayType != null)
			{
				if (getReducedArrayType)
				{
					typeSymbol = typeSymbolAsArrayType.ElementType;
				}
				else
				{
					return typeSymbolAsArrayType;
				}
			}

			// Named type
			if (typeSymbol is INamedTypeSymbol)
			{
				return typeSymbol;
			}

			// Type parameter
			var typeAsTypeParameterSymbol = typeSymbol as ITypeParameterSymbol;
			if (tryResolvingGenericConstraint && typeAsTypeParameterSymbol != null)
			{
				var typeAsConstrained = typeAsTypeParameterSymbol.TryGetAsConstrainedType();
				return typeAsConstrained ?? (ITypeSymbol)typeAsTypeParameterSymbol;
			}

			return typeSymbol;
		}


		/// <summary>
		/// Return a more display friendly name for some of the named types with abbreviated or otherwise vague names
		/// </summary>
		/// <param name="current">The named type symbol to get the name from</param>
		/// <returns>The display friendly version of the current type symbol if it exists, or simply its regular name if it does not</returns>
		public static string GetDisplayFriendlyName(this ITypeSymbol current)
		{
			switch (current.SpecialType)
			{
				case SpecialType.System_Int16:
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
					return "Integer";
				case SpecialType.System_UInt16:
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
					return "UnsignedInteger";
				default:
					return current.Name;
			}
		}
	}
}
