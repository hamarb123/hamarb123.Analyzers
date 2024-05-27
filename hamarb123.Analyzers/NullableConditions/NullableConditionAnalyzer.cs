using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.VisualBasic;

namespace hamarb123.Analyzers.NullableConditions;

[DiagnosticAnalyzer(LanguageNames.VisualBasic)]
public sealed class NullableConditionAnalyzer : DiagnosticAnalyzer
{
	private const string DiagnosticId = "HAM0002";
	private const string Title = "The condition is nullable";
	private const string MessageFormat = "The condition '{0}' is nullable, and therefore may produce unexpected results";
	private const string Description = "The condition is nullable.";
	private const string Category = "Correctness";

	private static readonly DiagnosticDescriptor _rule = new(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(_rule);

	public override void Initialize(AnalysisContext context)
	{
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics); //todo: do we want None?
		context.RegisterOperationAction(AnalyzeOperation, OperationKind.Conditional);
	}

	private void AnalyzeOperation(OperationAnalysisContext context)
	{
		//check if it should run
		if (!context.Options.ShouldRun("HAM0002")) return;

		//get the operation
		var op = (IConditionalOperation)context.Operation;

		switch (op.Condition)
		{
			case IInvocationOperation i:
			{
				//check if we have an implicit call to Nullable<bool>.GetValueOrDefault
				if (i.IsImplicit && i.Type?.SpecialType == SpecialType.System_Boolean && !i.TargetMethod.IsStatic
					&& i.TargetMethod.ContainingType.GetFullMetadataName() == "System.Nullable`1" && i.TargetMethod.ContainingType?.TypeArguments[0].SpecialType == SpecialType.System_Boolean
					&& i.TargetMethod.ReturnType.SpecialType == SpecialType.System_Boolean && i.TargetMethod.Parameters.Length == 0
					)
				{
					//report the diagnostic
					goto report;
				}

				break;
			}
			case IConversionOperation c:
			{
				//check if we're implicitly converting with an IConversionOperation (e.g. for 'Nothing', but not for 'New TypeWithConversionToBoolean()')
				if (c.IsImplicit && c.Type?.SpecialType == SpecialType.System_Boolean && !c.Conversion.IsUserDefined)
				{
					//report the diagnostic
					goto report;
				}

				break;
			}
		}

		//nothing to report
		return;

		//goto statement to report the diagnostic
		report:
		context.ReportDiagnostic(Diagnostic.Create(_rule, op.Condition.Syntax.GetLocation(), op.Condition.Syntax.ToString()));
	}
}
