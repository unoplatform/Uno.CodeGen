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
using Microsoft.CodeAnalysis;

namespace Uno.Helpers
{
	public static class NamedTypeSymbolExtensions
	{
		public static SymbolNames GetSymbolNames(this INamedTypeSymbol typeSymbol, INamedTypeSymbol typeToUseForSubstitutions = null)
		{
			var substitutions = typeToUseForSubstitutions.GetSubstitutionTypes();

			var symbolName = typeSymbol.Name;
			if (typeSymbol.TypeArguments.Length == 0) // not a generic type
			{
				return new SymbolNames(typeSymbol, symbolName, "", symbolName, symbolName, symbolName, symbolName, "");
			}

			var argumentNames = typeSymbol.GetTypeArgumentNames(substitutions);
			var genericArguments = string.Join(", ", argumentNames);

			// symbolNameWithGenerics: MyType<T1, T2>
			var symbolNameWithGenerics = $"{symbolName}<{genericArguments}>";

			// symbolNameWithGenerics: MyType&lt;T1, T2&gt;
			var symbolForXml = $"{symbolName}&lt;{genericArguments}&gt;";

			// symbolNameDefinition: MyType<,>
			var symbolNameDefinition = $"{symbolName}<{string.Join(",", typeSymbol.TypeArguments.Select(ta => ""))}>";

			// symbolNameWithGenerics: MyType_T1_T2
			var symbolFilename = $"{symbolName}_{string.Join("_", argumentNames)}";

			var genericConstraints = " " + string.Join(" ", typeSymbol
				.TypeArguments
				.OfType<ITypeParameterSymbol>()
				.SelectMany((tps, i) => tps.ConstraintTypes.Select(c => (tps: tps, c:c, i:i)))
				.Select(x => $"where {argumentNames[x.i]} : {x.c}"));

			return new SymbolNames(typeSymbol, symbolName, $"<{genericArguments}>", symbolNameWithGenerics, symbolForXml, symbolNameDefinition, symbolFilename, genericConstraints);
		}

		public static string[] GetTypeArgumentNames(
			this ITypeSymbol typeSymbol,
			(string argumentName, string type)[] substitutions)
		{
			if (typeSymbol is INamedTypeSymbol namedSymbol)
			{
				var dict = substitutions.ToDictionary(x => x.argumentName, x => x.type);

				return namedSymbol.TypeArguments
					.Select(ta => ta.ToString())
					.Select(t=> dict.ContainsKey(t) ? dict[t] : t)
					.ToArray();
			}
			return new string[0];
		}

		public static (string argumentName, string type)[] GetSubstitutionTypes(this INamedTypeSymbol type)
		{
			if (type == null || type.TypeArguments.Length == 0)
			{
				return new(string, string)[] { };
			}

			var argumentParameters = type.TypeParameters;
			var argumentTypes = type.TypeArguments;

			var result = new (string, string)[type.TypeArguments.Length];

			for (var i = 0; i < argumentParameters.Length; i++)
			{
				var parameterType = argumentTypes[i] as INamedTypeSymbol;
				var parameterNames = parameterType.GetSymbolNames();

				result[i]= (argumentParameters[i].Name, parameterNames.GetSymbolFullNameWithGenerics());
			}

			return result;
		}
	}

	public class SymbolNames
	{
		public SymbolNames(
			INamedTypeSymbol symbol,
			string symbolName,
			string genericArguments,
			string symbolNameWithGenerics,
			string symbolFoxXml,
			string symbolNameDefinition,
			string symbolFilename,
			string genericConstraints)
		{
			Symbol = symbol;
			SymbolName = symbolName;
			GenericArguments = genericArguments;
			SymbolNameWithGenerics = symbolNameWithGenerics;
			SymbolFoxXml = symbolFoxXml;
			SymbolNameDefinition = symbolNameDefinition;
			SymbolFilename = symbolFilename;
			GenericConstraints = genericConstraints;
		}

		public INamedTypeSymbol Symbol { get; }
		public string SymbolName { get; }
		public string GenericArguments { get; }
		public string SymbolNameWithGenerics { get; }
		public string SymbolFoxXml { get; }
		public string SymbolNameDefinition { get; }
		public string SymbolFilename { get; }
		public string GenericConstraints { get; }

		public string GetContainingTypeFullName(INamedTypeSymbol typeForSubstitutions = null)
		{
			if (Symbol.ContainingType != null)
			{
				return Symbol
					.ContainingType
					.GetSymbolNames(typeForSubstitutions)
					.GetSymbolFullNameWithGenerics();
			}

			return Symbol.ContainingNamespace.ToString();
		}

		public string GetSymbolFullNameWithGenerics(INamedTypeSymbol typeForSubstitutions = null)
		{
			return $"{GetContainingTypeFullName(typeForSubstitutions)}.{SymbolNameWithGenerics}";
		}

		public void Deconstruct(
			out string symbolName,
			out string genericArguments,
			out string symbolNameWithGenerics,
			out string symbolFoxXml,
			out string symbolNameDefinition,
			out string symbolFilename,
			out string genericConstraints)
		{
			symbolName = SymbolName;
			genericArguments = GenericArguments;
			symbolNameWithGenerics = SymbolNameWithGenerics;
			symbolFoxXml = SymbolFoxXml;
			symbolNameDefinition = SymbolNameDefinition;
			symbolFilename = SymbolFilename;
			genericConstraints = GenericConstraints;
		}
	}
}
