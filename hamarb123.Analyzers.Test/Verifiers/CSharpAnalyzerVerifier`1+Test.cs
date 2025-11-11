using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace hamarb123.Analyzers.Test
{
	public static partial class CSharpAnalyzerVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		public class Test : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
		{
			public Test(bool isLibrary, ReferenceAssemblies referenceAssemblies = null)
			{
				SolutionTransforms.Add((solution, projectId) =>
				{
					var compilationOptions = solution.GetProject(projectId).CompilationOptions;
					compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
						compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
					if (!isLibrary) compilationOptions = compilationOptions.WithOutputKind(OutputKind.ConsoleApplication);
					solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);

					return solution;
				});
				ReferenceAssemblies = referenceAssemblies ?? GlobalValues.Net100;
			}
		}
	}
}
