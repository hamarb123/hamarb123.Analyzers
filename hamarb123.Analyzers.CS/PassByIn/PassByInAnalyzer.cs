using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace hamarb123.Analyzers.PassByIn;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PassByInAnalyzer : DiagnosticAnalyzer
{
	private const string DiagnosticId1 = "HAM0008";
	private const string Title1 = "Argument should be passed by in";
	private const string MessageFormat1 = "Argument '{0}' should be passed by in";
	private const string Description1 = "Arguments should be passed by in when possible.";
	private const string Category1 = "Style";

	private static readonly DiagnosticDescriptor _rule1 = new(DiagnosticId1, Title1, MessageFormat1, Category1, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description1);

#pragma warning disable IDE0303 // Simplify collection initialization
	private static readonly ImmutableArray<DiagnosticDescriptor> _rules = ImmutableArray.Create(_rule1);
#pragma warning restore IDE0303 // Simplify collection initialization
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _rules;

	public sealed override void Initialize(AnalysisContext context)
	{
		// Configure options
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		// Register callbacks
		context.RegisterCompilationStartAction(CompilationStartAction);
	}

	private void CompilationStartAction(CompilationStartAnalysisContext context)
	{
		// Get the enabled analysers
		bool runHAM0008 = context.Options.ShouldRun("HAM0008", isStyleAnalyzer: true);

		// If enabled, register stuff
		if (runHAM0008)
		{
			context.RegisterOperationAction(AnalyzeArgument, OperationKind.Argument);
		}
	}

	private void AnalyzeArgument(OperationAnalysisContext context)
	{
		if (context.Operation is IArgumentOperation { Parameter.RefKind: RefKind.In, ArgumentKind: ArgumentKind.Explicit } op && op.Syntax is ArgumentSyntax s && s.RefKindKeyword.IsKind(SyntaxKind.None))
		{
			var containingSymbol = op.SemanticModel?.GetEnclosingSymbol(op.Syntax.Span.Start, context.CancellationToken);
			bool inStructInstanceMember = containingSymbol is { IsStatic: false, ContainingType.IsValueType: true };
			bool hasLocation = HasLocation(op.Value.Syntax, op.Value, inStructInstanceMember, context.CancellationToken);
			if (hasLocation)
			{
				var diagnostic = Diagnostic.Create(_rule1, op.Value.Syntax.GetLocation(), op.Parameter.Name);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}

	private static bool HasLocation(SyntaxNode? node, IOperation? operation, bool inStructInstanceMember, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (node?.Kind() is SyntaxKind.PointerIndirectionExpression or SyntaxKind.PointerMemberAccessExpression or SyntaxKind.ElementAccessExpression)
		{
			return true;
		}

		return operation switch
		{
			ILocalReferenceOperation => true,
			IParameterReferenceOperation => true,
			IFieldReferenceOperation => true,
			IPropertyReferenceOperation { Property.RefKind: not RefKind.None } => true,
			IInvocationOperation { TargetMethod.RefKind: not RefKind.None } => true,
			IFunctionPointerInvocationOperation { Target.Type: IFunctionPointerTypeSymbol { Signature.RefKind: not RefKind.None } } => true,
			IArrayElementReferenceOperation => true,
#if ROSLYN_4_7_0_OR_GREATER
			IInlineArrayAccessOperation => true,
#endif
			IInstanceReferenceOperation when inStructInstanceMember => true,
			IConditionalOperation { IsRef: true } => true,
			IConditionalOperation x when HasLocation(x.WhenTrue.Syntax, x.WhenTrue, inStructInstanceMember, cancellationToken) && HasLocation(x.WhenFalse?.Syntax, x.WhenFalse, inStructInstanceMember, cancellationToken) => true,
			IImplicitIndexerReferenceOperation { IndexerSymbol: IMethodSymbol { RefKind: not RefKind.None } or IPropertySymbol { RefKind: not RefKind.None } } => true,
			_ => false,
		};
	}
}
