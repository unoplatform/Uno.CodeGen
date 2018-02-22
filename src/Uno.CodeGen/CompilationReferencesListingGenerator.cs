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
