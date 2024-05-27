﻿using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.VisualBasic.Testing;

namespace hamarb123.Analyzers.Test
{
	public static partial class VisualBasicAnalyzerVerifier<TAnalyzer>
		where TAnalyzer : DiagnosticAnalyzer, new()
	{
		public class Test : VisualBasicAnalyzerTest<TAnalyzer, XUnitVerifier>
		{
			public Test()
			{
			}
		}
	}
}