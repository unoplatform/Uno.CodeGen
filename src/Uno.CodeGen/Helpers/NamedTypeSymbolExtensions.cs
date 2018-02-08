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

namespace Uno.Helpers
{
	public static class NamedTypeSymbolExtensions
	{
		public static (string symbolName, string genericArguments, string symbolNameWithGenerics, string symbolForXml, string symbolNameDefinition, string symbolFilename)
			GetSymbolNames(this INamedTypeSymbol typeSymbol)
		{
			var symbolName = typeSymbol.Name;
			if (typeSymbol.TypeArguments.Length == 0) // not a generic type
			{
				return (symbolName, "", symbolName, symbolName, symbolName, symbolName);
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

			return (symbolName, $"<{genericArguments}>", symbolNameWithGenerics, symbolForXml, symbolNameDefinition, symbolFilename);
		}

		public static string[] GetTypeArgumentNames(this ITypeSymbol typeSymbol)
		{
			return (typeSymbol as INamedTypeSymbol)?.TypeArguments.Select(ta => ta.MetadataName).ToArray() ?? new string[0];
		}
	}
}
