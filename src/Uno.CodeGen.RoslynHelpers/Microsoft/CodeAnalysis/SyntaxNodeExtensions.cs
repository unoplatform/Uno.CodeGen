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
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using Uno.RoslynHelpers.Helpers;

namespace Microsoft.CodeAnalysis
{
	public static class SyntaxNodeExtensions
	{
		public const bool DefaultConsiderParametersAsPotentialTargets = false;
		public const bool DefaultPrioritizeSkip = false;

		/// <summary>
		/// Returns the semantic symbol of the provided syntax node
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="context">The syntax analysis context</param>
		/// <returns>The symbol of the provided syntax node</returns>
		public static ISymbol GetSymbol(this SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context)
		{
			return context.SemanticModel.GetSymbolInfo(syntaxNode).Symbol;
		}

		/// <summary>
		/// Returns the semantic symbol of the provided syntax node
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="model">The syntax semantic model</param>
		/// <returns>The symbol of the provided syntax node</returns>
		public static ISymbol GetSymbol(this SyntaxNode syntaxNode, SemanticModel model)
		{
			return model.GetSymbolInfo(syntaxNode).Symbol;
		}

		/// <summary>
		/// Returns the semantic declared symbol of the provided syntax node as the provided symbol type
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="model">The syntax semantic model</param>
		/// <returns>The symbol of the provided syntax node</returns>
		public static ISymbol GetDeclaredSymbol(this SyntaxNode syntaxNode, SemanticModel model)
		{
			return model.GetDeclaredSymbol(syntaxNode);
		}

		/// <summary>
		/// Returns the semantic declared symbol of the provided syntax node as the provided symbol type
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="context">The syntax analysis context</param>
		/// <returns>The symbol of the provided syntax node</returns>
		public static ISymbol GetDeclaredSymbol(this SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context)
		{
			return context.SemanticModel.GetDeclaredSymbol(syntaxNode);
		}

		/// <summary>
		/// Returns the semantic symbol of the provided syntax node, or the declared symbol if the previous cast was invalid
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="context">The syntax analysis context</param>
		/// <returns>The symbol or declared symbol of the provided syntax node</returns>
		public static ISymbol GetSymbolOrDeclared(this SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context)
		{
			return syntaxNode.GetSymbol(context) ?? syntaxNode.GetDeclaredSymbol(context);
		}

		/// <summary>
		/// Returns the semantic symbol of the provided syntax node, or the declared symbol if the previous cast was invalid
		/// </summary>
		/// <param name="syntaxNode"></param>
		/// <param name="semanticModel"></param>
		/// <returns>The symbol or declared symbol of the provided syntax node</returns>
		public static ISymbol GetSymbolOrDeclared(this SyntaxNode syntaxNode, SemanticModel semanticModel)
		{
			return semanticModel.GetSymbolInfo(syntaxNode).Symbol ?? semanticModel.GetDeclaredSymbol(syntaxNode);
		}

		/// <summary>
		/// The the enclosing symbol for the current syntax node
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="semanticModel">The current semantic model</param>
		/// <returns></returns>
		public static ISymbol GetEnclosingSymbol(this SyntaxNode syntaxNode, SemanticModel semanticModel)
		{
			var position = syntaxNode.SpanStart;
			return semanticModel.GetEnclosingSymbol(position);
		}

		/// <summary>
		/// Returns the semantic symbol of the provided syntax node
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="model">The syntax semantic model</param>
		/// <returns>The symbol of the provided syntax node</returns>
		public static TSymbolType GetSymbolAs<TSymbolType>(this SyntaxNode syntaxNode, SemanticModel model) where TSymbolType : class, ISymbol
		{
			return model.GetSymbolInfo(syntaxNode).Symbol as TSymbolType;
		}

		/// <summary>
		/// Returns the semantic symbol of the provided syntax node
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="context">The syntax analysis context</param>
		/// <returns>The symbol of the provided syntax node</returns>
		public static TSymbolType GetSymbolAs<TSymbolType>(this SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context) where TSymbolType : class, ISymbol
		{
			return context.SemanticModel.GetSymbolInfo(syntaxNode).Symbol as TSymbolType;
		}

		/// <summary>
		/// Returns the semantic declared symbol of the provided syntax node as the provided symbol type
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="context">The syntax analysis context</param>
		/// <returns>The symbol of the provided syntax node</returns>
		public static TSymbolType GetDeclaredSymbolAs<TSymbolType>(this SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context) where TSymbolType : class, ISymbol
		{
			return context.SemanticModel.GetDeclaredSymbol(syntaxNode) as TSymbolType;
		}

		/// <summary>
		/// Returns the semantic declared symbol of the provided syntax node as the provided symbol type
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="model">The syntax analysis context</param>
		/// <returns>The symbol of the provided syntax node</returns>
		public static TSymbolType GetDeclaredSymbolAs<TSymbolType>(this SyntaxNode syntaxNode, SemanticModel model) where TSymbolType : class, ISymbol
		{
			return model.GetDeclaredSymbol(syntaxNode) as TSymbolType;
		}

		/// <summary>
		/// Returns the semantic symbol of the provided syntax node, or the declared symbol if the previous cast was invalid
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="context">The syntax analysis context</param>
		/// <returns>The symbol or declared symbol of the provided syntax node</returns>
		public static TSymbolType GetSymbolOrDeclaredAs<TSymbolType>(this SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context) where TSymbolType : class, ISymbol
		{
			return syntaxNode.GetSymbolAs<TSymbolType>(context) ?? syntaxNode.GetDeclaredSymbolAs<TSymbolType>(context);
		}

		/// <summary>
		/// Returns the semantic symbol of the provided syntax node, or the declared symbol if the previous cast was invalid
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="model">The syntax semantic model</param>
		/// <returns>The symbol or declared symbol of the provided syntax node</returns>
		public static TSymbolType GetSymbolOrDeclaredAs<TSymbolType>(this SyntaxNode syntaxNode, SemanticModel model) where TSymbolType : class, ISymbol
		{
			return syntaxNode.GetSymbolAs<TSymbolType>(model) ?? syntaxNode.GetDeclaredSymbolAs<TSymbolType>(model);
		}

		/// <summary>
		/// Finds first ancestor node such that predicate is met, unless terminateAt is met first. Returns null if terminateAt is true or if no ancestor meets predicate.
		/// </summary>
		/// <param name="node">The child node to start from.</param>
		/// <param name="predicate">The condition to return an ancestor.</param>
		/// <param name="terminateAt">If this is true for any ancestor, the search will be terminated.</param>
		/// <returns>The ancestor node that meets the predicate condition, or null if terminateAt is satisfied first or if no ancestor is found.</returns>
		public static SyntaxNode GetFirstAncestorWhere(this SyntaxNode node, Func<SyntaxNode, bool> predicate,
			Func<SyntaxNode, bool> terminateAt = null)
		{
			terminateAt = terminateAt ?? (_ => false);

			return node.Ancestors()
				.TakeWhile(n => !terminateAt(n))
				.Where(predicate)
				.FirstOrDefault();
		}

		/// <summary>
		/// Finds first ancestor node of type T, unless terminateAt is satisfied first. Returns null if terminateAt is true or if no ancestor is of type T.
		/// </summary>
		/// <typeparam name="T">The SyntaxNode type of interest.</typeparam>
		/// <param name="node">The child node to start from.</param>
		/// <param name="terminateAt">If this is true for any ancestor, the search will be terminated.</param>
		/// <returns>The first ancestor node of type T, or null if terminateAt is satisfied first or if no ancestor of type T is found.</returns>
		public static T GetFirstAncestorOfType<T>(this SyntaxNode node, Func<SyntaxNode, bool> terminateAt = null)
			where T : SyntaxNode
		{
			return GetFirstAncestorWhere(node, n => n is T, terminateAt) as T;
		}

		/// <summary>
		/// Get ancestor of a particular type which could 'refer to' node. This will return null if it encounters a 'boundary' (eg a StatementSyntax not of the
		/// type of interest, a lambda expression, a ConditionalStatement that the node is on the left-hand side of, etc.), or if no ancestor of type T is found.
		/// </summary>
		/// <typeparam name="T">The SyntaxNode type of interest.</typeparam>
		/// <param name="node">The child node to start from.</param>
		/// <returns>The first ancestor node of type T, or null if a boundary node is encountered first or if no ancestor of type T is found.</returns>
		public static T GetReferringAncestorOfType<T>(this SyntaxNode node)
			where T : SyntaxNode
		{
			return node.GetFirstAncestorOfType<T>(terminateAt: n => !(n is T) && n.IsExtendedSyntaxOfType(ExtendedSyntaxType.ReferenceBoundary));
		}

		/// <summary>
		/// Returns true if a node is the condition in a conditional expression.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static bool IsNodeConditionInConditionalExpression(this SyntaxNode node)
		{
			return node != null && (node.Parent as ConditionalExpressionSyntax)?.Condition == node;
		}

		/// <summary>
		/// Returns the the type symbol that this syntax node provides, if it has one (i.e InvocationSyntax -> invocation return type)
		/// </summary>
		/// <param name="syntaxNode">The syntax node</param>
		/// <param name="context">The syntax analysis context</param>
		/// <param name="useConvertedType">If true, the converted type (implicit conversion) is returned</param>
		/// <returns>The type symbol that the syntax node represents</returns>
		public static ITypeSymbol GetAsTypeSymbol(this SyntaxNode syntaxNode, SyntaxNodeAnalysisContext context, bool useConvertedType = false)
		{
			var symbolTypeInfo = context.SemanticModel.GetTypeInfo(syntaxNode);
			var type = useConvertedType ? symbolTypeInfo.ConvertedType : symbolTypeInfo.Type;

			return type.IsOfType<IErrorTypeSymbol>(context) ? null : type;
		}


		/// <summary>
		/// Indicates if the current syntax node is of the trageted syntax type
		/// </summary>
		/// <typeparam name="TSymbolType"></typeparam>
		/// <param name="current"></param>
		/// <returns></returns>
		public static bool IsSyntaxOfType<TSymbolType>(this SyntaxNode current) where TSymbolType : SyntaxNode
		{
			return current is TSymbolType;
		}

		/// <summary>
		/// Returns the syntax node safe-casted into a certain syntax type
		/// </summary>
		/// <typeparam name="TSymbolType">The syntax type the node should be cast to</typeparam>
		/// <param name="current">The syntax node to convert</param>
		/// <returns></returns>
		public static TSymbolType GetAsSyntaxOfType<TSymbolType>(this SyntaxNode current) where TSymbolType : SyntaxNode
		{
			return current as TSymbolType;
		}

		public static SyntaxSymbolPairing<TSyntax, TSymbol> LinkNodeToSymbol<TSyntax, TSymbol>(
			this TSyntax syntaxNode,
			Func<TSyntax, TSymbol> syntaxToSymbol,
			SyntaxNodeAnalysisContext? context = null)
			where TSyntax : SyntaxNode where TSymbol : ISymbol
		{
			return syntaxNode.LinkNodeToSymbol(node => node, syntaxToSymbol, context);
		}

		public static SyntaxSymbolPairing<TSourceSyntax, TSymbol> LinkNodeToSymbol<TSourceSyntax, TResultSyntax, TSymbol>(
			this TSourceSyntax syntaxNode,
			Func<TSourceSyntax, TResultSyntax> syntaxTransform,
			Func<TResultSyntax, TSymbol> syntaxToSymbol,
			SyntaxNodeAnalysisContext? context = null)
			where TSourceSyntax : SyntaxNode
			where TResultSyntax : SyntaxNode
			where TSymbol : ISymbol
		{
			return new SyntaxSymbolPairing<TSourceSyntax, TSymbol>(
				syntaxNode,
				node => syntaxToSymbol(syntaxTransform(node)),
				node => context.HasValue ? node.GetAsTypeSymbol(context.Value) : null
			);
		}

		/// <summary>
		/// Links each syntax node to a specified semantic representation of that node. 
		/// </summary>
		/// <param name="syntaxNodes">The nodes to map to symbols</param>
		/// <param name="syntaxToSymbol">Describes the transition from syntax to symbol applied to a node
		///  to obtain the desired semantic representation of that node</param>
		/// <param name="context">[Optionnal] The analysis context. Must be provided if the 
		/// <see cref="SyntaxSymbolPairing{TSyntax,TSymbol}.TypeSymbol"/> needs to be computed</param>
		/// <returns>A self-contained pairing between the given syntax node and its equivalent semantic representation</returns>
		public static IEnumerable<SyntaxSymbolPairing<TSyntax, TSymbol>> LinkNodesToSymbols<TSyntax, TSymbol>(
			this IEnumerable<TSyntax> syntaxNodes,
			Func<TSyntax, TSymbol> syntaxToSymbol,
			SyntaxNodeAnalysisContext? context = null)
			where TSyntax : SyntaxNode where TSymbol : ISymbol
		{
			return syntaxNodes.Select(node => node.LinkNodeToSymbol(syntaxToSymbol, context));
		}

		/// <summary>
		/// Indicates if this node is directly or conditionally returned in the context of its declaration
		/// </summary>
		/// <param name="node">The node to check</param>
		/// <returns>Is the node is an active member of a return statement</returns>
		public static bool IsReturned(this SyntaxNode node)
		{
			if (node.Parent.IsSyntaxOfType<ReturnStatementSyntax>())
			{
				return true;
			}

			var ancestorsUntilReturn = node.Ancestors()
				.TakeWhile(ancestor => !ancestor.IsSyntaxOfType<ReturnStatementSyntax>() && IsConditionalOrCoalesce(node));

			var returnStatement = ancestorsUntilReturn.LastOrDefault()?.Parent?.GetAsSyntaxOfType<ReturnStatementSyntax>();
			return returnStatement != null && returnStatement.Expression.GetAsSyntaxOfType<ConditionalExpressionSyntax>()?.Condition != node;
		}

		private static bool IsConditionalOrCoalesce(SyntaxNode node)
		{
			return node.IsSyntaxOfType<ConditionalExpressionSyntax>() || node.IsKind(SyntaxKind.CoalesceExpression);
		}

		/// <summary>
		/// Returns the nearest ancestor of type IfStatementSyntax where the syntax node is declared
		/// </summary>
		/// <param name="syntaxNode">The provided syntax node</param>
		/// <returns>The nearest ancestor of type IfStatementSyntax where the syntax node is declared</returns>
		public static IfStatementSyntax GetDeclaringIfStatement(this SyntaxNode syntaxNode)
		{
			return syntaxNode.FirstAncestorOrSelf<IfStatementSyntax>();
		}

		/// <summary>
		/// Return the nearest ancestor of any of the four loop type (for, while, do-while, foreach) where the syntax node is declared
		/// </summary>
		/// <param name="syntaxNode">The provided syntax node</param>
		/// <returns>The nearest ancestor of any of the four loop type (for, while, do-while, foreach) where the syntax node is declared</returns>
		public static StatementSyntax GetDeclaringLoopStatement(this SyntaxNode syntaxNode)
		{
			return syntaxNode.FirstAncestorOrSelf<ForEachStatementSyntax>() ?? syntaxNode.FirstAncestorOrSelf<ForStatementSyntax>() ??
				   (StatementSyntax)syntaxNode.FirstAncestorOrSelf<WhileStatementSyntax>() ?? syntaxNode.FirstAncestorOrSelf<DoStatementSyntax>();
		}

		/// <summary>
		/// Checks if the given node is a certain type of syntax. This type is actually one of the pre-defined check declared in ExtendedSyntaxType
		/// </summary>
		/// <param name="node">The current node</param>
		/// <param name="extendedSyntaxType">The target extended syntax type to check the current node against</param>
		/// <returns>True if the node matches the provided extended type</returns>
		public static bool IsExtendedSyntaxOfType(this SyntaxNode node, ExtendedSyntaxType extendedSyntaxType)
		{
			return extendedSyntaxType.CheckIfExtendedType(node);
		}

		/// <summary>
		/// Get the first ancestor which is a member declaration syntax 
		/// </summary>
		/// <param name="node">The current node</param>
		/// <param name="includeSelf">If true, the current node will be considered when checking for MemberDeclarationSyntax ancestors</param>
		/// <returns>The surrounding member declaration syntax</returns>
		public static MemberDeclarationSyntax GetSurroundingMemberDeclarationSyntax(this SyntaxNode node, bool includeSelf = false)
		{

			return GetSurroundingMemberDeclarationSyntax<MemberDeclarationSyntax>(node, includeSelf, false);
		}

		/// <summary>
		/// Get the first ancestor which is a member declaration syntax of the provided kind (class, method, namespace, etc) 
		/// </summary>
		/// <param name="node">The current node</param>
		/// <param name="includeSelf">If true, the current node will be considered when checking for MemberDeclarationSyntax ancestors</param>
		/// <param name="takeLast">If true, will grab the last surrounding declaration syntax of the given type. Useful to ignore of nested classes or namespaces</param>
		/// <returns>The surrounding member declaration syntax</returns>
		public static MemberDeclarationSyntax GetSurroundingMemberDeclarationSyntax<TMemberDeclarationType>(
			this SyntaxNode node,
			bool includeSelf = false,
			bool takeLast = false)
			where TMemberDeclarationType : MemberDeclarationSyntax
		{
			var ancestors = includeSelf ? node.AncestorsAndSelf() : node.Ancestors();
			var target = takeLast
				? ancestors.LastOrDefault(ancestor => ancestor.IsSyntaxOfType<TMemberDeclarationType>())
				: ancestors.FirstOrDefault(ancestor => ancestor.IsSyntaxOfType<TMemberDeclarationType>());

			return target as TMemberDeclarationType;
		}

		/// <summary>
		/// Checks if the current syntax node matches any of the given extended syntax type definitions
		/// </summary>
		/// <param name="node">The current node</param>
		/// <param name="extendedSyntaxTypes">The extended syntax types to match the current node agaisnt</param>
		/// <returns>True if the current node is of any of of the given extended types</returns>
		public static bool IsOneOfThoseExtendedSyntaxTypes(this SyntaxNode node, params ExtendedSyntaxType[] extendedSyntaxTypes)
		{
			return extendedSyntaxTypes.Any(t => t.CheckIfExtendedType(node));
		}

		/// <summary>
		/// Gets info about cyclomatic complexity, counting the number of predicates within the scope of rootNode and within each nested function
		/// (methods and anonymous functions). 
		/// </summary>
		/// <param name="rootNode">The SyntaxNode to measure cyclomatic complexity within</param>
		/// <returns>A sequence of RegionInfo objects, each containing the region-defining node (either rootNode or a function-defining node)
		/// and cyclomatic complexity values for the region. There will always be at least one RegionInfo returned for the rootNode,
		///  plus one for each nested function.</returns>
		public static IEnumerable<CyclomaticComplexityWalker.RegionInfo> GetCyclomaticComplexityInfo(this SyntaxNode rootNode, SemanticModel model)
		{
			var walker = new CyclomaticComplexityWalker(rootNode, model);
			walker.Visit(rootNode);
			return walker.Results;
		}

		/// <summary>
		/// Descends the hierarchy of <see cref="root"/>'s descendants depth-first, looking for nodes that match <see cref="keepPredicate"/>. 
		/// If an encountered node matches <see cref="keepPredicate"/>, it is returned, and its own descendants are ignored. 
		/// If an encountered node matches <see cref="skipPredicate"/>, it and its descendants are ignored.
		/// If <see cref="prioritizeSkip"/> is true, the skip predicate takes precedence over the keep predicate.
		/// </summary>
		/// <param name="root">The root syntax node to start the analysis at</param>
		/// <param name="keepPredicate">The predicate used to indicate if a node should be retained or not.</param>
		/// <param name="skipPredicate">The predicate used to indicate if a node (and its descendant) should be skipped or not.</param>
		/// <param name="prioritizeSkip">If true, even a node that matches the keep predicate will not be retained if it also matches the skip predicate</param>
		/// <returns>All the first occurences of the targeted nodes for each branch of the syntax tree under the given root</returns>
		public static IEnumerable<SyntaxNode> GetAllFirstMatchingSubTreeRoots(
			this SyntaxNode root,
			Func<SyntaxNode, bool> keepPredicate,
			Func<SyntaxNode, bool> skipPredicate = null,
			bool prioritizeSkip = DefaultPrioritizeSkip)
		{
			// Iterative depth first search of the tree
			var nodes = new Stack<SyntaxNode>(root.ChildNodes());
			while (nodes.Any())
			{
				var current = nodes.Pop();
				var skipCurrent = skipPredicate?.Invoke(current) ?? false;
				var keepCurrent = keepPredicate(current);

				if (prioritizeSkip && skipCurrent)
				{
					continue;
				}

				if (keepCurrent)
				{
					yield return current;
				}
				else if (!skipCurrent)
				{
					foreach (var child in current.ChildNodes())
					{
						nodes.Push(child);
					}
				}
			}
		}

		/// <summary>
		/// Descends the hierarchy of <see cref="root"/>'s descendants depth-first, looking for nodes that match <see cref="keepPredicate"/>. 
		/// If an encountered node matches <see cref="keepPredicate"/>, it is returned, and its own descendants are ignored. 
		/// If an encountered node matches <see cref="skipPredicate"/>, it and its descendants are ignored.
		/// If <see cref="prioritizeSkip"/> is true, the skip predicate takes precedence over the keep predicate.
		/// If no value is given for the <see cref="keepPredicate"/>, the predicate is assumed to be always 'true", and so any subtree root matching the 
		/// given type will be returned (while its descendants are ignored)
		/// </summary>
		/// <param name="root">The root syntax node to start the analysis at</param>
		/// <param name="keepPredicate">The predicate used to indicate if a node should be retained or not.</param>
		/// <param name="skipPredicate">The predicate used to indicate if a node (and its descendant) should be skipped or not.</param>
		/// <param name="prioritizeSkip">If true, even a node that matches the keep predicate will not be retained if it also matches the skip predicate</param>
		/// <returns>All the first occurences of the targeted nodes for each branch of the syntax tree under the given root</returns>
		public static IEnumerable<TNodeType> GetAllFirstMatchingSubTreeRootsOfType<TNodeType>(
			this SyntaxNode root,
			Func<SyntaxNode, bool> keepPredicate = null,
			Func<SyntaxNode, bool> skipPredicate = null,
			bool prioritizeSkip = DefaultPrioritizeSkip)
			where TNodeType : SyntaxNode
		{
			// We combine the 'keep' predicate with the type-matching predicate here
			Func<SyntaxNode, bool> combinedPredicate = node => node is TNodeType && (keepPredicate?.Invoke(node) ?? true);
			return root.GetAllFirstMatchingSubTreeRoots(node => combinedPredicate(node), skipPredicate, prioritizeSkip).Cast<TNodeType>();
		}
		/// <summary>
		/// Returns true if the node appears to be in generated code.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsInGeneratedCode(this SyntaxNode node)
		{
			var fileName = Path.GetFileName(node.SyntaxTree.FilePath);
			return IsFileNameForGeneratedCode(fileName);
		}

		/// <summary>
		/// Returns true if file name indicates that the code is generated.
		/// https://github.com/dotnet/roslyn/blob/master/src/Workspaces/Core/Portable/GeneratedCodeRecognition/GeneratedCodeRecognitionServiceFactory.cs
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private static bool IsFileNameForGeneratedCode(string fileName)
		{
			if (fileName.StartsWith("TemporaryGeneratedFile_", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			string extension = Path.GetExtension(fileName);
			if (extension != string.Empty)
			{
				fileName = Path.GetFileNameWithoutExtension(fileName);

				if (fileName.EndsWith("AssemblyInfo", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".designer", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".generated", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".g", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".g.i", StringComparison.OrdinalIgnoreCase) ||
					fileName.EndsWith(".AssemblyAttributes", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}
	}
}
