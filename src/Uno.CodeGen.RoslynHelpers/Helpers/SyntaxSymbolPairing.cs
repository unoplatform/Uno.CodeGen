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
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.RoslynHelpers.Helpers
{
	/// <summary>
	/// Used to pair a syntax node to a custom semantic representation of that node
	/// </summary>
	/// <typeparam name="TSyntax">The type of the syntax node</typeparam>
	/// <typeparam name="TSymbol">The type of the symbol that is chosen as the semantic representation of the node</typeparam>
	public class SyntaxSymbolPairing<TSyntax, TSymbol>
		where TSyntax : SyntaxNode
		where TSymbol : ISymbol
	{
		private readonly Lazy<ITypeSymbol> _lazyTypeSymbolInitializer;
		private readonly Lazy<TSymbol> _lazySymbolInitializer;

		public ITypeSymbol TypeSymbol => _lazyTypeSymbolInitializer.Value;
		public TSymbol Symbol => _lazySymbolInitializer.Value;
		public TSyntax Node { get; }

		public SyntaxSymbolPairing(TSyntax node, Func<TSyntax, TSymbol> syntaxTransform, Func<TSyntax, ITypeSymbol> typeSymbolTransform = null)
		{
			Node = node;

			_lazySymbolInitializer = new Lazy<TSymbol>(() => syntaxTransform == null ? default(TSymbol) : syntaxTransform.Invoke(Node));
			_lazyTypeSymbolInitializer = new Lazy<ITypeSymbol>(() => typeSymbolTransform?.Invoke(Node));
		}

		public SyntaxSymbolPairing(TSyntax node, TSymbol symbol, ITypeSymbol typeSymbol = null)
			: this(node, syntax => symbol, syntax => typeSymbol)
		{
		}
	}
}
