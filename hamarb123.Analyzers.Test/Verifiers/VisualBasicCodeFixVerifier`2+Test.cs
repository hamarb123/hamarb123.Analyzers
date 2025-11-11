using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace hamarb123.Analyzers.Test
{
	public static partial class VisualBasicCodeFixVerifier<TAnalyzer, TCodeFix>
		where TAnalyzer : DiagnosticAnalyzer, new()
		where TCodeFix : CodeFixProvider, new()
	{
		public class Test : VisualBasicCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
		{
		}
	}
}
