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
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.Helpers
{
	public static class TypeSymbolExtensions
	{
		public static bool IsGenericArgument(this ITypeSymbol type)
		{
			return type.GetType().Name.Equals("SourceTypeParameterSymbol");
		}

		public static bool IsImmutable(this ITypeSymbol type, bool treatArrayAsImmutable)
		{
			foreach (var attribute in type.GetAttributes())
			{
				switch (attribute.AttributeClass.ToString())
				{
					case "Uno.ImmutableAttribute":
					case "Uno.GeneratedImmutableAttribute":
						return true;
				}
			}

			switch (type.SpecialType)
			{
				case SpecialType.System_Boolean:
				case SpecialType.System_Byte:
				case SpecialType.System_Char:
				case SpecialType.System_DateTime:
				case SpecialType.System_Decimal:
				case SpecialType.System_Double:
				case SpecialType.System_Enum:
				case SpecialType.System_Int16:
				case SpecialType.System_Int32:
				case SpecialType.System_Int64:
				case SpecialType.System_IntPtr:
				case SpecialType.System_SByte:
				case SpecialType.System_String:
				case SpecialType.System_Single:
				case SpecialType.System_UInt16:
				case SpecialType.System_UInt32:
				case SpecialType.System_UInt64:
				case SpecialType.System_UIntPtr:
					return true;
				case SpecialType.None:
				case SpecialType.System_Collections_Generic_IReadOnlyCollection_T:
				case SpecialType.System_Collections_Generic_IReadOnlyList_T:
					break; // need further validation
				default:
					return false;
			}

			if (type is IArrayTypeSymbol arrayType)
			{
				return arrayType.ElementType?.IsImmutable(treatArrayAsImmutable) ?? false;
			}

			var definitionType = type;

			while ((definitionType as INamedTypeSymbol)?.ConstructedFrom.Equals(definitionType) == false)
			{
				definitionType = ((INamedTypeSymbol)definitionType).ConstructedFrom;
			}

			switch (definitionType.ToString())
			{
				case "System.Attribute": // strange, but valid
				case "System.DateTime":
				case "System.DateTimeOffset":
				case "System.TimeSpan":
				case "System.Type":
				case "System.Uri":
				case "System.Version":
					return true;

				// .NET framework
				case "System.Collections.Generic.IReadOnlyList<T>":
				case "System.Collections.Generic.IReadOnlyCollection<T>":
				case "System.Nullable<T>":
				case "System.Tuple<T>":
				// System.Collections.Immutable (nuget package)
				case "System.Collections.Immutable.IImmutableList<T>":
				case "System.Collections.Immutable.IImmutableQueue<T>":
				case "System.Collections.Immutable.IImmutableSet<T>":
				case "System.Collections.Immutable.IImmutableStack<T>":
				case "System.Collections.Immutable.ImmutableArray<T>":
				case "System.Collections.Immutable.ImmutableHashSet<T>":
				case "System.Collections.Immutable.ImmutableList<T>":
				case "System.Collections.Immutable.ImmutableQueue<T>":
				case "System.Collections.Immutable.ImmutableSortedSet<T>":
				case "System.Collections.Immutable.ImmutableStack<T>":
				{
					var argumentParameter = (type as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
					return argumentParameter == null || argumentParameter.IsImmutable(treatArrayAsImmutable);
				}
				case "System.Collections.Immutable.IImmutableDictionary<TKey, TValue>":
				case "System.Collections.Immutable.ImmutableDictionary<TKey, TValue>":
				case "System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>":
				{
					var keyTypeParameter = (type as INamedTypeSymbol)?.TypeArguments.FirstOrDefault();
					var valueTypeParameter = (type as INamedTypeSymbol)?.TypeArguments.Skip(1).FirstOrDefault();
					return (keyTypeParameter == null || keyTypeParameter.IsImmutable(treatArrayAsImmutable))
						&& (valueTypeParameter == null || valueTypeParameter.IsImmutable(treatArrayAsImmutable));
				}
			}

			switch (definitionType.GetType().Name)
			{
				case "TupleTypeSymbol":
					return true; // tuples are immutables
			}

			switch (definitionType.BaseType?.ToString())
			{
				case "System.Enum":
					return true;
				case "System.Array":
					return treatArrayAsImmutable;
			}

			if (definitionType.IsReferenceType)
			{
				return false;
			}

			return false;
		}
	}
}
