using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace hamarb123.Analyzers
{
	public static class CustomCSharpExtensions
	{
		[return: NotNullIfNotNull(nameof(expr))]
		public static ExpressionSyntax? GetOperationExpression(this ExpressionSyntax? expr)
		{
			while (true)
			{
				if (expr is ParenthesizedExpressionSyntax p) expr = p.Expression;
				else if (expr.IsKind(SyntaxKind.SuppressNullableWarningExpression)) expr = ((PostfixUnaryExpressionSyntax)expr).Operand;
				else return expr;
			}
		}

		[return: NotNullIfNotNull(nameof(node))]
		public static SyntaxNode? GetOperationExpression(this SyntaxNode? node)
		{
			if (node is ExpressionSyntax es) return es.GetOperationExpression();
			else return node;
		}
	}
}
