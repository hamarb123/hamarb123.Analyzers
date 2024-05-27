using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace hamarb123.Analyzers.StringNonOrdinal;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class StringNonOrdinalAnalyzer : DiagnosticAnalyzer
{
	private const string DiagnosticId = "HAM0004";
	private const string Title = "String operation does not use StringComparison.Ordinal by default";
	private const string MessageFormat = "Call to member '{0}' does not use StringComparison.Ordinal by default";
	private const string Description = "String operation does not use StringComparison.Ordinal by default.";
	private const string Category = "Correctness";

	private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

	private static readonly ImmutableArray<DiagnosticDescriptor> _rules = ImmutableArray.Create(_rule);
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _rules;

	public sealed override void Initialize(AnalysisContext context)
	{
		//configure options
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		//register callback
		context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
	}

	private static void AnalyzeInvocation(OperationAnalysisContext context)
	{
		//check if it should run
		if (!context.Options.ShouldRun("HAM0004")) return;

		//run it
		if (context.Operation is IInvocationOperation op)
		{
			//check if we're calling a method on string
			var method = op.TargetMethod;
			if (method.ContainingType.SpecialType != SpecialType.System_String) return;

			//check if it's a problematic method name
			var problematic = (method.IsStatic, method.Name) switch
			{
				(false, "IndexOf") => true,
				(false, "LastIndexOf") => true,
				(_, "Compare") => true,
				(false, "CompareTo") => true,
				(false, "EndsWith") => true,
				(_, "Equals") => true,
				(false, "StartsWith") => true,
				_ => false,
			};
			if (!problematic) return;

			//check if our first parameter is a string
			var parameters = method.Parameters;
			if (parameters.Length == 0 || parameters[0].Type.SpecialType != SpecialType.System_String) return;

			//check if we don't take a StringComparison or CultureInfo parameter
			if (!parameters.Any((x) => x.Type.GetFullMetadataName() is "System.StringComparison" or "System.Globalization.CultureInfo"))
			{
				//report diagnostic
				context.ReportDiagnostic(Diagnostic.Create(_rule, op.Syntax.GetLocation(), method.Name));
			}
		}
	}
}
