using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace hamarb123.Analyzers.PassByIn;

[ExportCodeFixProvider(LanguageNames.CSharp)]
public sealed class PassByInCodeFix : CodeFixProvider
{
#pragma warning disable IDE0303 // Simplify collection initialization
	public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create("HAM0008");
#pragma warning restore IDE0303 // Simplify collection initialization

	public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		// Get the first diagnostic reported by the analyzer.
		var diagnostic = context.Diagnostics.Single();
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Get the syntax root of the document.
		if (await context.Document.GetSyntaxRootAsync(context.CancellationToken) is not CompilationUnitSyntax root)
		{
			return;
		}

		// Find the argument syntax node that corresponds to the diagnostic span.
		var node = root.FindNode(diagnosticSpan);
		var argumentSyntax = node as ArgumentSyntax;
		if (argumentSyntax is null)
		{
			context.CancellationToken.ThrowIfCancellationRequested();

			while (node is ExpressionSyntax)
			{
				node = node.Parent;
			}

			if (node is ArgumentSyntax argumentSyntax2)
			{
				argumentSyntax = argumentSyntax2;
			}
			else
			{
				return;
			}
		}

		// Register a code fix action that will add the 'in' keyword to the argument.
		context.RegisterCodeFix
		(
			CodeAction.Create
			(
				"Add `in` keyword",
				createChangedDocument: (ct) => AddIn(context.Document, root, argumentSyntax, ct),
				equivalenceKey: nameof(PassByInCodeFix)
			),
			diagnostic
		);
	}

	private static async Task<Document> AddIn(Document document, CompilationUnitSyntax root, ArgumentSyntax argumentSyntax, CancellationToken ct)
	{
		// Apply 'in' keyword to the argument.
		var newArgumentSyntax = argumentSyntax.WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.InKeyword));

		// If argument is a ternary, we need to add 'ref' to the true and false expressions if they do not already have it, and we need to do it recursively for nested ternaries.
		var adjustedExpr = AdjustExpression(newArgumentSyntax.Expression);
		if (adjustedExpr != newArgumentSyntax.Expression) newArgumentSyntax = newArgumentSyntax.WithExpression(adjustedExpr);
		ExpressionSyntax AdjustExpression(ExpressionSyntax expression)
		{
			// Check cancellation:
			ct.ThrowIfCancellationRequested();

			// Check if parenthesized expression, if so, adjust the inner expression.
			if (expression is ParenthesizedExpressionSyntax parenExpr)
			{
				var adjustedInnerExpr = AdjustExpression(parenExpr.Expression);
				if (adjustedInnerExpr != parenExpr.Expression) return parenExpr.WithExpression(adjustedInnerExpr);
				return expression;
			}

			// Check if conditional expression, if so, adjust the true and false expressions.
			if (expression is ConditionalExpressionSyntax condExpr)
			{
				// Apply recursively.
				var adjustedTrueExpr = AdjustExpression(condExpr.WhenTrue);
				var adjustedFalseExpr = AdjustExpression(condExpr.WhenFalse);

				// Add 'ref' to the true and false expressions if they do not already have it.
				if (adjustedTrueExpr is not RefExpressionSyntax) adjustedTrueExpr = SyntaxFactory.RefExpression(adjustedTrueExpr);
				if (adjustedFalseExpr is not RefExpressionSyntax) adjustedFalseExpr = SyntaxFactory.RefExpression(adjustedFalseExpr);

				// Make new instance if required.
				if (adjustedTrueExpr != condExpr.WhenTrue || adjustedFalseExpr != condExpr.WhenFalse)
				{
					return condExpr.WithWhenTrue(adjustedTrueExpr).WithWhenFalse(adjustedFalseExpr);
				}

				// Return:
				return expression;
			}

			// Return:
			return expression;
		}

		// Replace the old argument syntax with the new one in the syntax tree.
		var newRoot = root.ReplaceNode(argumentSyntax, newArgumentSyntax);

		// Create a new document with the updated syntax tree.
		return document.WithSyntaxRoot(newRoot);
	}
}
