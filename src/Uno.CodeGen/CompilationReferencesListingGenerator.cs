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
using System.Linq;
using System.Text;
using Uno.SourceGeneration;

namespace Uno
{
	/// <summary>
	/// Simple generator outputing a list of all references used to compile the project.
	/// </summary>
	/// <remarks>
	/// Generates only one file containing only comments.
	/// </remarks>
	public class CompilationReferencesListingGenerator : SourceGenerator
	{
		/// <inheritdoc />
		public override void Execute(SourceGeneratorContext context)
		{
			var output = new StringBuilder();

			output.AppendLine("// This is the list of all external references used to compile this project:");

			var lines = context
				.Compilation
				.References
				.OrderBy(r => r.Display, StringComparer.InvariantCultureIgnoreCase)
				.Select((r, i) => $"// #{i}: {r.Display}");

			foreach (var line in lines)
			{
				output.AppendLine(line);
			}

			var filename = $"{nameof(CompilationReferencesListingGenerator)}.cs";
			context.AddCompilationUnit(filename, output.ToString());
		}
	}
}
