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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Uno.CodeGen.ClassLifecycle.Utils
{
	internal static class StringExtensions
	{
		public static bool HasValue(this string text) 
			=> !string.IsNullOrWhiteSpace(text);

		public static string ToLowerCamelCase(this string text)
		{
			var chars = text?.ToArray();
			if (chars?.Length == 0)
			{
				return "";
			}

			chars[0] = char.ToLowerInvariant(chars[0]);
			return new string(chars);
		}

		public static string JoinBy(this IEnumerable<string> values, string separator)
			=> string.Join(separator, values);

		public static string JoinByEmpty(this IEnumerable<string> values)
			=> string.Join(string.Empty, values);

		public static IDisposable Indent(this IndentedTextWriter writer)
		{
			var capture = writer.Indent;
			writer.Indent += 1;
			return Disposable.Create(() => writer.Indent = capture);
		}

		public static IDisposable Block(this IndentedTextWriter writer)
		{
			var capture = writer.Indent;
			writer.Indent += 1;
			writer.WriteLine("{");
			return Disposable.Create(() =>
			{
				writer.WriteLine("}");
				writer.Indent = capture;
			});
		}

		public static IDisposable Block(this IndentedTextWriter writer, string text)
		{
			var capture = writer.Indent;
			writer.Indent += 1;
			writer.WriteLine(text);
			writer.WriteLine("{");
			return Disposable.Create(() =>
			{
				writer.WriteLine("}");
				writer.Indent = capture;
			});
		}

		public static IDisposable NameSpaceOf(this IndentedTextWriter writer, INamedTypeSymbol type)
		{
			return type.GetContainingTypes().Reverse().Aggregate(writer.Block($"namespace {type.ContainingNamespace}"), CreateContainingBlock);

			IDisposable CreateContainingBlock(IDisposable previousLevel, INamedTypeSymbol containingType)
			{
				var containingTypeDisposable = writer.Block($"partial class {containingType.Name}");

				return Disposable.Create(() =>
				{
					containingTypeDisposable.Dispose();
					previousLevel.Dispose();
				});
			}
		}

		private class Disposable : IDisposable
		{
			private Action _disposeAction;

			public static IDisposable Create(Action disposeAction) => new Disposable(disposeAction);

			private Disposable(Action disposeAction) => _disposeAction = disposeAction;

			/// <inheritdoc />
			public void Dispose() => Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
		}
	}
}