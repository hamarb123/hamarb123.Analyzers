using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace hamarb123.Analyzers.GCRetrack;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GCRetrackAnalyzer : DiagnosticAnalyzer
{
	private const string DiagnosticId1 = "HAM0007";
	private const string Title1 = "GC retrack operation may be delayed, potentially indefinitely";
	private const string MessageFormat1 = "GC retrack operation is only guaranteed to occur immediately at byref local declaration site and for arguments at function call time - your code may rely on it occuring earlier";
	private const string Description1 = "GC retrack operation may be delayed until after other code could cause the operation to be incorrect, or may never occur.";
	private const string Category1 = "Correctness";

	private static readonly DiagnosticDescriptor _rule1 = new(DiagnosticId1, Title1, MessageFormat1, Category1, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description1);

#pragma warning disable IDE0303 // Simplify collection initialization
	private static readonly ImmutableArray<DiagnosticDescriptor> _rules = ImmutableArray.Create(_rule1);
#pragma warning restore IDE0303 // Simplify collection initialization
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => _rules;

	public sealed override void Initialize(AnalysisContext context)
	{
		//configure options
		context.EnableConcurrentExecution();
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

		//register callbacks
		context.RegisterCompilationStartAction(CompilationStartAction);
	}

	private void CompilationStartAction(CompilationStartAnalysisContext context)
	{
		if (context.Options.ShouldRun("HAM0007"))
		{
			ConcurrentDictionary<SyntaxNode, bool> doneReports = [];
			context.RegisterSyntaxNodeAction((ctx) => AnalyzeRefExpression(ctx, doneReports), SyntaxKind.RefExpression);
			context.RegisterSyntaxNodeAction((ctx) => AnalyzeArgumentSyntax(ctx, doneReports), SyntaxKind.Argument);
		}
	}

	private void AnalyzeArgumentSyntax(SyntaxNodeAnalysisContext context, ConcurrentDictionary<SyntaxNode, bool> doneReports)
	{
		//check if it's a gc retrack operation
		var node = context.Node as ArgumentSyntax;
		if (node is null) return;
		if (!IsArgumentSyntaxByRef(node, context.SemanticModel)) return;
		var byrefOperand = node.Expression.GetOperationExpression();
		if (!IsImportantGCRetrackExpression(byrefOperand, context.SemanticModel)) return;

		//analyze it
		if (context.SemanticModel.GetOperation(byrefOperand, context.CancellationToken) is not { } byrefOperandOp) return;
		AnalyzeRefOperandCandidate(context, byrefOperandOp, doneReports);
	}

	private void AnalyzeRefExpression(SyntaxNodeAnalysisContext context, ConcurrentDictionary<SyntaxNode, bool> doneReports)
	{
		//check if it's a gc retrack operation
		var node = context.Node as RefExpressionSyntax;
		if (node is null) return;
		var byrefOperand = node.Expression.GetOperationExpression();
		if (!IsImportantGCRetrackExpression(byrefOperand, context.SemanticModel)) return;

		//analyze it
		if (context.SemanticModel.GetOperation(byrefOperand, context.CancellationToken) is not { } byrefOperandOp) return;
		AnalyzeRefOperandCandidate(context, byrefOperandOp, doneReports);
	}

	private static bool IsArgumentSyntaxByRef(ArgumentSyntax node, SemanticModel semanticModel)
	{
		var argValue = node.Expression.GetOperationExpression();
		var argOp = semanticModel.GetOperation(argValue)?.Parent as IArgumentOperation;
		if (argOp?.Parameter is null) return false;
		if (argOp.Parameter.RefKind == RefKind.None) return false;
		return true;
	}

	private void AnalyzeRefOperandCandidate(SyntaxNodeAnalysisContext context, IOperation byrefOperandOp, ConcurrentDictionary<SyntaxNode, bool> doneReports)
	{
		//check if we're in a conditional expression, in which case we just take 1 of them
		while (byrefOperandOp.Parent is IConditionalOperation { IsRef: true }) byrefOperandOp = byrefOperandOp.Parent;

		//find the parent which is consuming it
		var consumingParent = byrefOperandOp.Parent;
		while (consumingParent is not null)
		{
			//these are the consuming parents we recognise
			if (consumingParent is ISimpleAssignmentOperation { IsRef: true }) break;
			if (consumingParent is IVariableDeclaratorOperation { Symbol.IsRef: true }) break;
			if (consumingParent is IReturnOperation) break;
			if (consumingParent is IBlockOperation) break;
			if (consumingParent is ISymbolInitializerOperation) break;
			if (consumingParent is IArgumentOperation { Parameter.RefKind: not RefKind.None })
			{
				consumingParent = consumingParent.Parent;
				break;
			}

			//these are the operations that mean we've missed the consuming operation, so we just exit & assume it's fine
			if (consumingParent is IExpressionStatementOperation) return;
			if (consumingParent is IMethodBodyBaseOperation) return;
			if (consumingParent is ILocalFunctionOperation) return;
			if (consumingParent is IAnonymousFunctionOperation) return;
			if (consumingParent is ILoopOperation) return;

			//otherwise, just go to parent
			consumingParent = consumingParent.Parent;
		}
		if (consumingParent is null) return;

		//loop upwards & check if later operations might depend on the gc retrack
		var parent = byrefOperandOp.Parent!;
		var lastParent = byrefOperandOp;
		bool mightNeedPriorRetrack = false;
		while (lastParent != consumingParent && parent is not null && !mightNeedPriorRetrack)
		{
			//move past current child
			var enumerator = parent.ChildOperations.GetEnumerator();
			while (true)
			{
				if (!enumerator.MoveNext()) return;
				if (enumerator.Current == lastParent)
				{
					if (!enumerator.MoveNext()) goto next;
					break;
				}
			}

			//check if any of the other children might depend on it
			do
			{
				if (MightNeedPriorRetrack(enumerator.Current, new()))
				{
					mightNeedPriorRetrack = true;
					break;
				}
			}
			while (enumerator.MoveNext());

			//move to next parent
			next:
			lastParent = parent;
			parent = parent.Parent;
		}
		if (!mightNeedPriorRetrack) return;

		//report the diagnostic
		var syntax = byrefOperandOp.Syntax;
		while (true)
		{
			if (syntax.Parent is ParenthesizedExpressionSyntax) syntax = syntax.Parent;
			else if (syntax.Parent.IsKind(SyntaxKind.SuppressNullableWarningExpression)) syntax = syntax.Parent;
			else break;
		}
		if (syntax.Parent is { }) syntax = syntax.Parent;
		if (doneReports.TryAdd(syntax, true)) context.ReportDiagnostic(Diagnostic.Create(_rule1, syntax.GetLocation()));
	}

	private static bool IsImportantGCRetrackExpression(SyntaxNode operand, SemanticModel semanticModel)
	{
		//check if it's a gc retrack expresion at all
		if (!IsGCRetrackExpression(operand, semanticModel, out var pointerOp)) return false;

		//check if it's just to a local (or constant value), in which case it doesn't matter
		while (pointerOp.Type is IPointerTypeSymbol or { SpecialType: SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_IntPtr or SpecialType.System_UIntPtr or SpecialType.System_Int32 or SpecialType.System_UInt32 })
		{
			if (pointerOp is IConversionOperation { OperatorMethod: null } co)
			{
				pointerOp = co.Operand;
			}
			else if (pointerOp is IBinaryOperation { OperatorMethod: null, RightOperand.ConstantValue.HasValue: true } bo1)
			{
				pointerOp = bo1.LeftOperand;
			}
			else if (pointerOp is IBinaryOperation { OperatorMethod: null, LeftOperand.ConstantValue.HasValue: true } bo2)
			{
				pointerOp = bo2.RightOperand;
			}
			else if (pointerOp is IAddressOfOperation aoo)
			{
				var operand2 = aoo.Reference;
				bool isPotentiallyToLocal = true;
				while (operand2 is IFieldReferenceOperation fro)
				{
					var node2 = fro.Syntax;
					if (node2.IsKind(SyntaxKind.PointerMemberAccessExpression))
					{
						isPotentiallyToLocal = false;
						break;
					}
					operand2 = fro.Instance;
				}
				if (isPotentiallyToLocal && operand2 is ILocalReferenceOperation or IParameterReferenceOperation) return false;
				return true;
			}
			else if (pointerOp.ConstantValue.HasValue)
			{
				return false; // Constant address
			}
			else
			{
				break;
			}
		}
		return true;
	}

	private static bool IsGCRetrackExpression(SyntaxNode operand, SemanticModel semanticModel, [NotNullWhen(true)] out IOperation? pointerOp)
	{
		pointerOp = null;
		SyntaxKind kind;
		while (true)
		{
			operand = operand.GetOperationExpression();
			kind = operand.Kind();
			if (kind == SyntaxKind.SimpleMemberAccessExpression && operand is MemberAccessExpressionSyntax maes)
			{
				var op = semanticModel.GetOperation(maes);
				if (op is IFieldReferenceOperation)
				{
					operand = maes.Expression;
					continue;
				}
			}
			break;
		}
		if (kind == SyntaxKind.PointerIndirectionExpression)
		{
			if (operand is not PrefixUnaryExpressionSyntax derefExpr) return false;
			var op = derefExpr.Operand.GetOperationExpression();
			pointerOp = semanticModel.GetOperation(op);
			if (pointerOp is IFieldReferenceOperation { Field.IsFixedSizeBuffer: true, Instance: { } field }) return IsGCRetrackExpression(field.Syntax, semanticModel, out pointerOp);
			return pointerOp?.Type is IPointerTypeSymbol { PointedAtType.SpecialType: not SpecialType.System_Void };
		}
		else if (kind == SyntaxKind.PointerMemberAccessExpression)
		{
			if (operand is not MemberAccessExpressionSyntax memberAccess) return false;
			var op = semanticModel.GetOperation(memberAccess);
			if (op is not IFieldReferenceOperation fro) return false;
			pointerOp = fro.Instance!;
			var childOps = pointerOp.ChildOperations;
			if (childOps.Count != 1) return false;
			pointerOp = childOps.First();
			return pointerOp?.Type is IPointerTypeSymbol { PointedAtType.SpecialType: not SpecialType.System_Void };
		}
		else if (kind == SyntaxKind.ElementAccessExpression)
		{
			if (operand is not ElementAccessExpressionSyntax elementAccess) return false;
			var expr = elementAccess.Expression.GetOperationExpression();
			pointerOp = semanticModel.GetOperation(expr);
			if (pointerOp is IFieldReferenceOperation { Field.IsFixedSizeBuffer: true, Instance: { } field }) return IsGCRetrackExpression(field.Syntax, semanticModel, out pointerOp);
			return pointerOp?.Type is IPointerTypeSymbol { PointedAtType.SpecialType: not SpecialType.System_Void };
		}
		else
		{
			return false;
		}
	}

	private struct CallerInfo
	{
		public bool IsForIFormatProvider;
		public bool DisallowMutableByRef;
	}

	private static bool MightNeedPriorRetrack(IOperation? op, CallerInfo callerInfo)
	{
		//try to have handling for as many ops as possible - some might not be fully handled, or recognize every case that is fine, but that's okay since the worst case is a false positive
		//most false positives can likely be fixed by just adding more known side-effect-free types / methods / properties
		//the idea here is to not warn unnecessarily for something like 'MyMethod(ref *x, 1.ToString() != null)', as even though the retrack doesn't occur immediately, it also doesn't matter since there's no way you could observe the retrack not having happened yet
		//note: we assume that the user isn't doing things like adding custom page fault handlers that could run arbitrary code, or modifying IL at runtime, etc.
		//note: we assume that a type with the same full name as a type in the BCL behaves the same way as that type in the BCL.
		//note: for simplicity we assume that gc retracks don't have any effects on textually prior gc retracks being correct (e.g., M(ref *ptr1, ref Unsafe.AsRef<int>(ptr2), ...)' could release a ptr1 pin if ptr2 moves, but this would require UB as you could never guarantee the timing of ptr2 retrack).
		while (true)
		{
			if (op is null) return false;
			else if (op.ConstantValue.HasValue) return false;
			else if (op is IConditionalOperation co) return MightNeedPriorRetrack(co.Condition, callerInfo with { IsForIFormatProvider = false }) || MightNeedPriorRetrack(co.WhenTrue, callerInfo) || MightNeedPriorRetrack(co.WhenFalse, callerInfo);
			else if (!callerInfo.IsForIFormatProvider && op is IFieldReferenceOperation fro) op = fro.Instance;
			else if (op is IConversionOperation co2 && (co2.OperatorMethod == null || (IsTypeSideEffectFree(co2.Type) && IsTypeSideEffectFree(co2.Operand.Type)))) op = co2.Operand;
			else if (op is IIsPatternOperation ipo) op = ipo.Value;
			else if (op is ISizeOfOperation soo && IsTypeSideEffectFree(soo.TypeOperand)) return false;
			else if (op is ITypeOfOperation too && IsTypeSideEffectFree(too.TypeOperand)) return false;
			else if (!callerInfo.IsForIFormatProvider && op is IObjectCreationOperation oco && IsTypeSideEffectFree(oco.Type)) return oco.Arguments.Any((x) => MightNeedPriorRetrack(x, callerInfo with { DisallowMutableByRef = true })) || MightNeedPriorRetrack(oco.Initializer, callerInfo);
			else if (!callerInfo.IsForIFormatProvider && op is ILocalReferenceOperation) return false;
			else if (!callerInfo.IsForIFormatProvider && op is IParameterReferenceOperation) return false;
			else if (callerInfo.IsForIFormatProvider && op is IPropertyReferenceOperation { Property: var p }) return !p.ContainingType.Is_System_Globalization_CultureInfo() || p.Name is not ("CurrentCulture" or "CurrentUICulture" or "InstalledUICulture" or "InvariantCulture");
			else if (op is IPropertyReferenceOperation pro && IsPropertySideEffectFree(pro.Property)) return MightNeedPriorRetrack(pro.Instance, callerInfo) || pro.Arguments.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is ITupleOperation to) return to.Elements.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is IUnaryOperation uo && (uo.OperatorMethod == null || IsTypeSideEffectFree(uo.Operand.Type))) op = uo.Operand;
			else if (op is IBinaryOperation bo && (bo.OperatorMethod == null || (IsTypeSideEffectFree(bo.LeftOperand.Type) && IsTypeSideEffectFree(bo.RightOperand.Type)))) return MightNeedPriorRetrack(bo.LeftOperand, callerInfo) || MightNeedPriorRetrack(bo.RightOperand, callerInfo);
			else if (op is ITupleBinaryOperation tbo && IsTypeSideEffectFree(tbo.LeftOperand.Type) && IsTypeSideEffectFree(tbo.RightOperand.Type)) return MightNeedPriorRetrack(tbo.LeftOperand, callerInfo) || MightNeedPriorRetrack(tbo.RightOperand, callerInfo);
			else if (op is IAddressOfOperation aoo) op = aoo.Reference;
			else if (!callerInfo.IsForIFormatProvider && op is IInvocationOperation io && IsMethodSideEffectFree(io.TargetMethod, out var allowRefsAlways) && IsTypeSideEffectFree(io.Instance?.Type)) return MightNeedPriorRetrack(io.Instance, callerInfo) || io.Arguments.Any((x) => MightNeedPriorRetrack(x, allowRefsAlways ? callerInfo : callerInfo with { DisallowMutableByRef = true }));
			else if (!callerInfo.IsForIFormatProvider && op is IArgumentOperation ao && ao.Parameter?.OriginalDefinition.Type.Is_System_IFormatProvider() == true) callerInfo.IsForIFormatProvider = true;
			else if (op is IArgumentOperation ao2 && (ao2.Parameter is { RefKind: not (RefKind.Ref or RefKind.Out) } || !callerInfo.DisallowMutableByRef || ao2.Value is IDeclarationExpressionOperation)) op = ao2.Value;
			else if (op is IAnonymousFunctionOperation afo) return false;
			else if (op is IAnonymousObjectCreationOperation aoco) return aoco.Initializers.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is IArrayCreationOperation aco) return aco.DimensionSizes.Any((x) => MightNeedPriorRetrack(x, callerInfo)) || MightNeedPriorRetrack(aco.Initializer, callerInfo);
			else if (!callerInfo.IsForIFormatProvider && op is IArrayElementReferenceOperation aero) return MightNeedPriorRetrack(aero.ArrayReference, callerInfo) || aero.Indices.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is IArrayInitializerOperation aio) return aio.ElementValues.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is IObjectOrCollectionInitializerOperation oocio) return oocio.Initializers.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is IBinaryPatternOperation bpo) return MightNeedPriorRetrack(bpo.LeftPattern, callerInfo) || MightNeedPriorRetrack(bpo.RightPattern, callerInfo);
			else if (op is IConstantPatternOperation) return false;
			else if (op is IDeclarationPatternOperation) return false;
			else if (op is IDiscardPatternOperation) return false;
			else if (op is IListPatternOperation lpo && IsTypeSideEffectFree(lpo.InputType) && IsSymbolSideEffectFree(lpo.LengthSymbol) && IsSymbolSideEffectFree(lpo.IndexerSymbol)) return lpo.Patterns.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is INegatedPatternOperation npo) op = npo.Pattern;
			else if (op is IRecursivePatternOperation rpo && IsTypeSideEffectFree(rpo.MatchedType) && IsSymbolSideEffectFree(rpo.DeconstructSymbol)) return rpo.PropertySubpatterns.Any((x) => MightNeedPriorRetrack(x, callerInfo)) || rpo.DeconstructionSubpatterns.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is IRelationalPatternOperation) return false;
			else if (op is ISlicePatternOperation spo && IsSymbolSideEffectFree(spo.SliceSymbol)) op = spo.Pattern;
			else if (op is ITypePatternOperation) return false;
			else if (op is ICoalesceAssignmentOperation cao) return MightNeedPriorRetrack(cao.Target, callerInfo) || MightNeedPriorRetrack(cao.Value, callerInfo);
			else if (op is IConditionalAccessOperation cao2) return MightNeedPriorRetrack(cao2.Operation, callerInfo) || MightNeedPriorRetrack(cao2.WhenNotNull, callerInfo);
			else if (op is ICoalesceOperation co3) return MightNeedPriorRetrack(co3.Value, callerInfo) || MightNeedPriorRetrack(co3.WhenNull, callerInfo);
#if ROSLYN_4_9_2_OR_GREATER
			else if (op is ICollectionExpressionOperation coe && IsTypeSideEffectFree(coe.Type) && IsMethodSideEffectFree(coe.ConstructMethod, out _)) return coe.Elements.Any((x) => MightNeedPriorRetrack(x, callerInfo));
#endif
			else if (op is IDefaultValueOperation) return false;
			else if (op is IDeclarationExpressionOperation deo) op = deo.Expression;
			else if (op is IDelegateCreationOperation dco) return false;
			else if (op is IDiscardOperation do1) return false;
			else if (op is IEmptyOperation) return false;
			else if (op is IEventReferenceOperation ero) op = ero.Instance;
			else if (op is IFieldInitializerOperation fio) op = fio.Value;
			else if (!callerInfo.IsForIFormatProvider && op is IImplicitIndexerReferenceOperation iiro && IsSymbolSideEffectFree(iiro.LengthSymbol) && IsSymbolSideEffectFree(iiro.IndexerSymbol)) return MightNeedPriorRetrack(iiro.Instance, callerInfo) || MightNeedPriorRetrack(iiro.Argument, callerInfo);
#if ROSLYN_4_7_0_OR_GREATER
			else if (!callerInfo.IsForIFormatProvider && op is IInlineArrayAccessOperation iaao) return MightNeedPriorRetrack(iaao.Instance, callerInfo) || MightNeedPriorRetrack(iaao.Argument, callerInfo);
#endif
			else if (!callerInfo.IsForIFormatProvider && op is IInstanceReferenceOperation) return false;
			else if (op is IInterpolatedStringAdditionOperation isao) return MightNeedPriorRetrack(isao.Left, callerInfo) || MightNeedPriorRetrack(isao.Right, callerInfo);
			else if (op is IInterpolatedStringOperation iso && IsTypeFullySideEffectFree(iso.Type)) return iso.Parts.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is ILiteralOperation) return false;
			else if (op is IInterpolatedStringAppendOperation isao2) op = isao2.AppendCall;
			else if (op is IInterpolatedStringHandlerArgumentPlaceholderOperation) return false;
			else if (op is IInterpolatedStringHandlerCreationOperation ishco) return MightNeedPriorRetrack(ishco.HandlerCreation, callerInfo) || MightNeedPriorRetrack(ishco.Content, callerInfo);
			else if (op is IInterpolatedStringTextOperation isto) op = isto.Text;
			else if (op is IInterpolationOperation io2 && IsTypeFullySideEffectFree(io2.Expression.Type)) return MightNeedPriorRetrack(io2.Expression, callerInfo) || MightNeedPriorRetrack(io2.Alignment, callerInfo) || MightNeedPriorRetrack(io2.FormatString, callerInfo);
			else if (op is IInvalidOperation) return false;
			else if (op is IIsTypeOperation ito) op = ito.ValueOperand;
			else if (op is IMethodReferenceOperation mro) op = mro.Instance;
			else if (op is INameOfOperation) return false;
			else if (op is IPropertySubpatternOperation pso) return MightNeedPriorRetrack(pso.Member, callerInfo) || MightNeedPriorRetrack(pso.Pattern, callerInfo);
			else if (op is IRangeOperation ro && IsMethodSideEffectFree(ro.Method, out _)) return MightNeedPriorRetrack(ro.LeftOperand, callerInfo) || MightNeedPriorRetrack(ro.RightOperand, callerInfo);
#if ROSLYN_4_9_2_OR_GREATER
			else if (op is ISpreadOperation so && IsMethodSideEffectFree(so.ElementConversion.MethodSymbol, out _)) op = so.Operand;
#endif
			else if (op is ISwitchExpressionArmOperation seao) return MightNeedPriorRetrack(seao.Pattern, callerInfo) || MightNeedPriorRetrack(seao.Guard, callerInfo) || MightNeedPriorRetrack(seao.Value, callerInfo);
			else if (op is ISwitchExpressionOperation seo) return MightNeedPriorRetrack(seo.Value, callerInfo) || seo.Arms.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else if (op is IThrowOperation) return false;
			else if (op is IUtf8StringOperation) return false;
			else if (op is IWithOperation wo && IsMethodSideEffectFree(wo.CloneMethod, out _)) return MightNeedPriorRetrack(wo.Operand, callerInfo) || MightNeedPriorRetrack(wo.Initializer, callerInfo);
			else if (op.Kind == OperationKind.None) return op.ChildOperations.Any((x) => MightNeedPriorRetrack(x, callerInfo));
			else return true;
		}
	}

	//note: the only relevant side effects here are those that could affect GC tracking (e.g., something that could free a GCHandle)
	private static bool IsTypeSideEffectFree(ITypeSymbol? type) => IsTypeSideEffectFree(type, out _, out _, out _);
	private static bool IsTypeFullySideEffectFree(ITypeSymbol? type) => IsTypeSideEffectFree(type, out _, out var isFormatEqualsHashCodeCompareToAlwaysSafe, out _) && isFormatEqualsHashCodeCompareToAlwaysSafe; //used when we might be calling ToString() / formatting / Equals() / GetHashCode() / CompareTo() on the type - all other members should be also checked with IsTypeSideEffectFree
	private static bool IsTypeSideEffectFree(ITypeSymbol? type, out bool areAllMethodsOnAllInstancesSafe, out bool isFormatEqualsHashCodeCompareToAlwaysSafe, out bool areAllStaticMembersSafe)
	{
		areAllMethodsOnAllInstancesSafe = true; // all members, other than .Equals(), .GetHashCode(), .CompareTo(), and formatting methods, are side-effect free on all instances of this type
		isFormatEqualsHashCodeCompareToAlwaysSafe = true;
		areAllStaticMembersSafe = true;
		if (type == null) return true;
		if (type.TypeKind is TypeKind.Pointer or TypeKind.Enum or TypeKind.Error or TypeKind.FunctionPointer) return true;
		if (type.SpecialType is SpecialType.System_Enum or SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_String or SpecialType.System_IntPtr or SpecialType.System_UIntPtr or SpecialType.System_Nullable_T or SpecialType.System_DateTime) return true;
		if (type is INamedTypeSymbol nts && nts.IsGenericType && nts.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T) return true;
		if (type.Is_System_Index() || type.Is_System_Range()) return true;
		areAllMethodsOnAllInstancesSafe = false;
		if (type.Is_System_Span_1(out _) || type.Is_System_ReadOnlySpan_1(out _)) return true;
		areAllStaticMembersSafe = false;
		if (type.TypeKind is TypeKind.Array or TypeKind.Delegate) return true;
		if (type.SpecialType is SpecialType.System_Array or SpecialType.System_TypedReference or SpecialType.System_RuntimeTypeHandle or SpecialType.System_MulticastDelegate or SpecialType.System_Delegate) return true;
		isFormatEqualsHashCodeCompareToAlwaysSafe = false;
		areAllStaticMembersSafe = true;
		if (type.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType) return true;
		if (type.Is_System_ValueTuple(out var args))
		{
			areAllMethodsOnAllInstancesSafe = true;
			isFormatEqualsHashCodeCompareToAlwaysSafe = true;
			foreach (var arg in args)
			{
				if (!IsTypeSideEffectFree(arg, out var areAllMethodsOnAllInstancesSafeArg, out var isFormatEqualsHashCodeCompareToAlwaysSafeArg, out _)) return false;
				areAllMethodsOnAllInstancesSafe &= areAllMethodsOnAllInstancesSafeArg;
				isFormatEqualsHashCodeCompareToAlwaysSafe &= isFormatEqualsHashCodeCompareToAlwaysSafeArg;
			}
			return true;
		}
		return false;
	}

	//note: the only relevant side effects here are those that could affect GC tracking (e.g., something that could free a GCHandle)
	private static bool IsPropertySideEffectFree(IPropertySymbol? property)
	{
		if (property == null) return true;
		if (IsTypeSideEffectFree(property.ContainingType, out var areAllSafe, out _, out var areAllStaticMembersSafe) && (property.IsStatic ? areAllStaticMembersSafe : areAllSafe)) return true;
		if (!property.IsStatic && (property.ContainingType.TypeKind == TypeKind.Array || property.ContainingType.SpecialType == SpecialType.System_Array)) return true;
		if (property.ContainingType.Is_System_Span_1(out _) || property.ContainingType.Is_System_ReadOnlySpan_1(out _)) return true;
		return false;
	}

	//note: the only relevant side effects here are those that could affect GC tracking (e.g., something that could free a GCHandle)
	private static bool IsMethodSideEffectFree(IMethodSymbol? method, out bool allowRefsAlways)
	{
		allowRefsAlways = false;
		if (method == null) return true;
		if (IsTypeSideEffectFree(method.ContainingType, out var areAllSafe, out var isFormatEqualsHashCodeCompareToAlwaysSafe, out var areAllStaticMembersSafe) && (method.Name is "Equals" or "GetHashCode" or "ToString" or "CompareTo" or "TryFormat" ? isFormatEqualsHashCodeCompareToAlwaysSafe : (method.IsStatic ? areAllStaticMembersSafe : areAllSafe))) return true;
		if (method.AssociatedSymbol is { } assocSymbol) return IsSymbolSideEffectFree(assocSymbol);
		if (method.ContainingType.Is_System_MemoryExtensions())
		{
			if (method.Name is "AsMemory" or "AsSpan" or "Overlaps" or "ToLower" or "ToLowerInvariant" or "ToUpper" or "ToUpperInvariant") return true;
			if (method.Parameters is [{ Type: INamedTypeSymbol { TypeArguments: [var arg] } }, ..] && IsTypeFullySideEffectFree(arg) && !method.OriginalDefinition.Parameters.Any((x) => x.Type.Is_System_Collections_Generic_IEqualityComparer_1(out _) || x.Type.Is_System_Collections_Generic_IComparer_1(out _))) return method.Name is "CommonPrefixLength" or "CompareTo" or "Contains" or "ContainsAny" or "ContainsAnyExcept" or "ContainsAnyExceptInRange" or "ContainsInRange" or "Count" or "CountAny" or "EndsWith" or "Equals" or "IndexOf" or "IndexOfAny" or "IndexOfAnyExcept" or "IndexOfAnyExceptInRange" or "IndexOfInRange" or "IsWhiteSpace" or "LastIndexOf" or "LastIndexOfAny" or "LastIndexOfAnyExcept" or "LastIndexOfAnyExceptInRange" or "LastIndexOfInRange" or "SequenceCompareTo" or "SequenceEqual" or "StartsWith" or "Trim" or "TrimEnd" or "TrimStart";
		}
		else if (method.ContainingType.Is_System_Span_1(out _) || method.ContainingType.Is_System_ReadOnlySpan_1(out _))
		{
			if (method.Name is "CastUp" or "GetPinnableReference" or "Slice" or "ToArray" or "ToString") return true;
		}
		else if (method.ContainingType.Is_System_Runtime_InteropServices_MemoryMarshal())
		{
			allowRefsAlways = true;
			if (method.Name is "AsBytes" or "AsMemory" or "AsRef" or "Cast" or "CreateFromPinnedArray" or "CreateReadOnlySpan" or "CreateReadOnlySpanFromNullTerminated" or "CreateSpan" or "GetArrayDataReference" or "GetReference" or "Read" or "ToEnumerable") return true;
			allowRefsAlways = false;
			if (method.Name is "TryGetArray" or "TryGetMemoryManager" or "TryGetString" or "TryRead") return true;
		}
		else if (method.ContainingType.Is_System_Runtime_CompilerServices_Unsafe())
		{
			allowRefsAlways = true;
			if (method.Name is "Add" or "AddByteOffset" or "AreSame" or "As" or "AsRef" or "AsPointer" or "BitCast" or "ByteOffset" or "IsAddressGreaterThan" or "IsAddressGreaterThanOrEqual" or "IsAddressLessThan" or "IsAddressLessThanOrEqual" or "IsNullRef" or "NullRef" or "Read" or "ReadUnaligned" or "SizeOf" or "SkipInit" or "Subtract" or "SubtractByteOffsets" or "Unbox") return true;
			allowRefsAlways = false;
		}
		else if (method.ContainingType.Is_System_Threading_Volatile())
		{
			allowRefsAlways = true;
			if (method.Name == "Read") return true;
			allowRefsAlways = false;
		}
		return false;
	}

	//note: the only relevant side effects here are those that could affect GC tracking (e.g., something that could free a GCHandle)
	private static bool IsSymbolSideEffectFree(ISymbol? symbol)
	{
		if (symbol is ITypeSymbol ts) return IsTypeSideEffectFree(ts);
		else if (symbol is IMethodSymbol ms) return IsMethodSideEffectFree(ms, out _);
		else if (symbol is IPropertySymbol ps) return IsPropertySideEffectFree(ps);
		else return false;
	}
}
