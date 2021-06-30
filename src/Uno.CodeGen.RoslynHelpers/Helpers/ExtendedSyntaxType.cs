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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Uno.RoslynHelpers.Helpers
{
	/// <summary>
	/// Used to provided delegate predicates, wrapped as ExtendedSyntaxType objects, to facilitate the 
	/// identification of more complex or composite syntax types that do not exist by default.
	/// </summary>
	public class ExtendedSyntaxType
	{
		public const bool DefaultConsiderParametersAsPotentialTargets = false;

		/// <summary>
		/// Provides all the potential syntax kinds that define binary expressions. Does not 
		/// include the various member access expression ("a.b", "a?.b", "a->b") syntax kinds
		/// </summary>
		public static readonly SyntaxKind[] BinaryExpressionSyntaxKinds =
			Enum.GetValues(typeof(SyntaxKind))
			.Cast<SyntaxKind>()
			.Select(SyntaxFacts.GetBinaryExpression)
			.Where(kind => kind != SyntaxKind.None)
			.ToArray();

		private static readonly HashSet<Type> _simplePredicateTypes = new HashSet<Type>
		{
			typeof (IfStatementSyntax),
			typeof (CaseSwitchLabelSyntax),
			typeof (ConditionalExpressionSyntax),
			typeof (WhileStatementSyntax),
			typeof (ForStatementSyntax),
			typeof (DoStatementSyntax)
		};

		private static readonly HashSet<SyntaxKind> _binaryExpressionPredicateConnectors = new HashSet<SyntaxKind>
		{
				SyntaxKind.LogicalOrExpression,
				SyntaxKind.LogicalAndExpression,
				SyntaxKind.CoalesceExpression
		};

		private static readonly HashSet<Type> _syntaxTypesWithOptionalBlock = new HashSet<Type>()
		{
			typeof(IfStatementSyntax),
			typeof(ElseClauseSyntax),
			typeof(ForStatementSyntax),
			typeof(ForEachStatementSyntax),
			typeof(WhileStatementSyntax),
			typeof(DoStatementSyntax),
			typeof(UsingStatementSyntax),
			typeof(SimpleLambdaExpressionSyntax),
			typeof(ParenthesizedLambdaExpressionSyntax),
			typeof(FixedStatementSyntax),
			typeof(LockStatementSyntax)
		};

		#region Delegate declarations

		public delegate bool CheckExtendedSyntaxKindDelegate(SyntaxNode node);

		#endregion

		#region Exposed delegate verification strategies 

		public CheckExtendedSyntaxKindDelegate CheckIfExtendedType { get; }

		#endregion


		#region Util methods

		/// <summary>
		/// Returns the extended version of the given syntax node, so that it can be used in methods that require extended syntax types
		/// </summary>
		/// <typeparam name="T">The syntax node type</typeparam>
		/// <returns>The extended syntax version of the given node</returns>
		public static ExtendedSyntaxType AsExtended<T>() where T : SyntaxNode
		{
			return new ExtendedSyntaxType(node => node is T);
		}

		#endregion


		#region Exposed extended rules

		/// <summary>
		/// Indicates if the given node is a loop (for, while, foreach, etc)
		/// </summary>
		public static readonly ExtendedSyntaxType LoopStatementSyntax = new ExtendedSyntaxType(IsLoopStatementSyntax);

		/// <summary>
		/// Indicates if the given node is returned as part of a "return" statement
		/// </summary>
		public static readonly ExtendedSyntaxType Returned = new ExtendedSyntaxType(IsReturned);

		/// <summary>
		/// Indicates if the given node is a conditional expression or a binary expression defined with a coalesce (??) operator
		/// </summary>
		public static readonly ExtendedSyntaxType ConditionalOrCoalesce = new ExtendedSyntaxType(IsConditionalOrCoalesce);

		/// <summary>
		/// Indicates if the given node is a reference boundary, which is to say that it is either a statement, a lambda expression, 
		/// or the condition of a conditional expression
		/// </summary>
		public static readonly ExtendedSyntaxType ReferenceBoundary = new ExtendedSyntaxType(IsReferenceBoundary);

		/// <summary>
		/// Indicates if the given node is the condition of a conditional expression
		/// </summary>
		public static readonly ExtendedSyntaxType ConditionInConditionalExpression = new ExtendedSyntaxType(IsNodeConditionInConditionalExpression);

		/// <summary>
		/// Indicates if the given node is a lambda expression
		/// </summary>
		public static readonly ExtendedSyntaxType LambdaExpression = new ExtendedSyntaxType(IsLambdaExpression);

		/// <summary>
		/// Indicates if the given node is an anonymous function (either a lambda or an anonymous method)
		/// </summary>
		public static readonly ExtendedSyntaxType AnonymousFunctionExpression = new ExtendedSyntaxType(IsAnonymousFunctionExpression);

		/// <summary>
		/// Indicates if the given node is a predicate that results in different conditional execution 
		/// paths when encountered (if statement, loop, binary operators, etc)
		/// </summary>
		public static readonly ExtendedSyntaxType SimplePredicate = new ExtendedSyntaxType(IsSimplePredicate);
		public static readonly ExtendedSyntaxType BodiedLambda = new ExtendedSyntaxType(IsBodiedLambda);

		public static readonly ExtendedSyntaxType TakesOptionalBlock = new ExtendedSyntaxType(CanTakeOptionalBlock);

		public static readonly ExtendedSyntaxType VariableAssignmentOrDeclarationTarget = new ExtendedSyntaxType(IsAssignmentOrDeclarationTarget);
		public static readonly ExtendedSyntaxType VariableAssignmentOrDeclarationSource = new ExtendedSyntaxType(IsAnonymousFunctionExpression);

		public static readonly ExtendedSyntaxType VariableAssignmentTarget = new ExtendedSyntaxType(IsAssignmentTarget);
		public static readonly ExtendedSyntaxType VariableAssignmentSource = new ExtendedSyntaxType(IsAssignmentSource);
		public static readonly ExtendedSyntaxType VariableDeclarationTarget = new ExtendedSyntaxType(IsDeclarationTarget);
		public static readonly ExtendedSyntaxType VariableDeclarationSource = new ExtendedSyntaxType(IsDeclarationSource);

		#endregion

		protected ExtendedSyntaxType(CheckExtendedSyntaxKindDelegate extendedCheckDelegate)
		{
			CheckIfExtendedType = extendedCheckDelegate;
		}

		#region Extended syntax check predicates

		private static bool IsAnonymousFunctionExpression(SyntaxNode node)
		{
			return IsLambdaExpression(node) || node.IsSyntaxOfType<AnonymousMethodExpressionSyntax>();
		}

		private static bool IsLoopStatementSyntax(SyntaxNode node)
		{
			var loopAncestor =
				node.FirstAncestorOrSelf<ForEachStatementSyntax>()
				?? node.FirstAncestorOrSelf<ForStatementSyntax>()
				?? node.FirstAncestorOrSelf<WhileStatementSyntax>()
				?? (StatementSyntax)node.FirstAncestorOrSelf<DoStatementSyntax>();

			return loopAncestor != null;
		}

		private static bool IsConditionalOrCoalesce(SyntaxNode node)
		{
			return node.IsSyntaxOfType<ConditionalExpressionSyntax>() || node.IsKind(SyntaxKind.CoalesceExpression);
		}

		/// <summary>
		/// Returns true if a node is a 'boundary' in the sense that its ancestors have no direct reference to its descendants. (eg a StatementSyntax, 
		/// a lambda expression, a ConditionalStatement that the node is on the far left-hand side of, etc.)
		/// </summary>
		/// <param name="node">The syntax node to check.</param>
		/// <returns>True if the node is a boundary, false otherwise.</returns>
		private static bool IsReferenceBoundary(SyntaxNode node)
		{
			return node is StatementSyntax ||
					IsLambdaExpression(node) ||
					IsNodeConditionInConditionalExpression(node) ||
					node is ArgumentListSyntax;
		}

		/// <summary>
		/// Indicates if this node is directly or conditionally returned in the context of its declaration (whether in a lambda or a declared method)
		/// </summary>
		/// <param name="node">The node to check</param>
		/// <returns>Is the node is an active member of a return statement</returns>
		private static bool IsReturned(SyntaxNode node)
		{
			// Checks if the given node is a lambda statement or is a non-lambda method return statement
			Func<SyntaxNode, bool> isLambdaOrMethodReturnPredicate = targetNode =>
			{
				return IsLambdaExpression(targetNode) ||
						(targetNode.Parent.IsSyntaxOfType<ReturnStatementSyntax>() &&
						targetNode.Parent.GetFirstAncestorWhere(ancestor => IsLambdaExpression(targetNode)) == null);
			};


			if (isLambdaOrMethodReturnPredicate(node.Parent))
			{
				return true;
			}

			// Get all the ancestors while we haven't encountered a return or a lambda statement, 
			// skipping over conditional, coalesce, cast, and parenthesized expressions.
			var ancestorSyntaxTypesThatCanBeEncountered = new[]
			{
				ConditionalOrCoalesce,
				AsExtended<ParenthesizedExpressionSyntax>(),
				AsExtended<CastExpressionSyntax>()
			};

			var ancestorsUntilReturnOrLambda = node
				.Ancestors()
				.TakeWhile(ancestor => !isLambdaOrMethodReturnPredicate(ancestor) &&
									   node.IsOneOfThoseExtendedSyntaxTypes(ancestorSyntaxTypesThatCanBeEncountered));

			var returnOrLambdaAncestorNode = ancestorsUntilReturnOrLambda.LastOrDefault()?.Parent;

			// Get what is returned
			// ReturnStatementSyntax: return <target>
			// SimpleLambdaExpressionSyntax: () => <target>
			// ParenthesizedLambdaExpressionSyntax: () => { return <target>}
			SyntaxNode returnedTarget;
			if (returnOrLambdaAncestorNode.IsSyntaxOfType<ReturnStatementSyntax>())
			{
				returnedTarget = returnOrLambdaAncestorNode
					.GetAsSyntaxOfType<ReturnStatementSyntax>()
					.Expression;
			}
			else if (node.IsSyntaxOfType<SimpleLambdaExpressionSyntax>())
			{
				returnedTarget = returnOrLambdaAncestorNode
					.GetAsSyntaxOfType<SimpleLambdaExpressionSyntax>()
					.Body
					?.GetAsSyntaxOfType<ReturnStatementSyntax>()
					?.Expression;
			}
			else if (node.IsSyntaxOfType<ParenthesizedLambdaExpressionSyntax>())
			{
				returnedTarget = returnOrLambdaAncestorNode
					.GetAsSyntaxOfType<ParenthesizedLambdaExpressionSyntax>()
					.Body
					.GetAsSyntaxOfType<BlockSyntax>()
					.Statements
					.FirstOrDefault();
			}
			else
			{
				return false;
			}

			// One final check to see if the returned target is not a condition of a conditional expression (in which case it is not returned)
			return returnedTarget != null && returnedTarget.GetAsSyntaxOfType<ConditionalExpressionSyntax>()?.Condition != node;
		}

		private static bool IsLambdaExpression(SyntaxNode node)
		{
			return node.IsSyntaxOfType<SimpleLambdaExpressionSyntax>() ||
				   node.IsSyntaxOfType<ParenthesizedLambdaExpressionSyntax>();
		}

		private static bool IsNodeConditionInConditionalExpression(SyntaxNode node)
		{
			return node != null && (node.Parent as ConditionalExpressionSyntax)?.Condition == node;
		}
		private static bool IsAssignmentTarget(SyntaxNode node)
		{
			return node != null && node.Parent.GetAsSyntaxOfType<AssignmentExpressionSyntax>()?.Left == node;
		}

		private static bool IsDeclarationTarget(SyntaxNode node)
		{
			return node.IsSyntaxOfType<VariableDeclaratorSyntax>() || node.IsSyntaxOfType<ParameterSyntax>();
		}

		private static bool IsAssignmentOrDeclarationTarget(SyntaxNode node)
		{
			return IsAssignmentTarget(node) || IsDeclarationTarget(node);
		}

		private static bool IsAssignmentSource(SyntaxNode node)
		{
			return node != null && node.Parent.GetAsSyntaxOfType<AssignmentExpressionSyntax>()?.Right == node;
		}

		private static bool IsDeclarationSource(SyntaxNode node)
		{
			var grandParent = node?.Parent?.Parent;
			if (grandParent == null)
			{
				return false;
			}

			return (grandParent.GetAsSyntaxOfType<VariableDeclaratorSyntax>()?.Initializer?.Value == node);
		}

		private static bool IsAssignmentOrDeclarationSource(SyntaxNode node)
		{
			return IsAssignmentSource(node) || IsDeclarationSource(node);
		}

		private static bool IsSimplePredicate(SyntaxNode node)
		{
			return _simplePredicateTypes.Contains(node.GetType()) || _binaryExpressionPredicateConnectors.Contains(node.Kind());
		}

		/// <summary>
		/// Indicates if the syntax node is a type that may optionally be followed by a BlockSyntax (curly brackets): if, else, loops, usings, lambdas...
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		private static bool CanTakeOptionalBlock(SyntaxNode node)
		{
			return _syntaxTypesWithOptionalBlock.Contains(node.GetType());
		}

		private static bool IsBodiedLambda(SyntaxNode node)
		{
			var nodeAsSimpleLambda = node as SimpleLambdaExpressionSyntax;
			if (nodeAsSimpleLambda != null)
			{
				return nodeAsSimpleLambda.Body is BlockSyntax;
			}

			var nodeAsParenthesizedLambda = node as ParenthesizedLambdaExpressionSyntax;
			if (nodeAsParenthesizedLambda != null)
			{
				return nodeAsParenthesizedLambda.Body is BlockSyntax;
			}

			return false;
		}


		#endregion
	}
}
