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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
	public static class MethodSymbolExtensions
	{
		private const bool DefaultMatchUsingInheritance = true;

		private static bool HasMatchingParametersWith(this IMethodSymbol current, IMethodSymbol other)
		{
			var currentArgTypes = current.Parameters.Select(param => param.Type);
			var otherArgTypes = other.Parameters.Select(param => param.Type);
			return currentArgTypes.SequenceEqual(otherArgTypes);
		}

		/// <summary>
		/// Check if two methods have the same signature, which means same name, return type and parameters
		/// </summary>
		/// <param name="current">The current method</param>
		/// <param name="other">Another method</param>
		/// <param name="considerNameForEquivalence">If true, the name of the methods will be used for the equality comparaison</param>
		/// <returns>True if the methods have the same signature</returns>
		public static bool IsSameSignatureAs(this IMethodSymbol current, IMethodSymbol other, bool considerNameForEquivalence = true)
		{
			current = current.GetReducedFromOrSelf();
			other = other.GetReducedFromOrSelf();

			return (!considerNameForEquivalence || current.Name == other.Name) &&
					current.ReturnType.EqualsType(other.ReturnType) &&
					current.HasMatchingParametersWith(other);
		}

		/// <summary>
		/// Indicates if a candidate method is the async equivalent of another target method
		/// </summary>
		/// <param name="asyncCandidate">The potential method symbol that is the async equivalent of the target</param>
		/// <param name="target">The target method</param>
		/// <param name="context">The analysis context</param>
		/// <param name="compareUsingTaskEquivalence">
		/// If true, the comparison will include a check to see if the candidate has a matching Task as its return type
		/// For example, Task&lt;int&gt; Foo() is the equivalent of int Foo()
		/// </param>
		/// <param name="needsToHaveAsyncKeyword">If true, a positive match will require the candidate method to be declared with the async keyword</param>
		/// <returns>True if the candidate method is the async equivalent of the target method</returns>
		public static bool IsAsyncEquivalentOf(
			this IMethodSymbol asyncCandidate,
			IMethodSymbol target,
			SyntaxNodeAnalysisContext context,
			bool compareUsingTaskEquivalence = false,
			bool needsToHaveAsyncKeyword = false)
		{
			if (needsToHaveAsyncKeyword && !asyncCandidate.IsAsync)
			{
				return false;
			}

			var hasMatchingParameters = asyncCandidate.HasMatchingParametersWith(target);
			var matchingAsyncEquivalentName = $"{target.Name}Async";

			asyncCandidate = asyncCandidate.GetReducedFromOrSelf();
			target = target.GetReducedFromOrSelf();

			if (!asyncCandidate.Name.Equals(matchingAsyncEquivalentName, StringComparison.OrdinalIgnoreCase) ||
				!hasMatchingParameters)
			{
				return false;
			}

			if (!compareUsingTaskEquivalence)
			{
				return true;
			}

			var asyncCandidateReturnType = asyncCandidate.ReturnType as INamedTypeSymbol;

			return asyncCandidateReturnType != null &&
				   asyncCandidateReturnType.IsGenericType &&
				   asyncCandidate.ReturnType.OriginalDefinition.DerivesFromType<Task>(context) &&
				   asyncCandidateReturnType.TypeArguments.FirstOrDefault()?.Equals(target.ReturnType) == true;
		}

		/// <summary>
		/// Indicates if the current method is of a given name and part of a given type
		/// </summary>
		/// <param name="methodSymbol">The current method symbol</param>
		/// <param name="methodName">The name of the to check in the provided type</param>
		/// <param name="typeName">The name of the type that the current method must be contained in</param>
		/// <param name="context">The context</param>
		/// <param name="matchUsingInheritance">If true, the comparison will involve checking if the method's containing type derives from the given type. 
		/// If false, the comparison will compare the method's containing type and the given type directly for equality instead</param>
		/// <returns>True if a method with the same name as the current method exists in the given type</returns>
		public static bool IsNamedMethodOnType(
			this IMethodSymbol methodSymbol,
			string methodName,
			string typeName,
			SyntaxNodeAnalysisContext context,
			bool matchUsingInheritance = DefaultMatchUsingInheritance)
		{
			if (methodSymbol == null || methodSymbol.Name != methodName)
			{
				return false;
			}

			return matchUsingInheritance
				? methodSymbol.ContainingType.DerivesFromType(typeName, context)
				: methodSymbol.ContainingType.IsOfType(typeName, context);
		}

		/// <summary>
		/// Indicates if the current method is of a given name and contained in a given type
		/// </summary>
		/// <typeparam name="TContainingType">The type that the current method must be contained in</typeparam>
		/// <param name="methodSymbol">The current method symbol</param>
		/// <param name="methodName">The name that the current moethod name must match</param>
		/// <param name="context">The context</param>
		/// <param name="matchUsingInheritance">If true, the comparison will involve checking if the method's containing type derives from the given type. 
		/// If false, the comparison will compare the method's containing type and the given type directly for equality instead</param>
		/// <returns>True if a method with the same name as the current method exists in the given type</returns>
		public static bool IsNamedMethodOnType<TContainingType>(
			this IMethodSymbol methodSymbol,
			string methodName,
			SyntaxNodeAnalysisContext context,
			bool matchUsingInheritance = DefaultMatchUsingInheritance)
		{
			if (methodSymbol == null || methodSymbol.Name != methodName)
			{
				return false;
			}

			return matchUsingInheritance
				? methodSymbol.ContainingType.DerivesFromType<TContainingType>(context)
				: methodSymbol.ContainingType.IsOfType<TContainingType>(context);
		}

		/// <summary>
		/// Indicates if the current method is of a given name and contained in any of the given types
		/// </summary>
		/// <param name="methodSymbol">The current method symbol</param>
		/// <param name="methodName">The name that the current moethod name must match</param>
		/// <param name="context">The context</param>
		/// <param name="matchUsingInheritance">If true, the types will be compared by checking if the method's containing type derives from any of the given types. 
		/// If false, the types will be compared for equality instead</param>
		/// <param name="typeNames">The name of the types in one of which the current method must be contained</param>
		/// <returns>True if a method with the same name as the current method exists in the given type</returns>
		public static bool IsNamedMethodOnAnyOfTheseTypes(
			this IMethodSymbol methodSymbol,
			string methodName,
			SyntaxNodeAnalysisContext context,
			bool matchUsingInheritance = DefaultMatchUsingInheritance,
			params string[] typeNames)
		{
			return typeNames.Any(typeName => methodSymbol.IsNamedMethodOnType(methodName, typeName, context, matchUsingInheritance));
		}

		/// <summary>
		/// Checks if a similar method is declared and accessible in a given type
		/// </summary>
		/// <param name="currentMethod">The method to check</param>
		/// <param name="type">The type to check agaisnt</param>
		/// <param name="context">The syntax analysis context</param>
		/// <param name="considerNameForEquivalence">If true, the name of the methods will be used for the equality comparaison</param>
		/// <returns>True if the method belongs to this </returns>
		public static bool HasSimilarMethodDeclaredInType(
			this IMethodSymbol currentMethod,
			ITypeSymbol type,
			SyntaxNodeAnalysisContext context,
			bool considerNameForEquivalence = true)
		{
			var typeMethods = currentMethod.ContainingType.EqualsType(type)
				? type.GetAllAccessibleMethodsFromWithinType(context, false, false, currentMethod.Name)
				: type.GetAllPubliclyAccessibleMethodsFromType(context, false, false, currentMethod.Name);

			return typeMethods.Any(m => m.IsSameSignatureAs(currentMethod, considerNameForEquivalence));
		}

		/// <summary>
		/// Checks if a similar method is declared and accessible in a given type or any of its ancestor
		/// </summary>
		/// <param name="currentMethod">The method to check</param>
		/// <param name="type">The type to check agaisnt</param>
		/// <param name="context">The syntax analysis context</param>
		/// <param name="considerNameForEquivalence">If true, the name of the methods will be used for the equality comparaison</param>
		/// <returns>True if the method belongs to this </returns>
		public static bool HasSimilarMethodDeclaredInTypeOrAncestors(
			this IMethodSymbol currentMethod,
			ITypeSymbol type,
			SyntaxNodeAnalysisContext context,
			bool considerNameForEquivalence = true)
		{
			var typeAndAncestorsMethods = currentMethod.ContainingType.EqualsType(type)
				? type.GetAllAccessibleMethodsFromWithinType(context, true, false, currentMethod.Name)
				: type.GetAllPubliclyAccessibleMethodsFromType(context, true, false, currentMethod.Name);

			return typeAndAncestorsMethods.Any(m => m.IsSameSignatureAs(currentMethod, considerNameForEquivalence));
		}

		/// <summary>
		/// Returns the reduced version of this method or the method itself
		/// </summary>
		/// <param name="currentMethod">The method to check</param>
		/// <returns>The method the current method was reduced from or the current method itself</returns>
		public static IMethodSymbol GetReducedFromOrSelf(this IMethodSymbol currentMethod)
		{
			return currentMethod.ReducedFrom ?? currentMethod;
		}

		/// <summary>
		/// Indicates if the current method is a constructor (normal, static, shared) or a destructor
		/// </summary>
		/// <param name="currentMethod">The current method</param>
		/// <returns>True if the current method is a constructor or a destructor</returns>
		public static bool IsConstructorOrDestructor(this IMethodSymbol currentMethod) => currentMethod.IsConstructor() || currentMethod.IsDestructor();

		/// <summary>
		/// Indicates if the current method is a constructor (normal, static, shared)
		/// </summary>
		/// <param name="currentMethod">The current method</param>
		/// <returns>True if the current method is a constructor or a destructor</returns>
		public static bool IsDestructor(this IMethodSymbol currentMethod) =>
			currentMethod.MethodKind == MethodKind.SharedConstructor ||
			currentMethod.MethodKind == MethodKind.StaticConstructor;

		/// <summary>
		/// Indicates if the current method is a destructor (normal, static, shared)
		/// </summary>
		/// <param name="currentMethod">The current method</param>
		/// <returns>True if the current method is a constructor or a destructor</returns>
		public static bool IsConstructor(this IMethodSymbol currentMethod) =>
			currentMethod.MethodKind == MethodKind.Constructor ||
			currentMethod.MethodKind == MethodKind.Destructor;

		/// <summary>
		/// Indicates if the method is a delegate invocation
		/// </summary>
		/// <param name="currentMethod">TThe current method</param>
		/// <returns>True if the method is a delegate invocation</returns>
		public static bool IsDelegateInvocation(this IMethodSymbol currentMethod)
		{
			return currentMethod.MethodKind == MethodKind.DelegateInvoke;
		}

		/// <summary>
		/// Indicates if the method if a lambda of an anonymous function declaration
		/// </summary>
		/// <param name="currentMethod"></param>
		/// <returns>True if the method is a lambda or delegate declaration</returns>
		public static bool IsLambdaOrDelegateDeclaration(this IMethodSymbol currentMethod)
		{
			return currentMethod.MethodKind == MethodKind.LambdaMethod || currentMethod.MethodKind == MethodKind.AnonymousFunction;
		}
	}
}
