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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Uno.RoslynHelpers.Helpers
{
	public class CyclomaticComplexityWalker : CSharpSyntaxWalker
	{
		private Dictionary<SyntaxNode, RegionInfo> _codeRegionInfos;
		private SyntaxNode _currentCodeRegionRoot;
		private int _nestingLevel = 0;
		private SemanticModel _model;

		public IEnumerable<RegionInfo> Results { get { return _codeRegionInfos.Values; } }

		public CyclomaticComplexityWalker(SyntaxNode entryNode, SemanticModel model)
		{
			_codeRegionInfos = new Dictionary<SyntaxNode, RegionInfo>();
			_codeRegionInfos[entryNode] = new RegionInfo(entryNode);
			_model = model;
		}


		public override void Visit(SyntaxNode node)
		{
			var priorRegion = _currentCodeRegionRoot;
			if (node is MethodDeclarationSyntax)
			{
				_currentCodeRegionRoot = node;
				if (!_codeRegionInfos.ContainsKey(_currentCodeRegionRoot))
				{
					_codeRegionInfos[_currentCodeRegionRoot] = new RegionInfo(_currentCodeRegionRoot);
				}
			}

			bool isSimplePredicate = node.IsExtendedSyntaxOfType(ExtendedSyntaxType.SimplePredicate);
			bool isAnonymousFunction = node.IsExtendedSyntaxOfType(ExtendedSyntaxType.AnonymousFunctionExpression);

			if (isAnonymousFunction && _currentCodeRegionRoot != null)
			{
				if (!IsBuildDefinition(node))
				{
					_nestingLevel++;
					_codeRegionInfos[_currentCodeRegionRoot].PredicateCount += _nestingLevel;
				}
				else
				{
					_codeRegionInfos[_currentCodeRegionRoot].PredicateCount += 1;
				}
			}

			if (isSimplePredicate && _currentCodeRegionRoot != null)
			{
				_codeRegionInfos[_currentCodeRegionRoot].PredicateCount += 1;
			}

			base.Visit(node);

			if (isAnonymousFunction && !IsBuildDefinition(node))
			{
				_nestingLevel--;
			}

			_currentCodeRegionRoot = priorRegion;
		}

		private bool IsBuildDefinition(SyntaxNode node)
		{
			var simpleLambda = node as SimpleLambdaExpressionSyntax;

			if (simpleLambda != null)
			{
				var parameterSymbol = _model.GetDeclaredSymbol(simpleLambda.Parameter);

				var isBuilderType = parameterSymbol?.ToDisplayString()?.EndsWith("Builder");

				if (isBuilderType ?? false)
				{
					return true;
				}
			}

			return false;
		}

		private static int Factorial(int input)
		{
			if (input < 0)
			{
				throw new ArgumentOutOfRangeException("Factorial is only defined for non-negative integers.");
			}

			int answer = 1;

			while (input > 1)
			{
				answer *= input--;
			}

			return answer;
		}

		public class RegionInfo
		{
			public SyntaxNode RegionRoot { get; }
			public int PredicateCount { get; set; }
			public int CyclomaticComplexity { get { return PredicateCount + 1; } }

			public RegionInfo(SyntaxNode regionRoot)
			{
				RegionRoot = regionRoot;
			}
		}
	}
}
