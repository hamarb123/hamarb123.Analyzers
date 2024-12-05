using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;
using System.Threading;

namespace hamarb123.Analyzers.DefensiveCopies;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DefensiveCopyAnalyzer : DiagnosticAnalyzer
{
	private const string DiagnosticId1 = "HAM0001";
	private const string Title1 = "Operation causes the compiler to create a defensive copy";
	private const string MessageFormat1 = "Call to member '{0}' will execute on a defensive copy of '{1}'";
	private const string Description1 = "Operation causes the compiler to create a defensive copy.";
	private const string Category1 = "Correctness";

	private const string DiagnosticId2 = "HAM0003";
	private const string Title2 = "Operation on readonly member causes the compiler to unnecessarily create a defensive copy";
	private const string MessageFormat2 = "Call to readonly member '{0}' will execute on an unnecessary defensive copy of '{1}'";
	private const string Description2 = "Operation on readonly member causes the compiler to unnecessarily create a defensive copy.";
	private const string Category2 = "Performance";

	private static readonly DiagnosticDescriptor _rule1 = new(DiagnosticId1, Title1, MessageFormat1, Category1, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description1);
	private static readonly DiagnosticDescriptor _rule2 = new(DiagnosticId2, Title2, MessageFormat2, Category2, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description2);

	private static readonly ImmutableArray<DiagnosticDescriptor> _rules = ImmutableArray.Create(_rule1, _rule2);
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
		//get the enabled analysers
		bool runHAM0001 = context.Options.ShouldRun("HAM0001"), runHAM0003 = context.Options.ShouldRun("HAM0003");

		//if at least 1 is enabled, register them all
		if (runHAM0001 || runHAM0003)
		{
			context.RegisterOperationAction((ctx) => AnalyzePropertyReference(ctx, runHAM0001, runHAM0003), OperationKind.PropertyReference);
			context.RegisterOperationAction((ctx) => AnalyzeEventAssignment(ctx, runHAM0001, runHAM0003), OperationKind.EventAssignment);
			context.RegisterOperationAction((ctx) => AnalyzeInvocation(ctx, runHAM0001, runHAM0003), OperationKind.Invocation);
			context.RegisterOperationAction((ctx) => AnalyzeForEach(ctx, runHAM0001, runHAM0003), OperationKind.Loop);
			context.RegisterOperationAction((ctx) => AnalyzeBinaryOperation(ctx, runHAM0001, runHAM0003), OperationKind.Binary, OperationKind.CompoundAssignment);
			context.RegisterOperationAction((ctx) => AnalyzeImplicitIndexer(ctx, runHAM0001, runHAM0003), OperationKind.ImplicitIndexerReference);
			context.RegisterOperationAction((ctx) => AnalyzeAwait(ctx, runHAM0001, runHAM0003), OperationKind.Await);
			context.RegisterSyntaxNodeAction((ctx) => AnalyzeFixedStatement(ctx, runHAM0001, runHAM0003), SyntaxKind.FixedStatement);
		}
	}

	private static void AnalyzePropertyReference(OperationAnalysisContext context, bool runHAM0001, bool runHAM0003)
	{
		//run it
		if (context.Operation is IPropertyReferenceOperation op)
		{
			//get the lhs instance
			var lhs = op.Instance;
			if (lhs != null)
			{
				//get the property symbol, and determine if we're calling the getter or the setter
				var propertySymbol = op.Property;
				var (getter, setter) = GetPropertyUsage(op, propertySymbol, context);

				//check if we're in a nameof or similar further up
				context.CancellationToken.ThrowIfCancellationRequested();
				IOperation? op2 = op;
				while (op2 != null)
				{
					if (op2 is INameOfOperation or ITypeOfOperation or ISizeOfOperation) return;
					op2 = op2.Parent;
				}
				context.CancellationToken.ThrowIfCancellationRequested();

				//for both getter and setter (if used), get the rhs symbol, then analyze
				if (getter)
				{
					var rhsSymbol = propertySymbol.GetMethod;
					if (rhsSymbol != null) AnalyzeMemberAccess(lhs, rhsSymbol, op, propertySymbol.IsIndexer ? "this[]" : propertySymbol.Name, false, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
				}
				if (setter)
				{
					var rhsSymbol = propertySymbol.SetMethod;
					if (rhsSymbol != null) AnalyzeMemberAccess(lhs, rhsSymbol, op, propertySymbol.IsIndexer ? "this[]" : propertySymbol.Name, false, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
				}
			}
		}
	}

	private static (bool Get, bool Set) GetPropertyUsage(IOperation op, IPropertySymbol propertySymbol, OperationAnalysisContext context)
	{
		var usage = op.GetValueUsageInfo(context.ContainingSymbol);
		var read = usage.IsReadFrom();
		var write = usage.IsWrittenTo();
		var reference = usage.IsReference();
		return (read, write, reference, propertySymbol.RefKind != RefKind.None) switch
		{
			(var r, var w, _, false) => (r, w),
			(var r, var w, false, true) => (r || w, false),
			(_, _, true, true) => (true, false),
		};
	}

	private static void AnalyzeEventAssignment(OperationAnalysisContext context, bool runHAM0001, bool runHAM0003)
	{
		//run it
		if (context.Operation is IEventAssignmentOperation op && op.EventReference is IEventReferenceOperation eventRef)
		{
			//get the lhs instance
			var lhs = eventRef.Instance;
			if (lhs != null)
			{
				//get the event accessor we're invoking, then analyze
				var eventSymbol = eventRef.Event;
				var rhsSymbol = op.Adds ? eventSymbol?.AddMethod : eventSymbol?.RemoveMethod;
				if (rhsSymbol != null) AnalyzeMemberAccess(lhs, rhsSymbol, eventRef, eventSymbol!.Name, false, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
			}
		}
	}

	private static void AnalyzeInvocation(OperationAnalysisContext context, bool runHAM0001, bool runHAM0003)
	{
		//run it
		if (context.Operation is IInvocationOperation op)
		{
			//get the lhs instance
			var lhs = op.Instance;
			if (lhs != null)
			{
				//get the rhs method we're calling, then analyze
				var rhsSymbol = op.TargetMethod;
				if (rhsSymbol != null) AnalyzeMemberAccess(lhs, rhsSymbol, op, null, false, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
			}
		}
	}

	private static void AnalyzeForEach(OperationAnalysisContext context, bool runHAM0001, bool runHAM0003)
	{
		//run it
		if (context.Operation is IForEachLoopOperation op)
		{
			//get the loop variable
			var lhs = op.Collection;
			if (lhs is IConversionOperation conversionOp && conversionOp.IsImplicit) lhs = conversionOp.Operand;
			if (lhs != null)
			{
				//get the GetEnumerator method we're calling, then analyze
				if (op.Syntax is CommonForEachStatementSyntax forEachStatementSyntax)
				{
					var rhsSymbol = op.SemanticModel.GetForEachStatementInfo(forEachStatementSyntax).GetEnumeratorMethod;
					if (rhsSymbol != null) AnalyzeMemberAccess(lhs, rhsSymbol, lhs, "GetEnumerator", false, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
				}
			}
		}
	}

	private static void AnalyzeBinaryOperation(OperationAnalysisContext context, bool runHAM0001, bool runHAM0003)
	{
		//get the left and right side, and exit if it's not + or +=
		IOperation? left = null, right = null;
		if (context.Operation is IBinaryOperation binaryOp)
		{
			left = binaryOp.LeftOperand;
			right = binaryOp.RightOperand;
			if (binaryOp.OperatorKind != BinaryOperatorKind.Add) return;
		}
		else if (context.Operation is ICompoundAssignmentOperation compoundAssignmentOp)
		{
			left = compoundAssignmentOp.Target;
			right = compoundAssignmentOp.Value;
			if (compoundAssignmentOp.OperatorKind != BinaryOperatorKind.Add) return;
		}

		//check if it's string + implicit object
		if (left == null || right == null) return;
		if (left.Type?.SpecialType != SpecialType.System_String) return;
		if (right.Type?.SpecialType != SpecialType.System_Object) return;
		if (right is IConversionOperation conversionOp)
		{
			if (!conversionOp.IsImplicit) return;
			if (conversionOp.Conversion.IsUserDefined) return;
			right = conversionOp.Operand;
		}
		else return;

		//lookup the ToString method, then analyze
		var rightType = right.Type;
		if (rightType == null) return;
		var objectToString = (IMethodSymbol)context.Compilation.ObjectType.GetMembers("ToString").Single((x) => x is IMethodSymbol ms && ms.Parameters.Length == 0 && ms.ReturnType.SpecialType == SpecialType.System_String);
		var rhsSymbol = rightType.GetOverrideFor(objectToString) ?? objectToString;
		AnalyzeMemberAccess(right, rhsSymbol, right, null, true, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
	}

	private static void AnalyzeImplicitIndexer(OperationAnalysisContext context, bool runHAM0001, bool runHAM0003)
	{
		//run it
		if (context.Operation is IImplicitIndexerReferenceOperation op)
		{
			//get the lhs operation
			var lhs = op.Instance;

			//if we have an InlineArray, exit early
			if (lhs.Type?.GetAttributes().Any((x) => x.AttributeClass?.SpecialType == SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute) == true) return;

			//get the rhs symbol/s
			List<IMethodSymbol> rhsSymbols = op.IndexerSymbol switch
			{
				IMethodSymbol ms => [ms],
				IPropertySymbol ps => GetPropertyUsage(op, ps, context) switch
				{
					(false, false) => [],
					(true, false) => [ps.GetMethod!],
					(false, true) => [ps.SetMethod!],
					(true, true) => [ps.GetMethod!, ps.SetMethod!],
				},
				_ => [],
			};

			//foreach rhs symbol
			foreach (var _rhsSymbol in rhsSymbols)
			{
				var rhsSymbol = _rhsSymbol;

				//check if we're using the Length symbol, since our current symbol might be readonly, but our length symbol might not be
				string rhsSymbolName = "this[]";
				if (rhsSymbol == null || rhsSymbol.IsExtensionMethod || rhsSymbol.IsReadOnly || rhsSymbol.ContainingType.IsReadOnly)
				{
					//get lhs and rhs of the range, or the the value of the index
					var rangeLeft = op.Argument switch
					{
						IRangeOperation rangeOp => rangeOp.LeftOperand,
						_ => op.Argument,
					};
					var rangeRight = op.Argument switch
					{
						IRangeOperation rangeOp => rangeOp.RightOperand,
						_ => null,
					};

					//check for implicit conversion from int -> Index
					if (rangeLeft is IConversionOperation convOp1 && convOp1.IsImplicit)
					{
						var m = convOp1.Conversion.MethodSymbol;
						if (m != null && m.ContainingType.Is_System_Index() && m.MethodKind == MethodKind.Conversion && m.Name == "op_Implicit" && m.ReturnType.Is_System_Index() && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32)
						{
							rangeLeft = convOp1.Operand;
						}
					}
					if (rangeRight is IConversionOperation convOp2 && convOp2.IsImplicit)
					{
						var m = convOp2.Conversion.MethodSymbol;
						if (m != null && m.ContainingType.Is_System_Index() && m.MethodKind == MethodKind.Conversion && m.Name == "op_Implicit" && m.ReturnType.Is_System_Index() && m.Parameters[0].Type.SpecialType == SpecialType.System_Int32)
						{
							rangeRight = convOp2.Operand;
						}
					}

					//check if either left or right is an index, or agument is a range
					if ((op.Argument is not IRangeOperation && op.Argument.Type.Is_System_Range()) || (rangeLeft?.Type?.Is_System_Index() ?? false) || (rangeRight?.Type?.Is_System_Index() ?? false))
					{
						//get the length symbol
						var rhsSymbol2 = op.LengthSymbol switch
						{
							IMethodSymbol ms => ms,
							IPropertySymbol ps => ps.GetMethod,
							_ => null,
						};

						//if it's not readonly, then we want to use it instead
						if (rhsSymbol2 != null && !(rhsSymbol2.IsExtensionMethod || rhsSymbol2.IsReadOnly || rhsSymbol2.ContainingType.IsReadOnly))
						{
							rhsSymbol = rhsSymbol2;
							rhsSymbolName = op.LengthSymbol.Name;
						}
					}
				}

				//analyze if we have a method, and as long as the instance is ref readonly
				if (rhsSymbol == null) return;
				AnalyzeMemberAccess(lhs, rhsSymbol, op, rhsSymbolName, true, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
			}
		}
	}

	private void AnalyzeAwait(OperationAnalysisContext context, bool runHAM0001, bool runHAM0003)
	{
		//run it
		if (context.Operation is IAwaitOperation op)
		{
			//get the lhs
			var lhs = op.Operation;

			//get the GetAwaiter method we're calling
			if (op.Syntax is AwaitExpressionSyntax syn)
			{
				var info = op.SemanticModel.GetAwaitExpressionInfo(syn);
				if (info.IsDynamic) return;
				var rhsSymbol = info.GetAwaiterMethod;

				//analyze if we found the GetAwaiter method
				if (rhsSymbol != null) AnalyzeMemberAccess(lhs, rhsSymbol, op, null, false, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
			}
		}
	}

	private static void AnalyzeFixedStatement(SyntaxNodeAnalysisContext context, bool runHAM0001, bool runHAM0003)
	{
		//run it
		if (context.Node is FixedStatementSyntax node)
		{
			//get the declaration operation's variables
			if (context.SemanticModel.GetOperation(node.Declaration) is not IVariableDeclarationOperation declOp) return;
			var declaredVariables = declOp.GetDeclaredVariables();

			//get the value for each declaration
			for (int i = 0; i < node.Declaration.Variables.Count; i++)
			{
				//check if we've cancelled already
				context.CancellationToken.ThrowIfCancellationRequested();

				//get the element type of this declaration
				var elementType = declaredVariables[i].Type;
				if (elementType == null) continue;
				if (elementType is IPointerTypeSymbol ptrTypeSymbol) elementType = ptrTypeSymbol.PointedAtType;
				else continue;

				//process the variable declaration
				VariableDeclaratorSyntax? variable = node.Declaration.Variables[i];
				if (variable.Initializer?.Value is ExpressionSyntax value)
				{
					//check if it's not &, in which case there's no defensive copy
					var op = context.SemanticModel.GetOperation(value);
					if (op != null)
					{
						if (op is not IAddressOfOperation)
						{
							//see if we can find the method
							var type = op.Type;
							if (type == null || type.IsReferenceType) return;
							var containingSymbol = context.ContainingSymbol?.ContainingType;
							if (containingSymbol == null) return;
							IMethodSymbol? getPinnableReferenceSymbol = null;

							//check for members directly on type
							foreach (var member in type.GetMembers("GetPinnableReference"))
							{
								if (member is IMethodSymbol ms && !ms.IsStatic && ms.Parameters.Length == 0 && ms.RefKind != RefKind.None && context.Compilation.IsSymbolAccessibleWithin(ms, containingSymbol))
								{
									getPinnableReferenceSymbol = ms;
									break;
								}
							}

							//check for members on type constraints
							if (getPinnableReferenceSymbol == null && type is ITypeParameterSymbol tps)
							{
								foreach (var type2 in tps.ConstraintTypes)
								{
									//check directly on constraint type
									IMethodSymbol? localSymbol = null;
									foreach (var member in type2.GetMembers("GetPinnableReference"))
									{
										if (member is IMethodSymbol ms && !ms.IsStatic && ms.Parameters.Length == 0 && ms.RefKind != RefKind.None && context.Compilation.IsSymbolAccessibleWithin(ms, containingSymbol))
										{
											localSymbol = ms;
											goto leave;
										}
									}

									//check via interface inheritance
									if (type2.TypeKind != TypeKind.Interface) goto leave;
									foreach (var interfaceType in type2.AllInterfaces)
									{
										foreach (var member in interfaceType.GetMembers("GetPinnableReference"))
										{
											if (member is IMethodSymbol ms && !ms.IsStatic && ms.Parameters.Length == 0 && ms.RefKind != RefKind.None && context.Compilation.IsSymbolAccessibleWithin(ms, containingSymbol))
											{
												localSymbol = ms;
												goto leave;
											}
										}
									}

									//store our symbol - if we have already found one, it's ambiguous, so set to null and break so we exit later
									leave:
									if (localSymbol != null)
									{
										//check ambiguous
										if (getPinnableReferenceSymbol != null)
										{
											getPinnableReferenceSymbol = null;
											break;
										}

										getPinnableReferenceSymbol = localSymbol;
									}
								}
							}

							//analyze if we found an instance (not interface, unless through constraint) GetPinnableReference, extension ones must always be passed correctly
							//skip analysis if type doesn't match properly
							if (getPinnableReferenceSymbol == null) return;
							if (!(elementType.SpecialType == SpecialType.System_Void || SymbolEqualityComparer.Default.Equals(getPinnableReferenceSymbol.ReturnType, elementType))) return;
							AnalyzeMemberAccess(op, getPinnableReferenceSymbol, op, "GetPinnableReference", false, context.CancellationToken, context.ReportDiagnostic, runHAM0001, runHAM0003);
						}
					}
				}
			}
		}
	}

	private static void AnalyzeMemberAccess(IOperation lhs, IMethodSymbol rhsSymbol, IOperation diagnosticLocationOperation, string? rhsSymbolName, bool forceIncludeReadOnlyMember, CancellationToken cancellationToken, Action<Diagnostic> reportDiagnostic, bool runHAM0001, bool runHAM0003)
	{
		//check if we've cancelled already
		cancellationToken.ThrowIfCancellationRequested();

		//check if it's not an applicable rhs (only warn on non-readonly instance struct method call)
		//we also support checking constrained calls here (a non-struct defined method call on a struct instance)
		//additionally, we emulate when C# emits a defensive copy with generics (emit when not constrained to either class / struct, or when method is defined on interface)
		if ((!forceIncludeReadOnlyMember && rhsSymbol.IsReadOnly) || rhsSymbol.IsStatic) return;
		var lhsType = lhs.Type!;
		if (!(lhsType.IsValueType || lhsType.TypeKind == TypeKind.TypeParameter) || lhsType.IsReferenceType || (!forceIncludeReadOnlyMember && lhsType.IsReadOnly && !(rhsSymbol.ContainingType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum))) return;

		//check if we've cancelled already
		cancellationToken.ThrowIfCancellationRequested();

		//determine whether we're inside a readonly method (affects whether this is passed by ref readonly or not),
		//whether we're in an allow mutation method (ctor / init), and whether we're in a cctor
		bool isReadonlyMember = false;
		bool isInAllowMutateThisMember = false;
		ITypeSymbol? typeContainingAnalysisCctor = null;
		var containingSymbol = lhs.SemanticModel?.GetEnclosingSymbol(lhs.Syntax.Span.Start, cancellationToken);
		if (containingSymbol is IFieldSymbol fieldSymbol)
		{
			if (fieldSymbol.IsStatic) typeContainingAnalysisCctor = fieldSymbol.ContainingType;
		}
		else if (containingSymbol is IMethodSymbol methodSymbol)
		{
			isReadonlyMember = !methodSymbol.IsExtensionMethod && (methodSymbol.IsReadOnly || (!methodSymbol.IsStatic && methodSymbol.ContainingType.IsReadOnly));
			isInAllowMutateThisMember = !methodSymbol.IsExtensionMethod && !methodSymbol.IsStatic && (methodSymbol.IsInitOnly || methodSymbol.MethodKind == MethodKind.Constructor);
			typeContainingAnalysisCctor = methodSymbol.MethodKind == MethodKind.StaticConstructor ? methodSymbol.ContainingType : null;
		}
		else if (containingSymbol is IPropertySymbol propertySymbol)
		{
			methodSymbol = propertySymbol.GetMethod!;
			isReadonlyMember = !methodSymbol.IsExtensionMethod && (methodSymbol.IsReadOnly || (!methodSymbol.IsStatic && methodSymbol.ContainingType.IsReadOnly));
		}

		//check if it's not an applicable lhs (only warn on effective ref readonly)
		if (!IsLHSRefReadonly(lhs, isReadonlyMember, isInAllowMutateThisMember, typeContainingAnalysisCctor, cancellationToken)) return;

		//check if we're in `[CompilerGenerated]`
		if (HasCompilerGeneratedAttributeOrParent(containingSymbol, cancellationToken)) return;

		//check if we've cancelled already
		cancellationToken.ThrowIfCancellationRequested();

		//determine if defensive copy is unnecessary
		bool isUnnecessary = false;
		if (rhsSymbol.IsReadOnly || lhsType.IsReadOnly) isUnnecessary = true;
		if (lhsType.IsValueType && lhsType.TypeKind != TypeKind.TypeParameter && rhsSymbol.ContainingType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum)
		{
			var methodOverride = lhsType.GetOverrideFor(rhsSymbol);
			if (methodOverride == null || methodOverride.ContainingType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum || methodOverride.IsReadOnly) isUnnecessary = true;
		}
		if
		(
			lhsType.SpecialType
			is SpecialType.System_Boolean or SpecialType.System_Char
			or SpecialType.System_SByte or SpecialType.System_Byte
			or SpecialType.System_Int16 or SpecialType.System_UInt16
			or SpecialType.System_Int32 or SpecialType.System_UInt32
			or SpecialType.System_Int64 or SpecialType.System_UInt64
			or SpecialType.System_Single or SpecialType.System_Double
			or SpecialType.System_IntPtr or SpecialType.System_UIntPtr
			or SpecialType.System_Decimal or SpecialType.System_DateTime
			|| lhsType.TypeKind == TypeKind.Enum
			|| (lhsType.Is_System_Nullable_1(out _) && ((rhsSymbol.Name is "get_HasValue" or "get_Value" or "GetValueOrDefault" && rhsSymbol.Parameters.Length == 0) || (rhsSymbol.Name == "GetValueOrDefault" && rhsSymbol.OriginalDefinition.Parameters is [IParameterSymbol { RefKind: RefKind.None, CustomModifiers: [], Type: ITypeParameterSymbol { Ordinal: 0, TypeParameterKind: TypeParameterKind.Type } } ])))
			|| (lhsType is ITypeParameterSymbol tps && tps.ConstraintTypes.Any((x) => x.SpecialType == SpecialType.System_Enum))
		) isUnnecessary = true;
		if (rhsSymbol.ContainingType.TypeKind == TypeKind.Interface)
		{
			var impl = lhsType.FindImplementationForInterfaceMember(rhsSymbol);
			if (impl is IMethodSymbol ms && ms.IsReadOnly) isUnnecessary = true;
		}

		//check if we've cancelled already
		cancellationToken.ThrowIfCancellationRequested();

		//report the diagnostic
		if (!(isUnnecessary ? runHAM0003 : runHAM0001)) return; //check if we're supposed to report this or not
		var node = diagnosticLocationOperation.Syntax;
		while (node is ParenthesizedExpressionSyntax p) node = p.Expression;
		reportDiagnostic(Diagnostic.Create(isUnnecessary ? _rule2 : _rule1, node.GetLocation(), rhsSymbolName ?? rhsSymbol.Name, GetOperationSymbolName(lhs)));
	}

	private static bool IsLHSRefReadonly(IOperation? lhs, bool isReadOnlyMember, bool isInAllowMutateThisMember, ITypeSymbol? typeContainingAnalysisCctor, CancellationToken cancellationToken)
	{
		//check if we've cancelled
		again:
		cancellationToken.ThrowIfCancellationRequested();

		//common logic for readonly "field"
		//false return means it's mutable
		static bool IsReadonlyAccessToReadonlyFieldLike(bool isStatic, IOperation? lhs, ITypeSymbol containingType, bool isInAllowMutateThisMember, ITypeSymbol? typeContainingAnalysisCctor)
		{
			//simple cases
			if (isStatic && typeContainingAnalysisCctor == null) return true;
			if (!isStatic && !isInAllowMutateThisMember) return true;

			//check if we're accessing a field directly on the type
			if (isStatic) return !SymbolEqualityComparer.Default.Equals(containingType, typeContainingAnalysisCctor);
			else return lhs != null && lhs is not IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance };
		}

		//check for ref readonly / in for method, property, local, and parameter
		if (lhs is IInvocationOperation invocationOp) return invocationOp.TargetMethod.ReturnsByRefReadonly;
		if (lhs is IPropertyReferenceOperation propertyRefOp) return propertyRefOp.Property.ReturnsByRefReadonly;
		if (lhs is ILocalReferenceOperation localRefOp) return localRefOp.Local.RefKind == RefKind.RefReadOnly;

		//check for by-ref parameters specially, and then consider implicit fields caused by primary constructor to pass on their LHS readonly-ness
		if (lhs is IParameterReferenceOperation paramRefOp)
		{
			if (paramRefOp.Parameter.RefKind is RefKind.In or RefKind.RefReadOnlyParameter) return true;
			else if (paramRefOp.Parameter.RefKind == RefKind.Ref) return false;

			bool isPrimaryConstructorParameterOnStruct = false;
			ITypeSymbol? containingType = null;
			foreach (var declParamSyntax in paramRefOp.Parameter.DeclaringSyntaxReferences)
			{
				var syntax = ((declParamSyntax.GetSyntax(cancellationToken) as ParameterSyntax)?.Parent as ParameterListSyntax)?.Parent as StructDeclarationSyntax;
				if (syntax is not null)
				{
					isPrimaryConstructorParameterOnStruct = true;
					break;
				}
			}
			if (isPrimaryConstructorParameterOnStruct && isReadOnlyMember)
			{
				//check that we're not in a field/property initializer (where no defensive copy is made)
				var op = lhs;
				if (op?.SemanticModel?.GetEnclosingSymbol(op.Syntax.Span.Start, cancellationToken) is IFieldSymbol or IPropertySymbol) return false;
				return IsReadonlyAccessToReadonlyFieldLike(false, null, containingType!, isInAllowMutateThisMember, typeContainingAnalysisCctor);
			}
			return false;
		}

		//check for simple assignment operation, if it's by-ref, then we need to check the LHS
		if (lhs is ISimpleAssignmentOperation simpleAssignOp)
		{
			if (simpleAssignOp.IsRef)
			{
				lhs = simpleAssignOp.Target;
				goto again;
			}
			return false;
		}

		//check for by-ref fields specially, and then consider fields to pass on their LHS readonly-ness
		if (lhs is IFieldReferenceOperation fieldRefOp)
		{
			if (fieldRefOp.Field.RefKind == RefKind.RefReadOnly) return true;
			else if (fieldRefOp.Field.RefKind == RefKind.Ref) return false;
			if (fieldRefOp.Field.IsReadOnly) return IsReadonlyAccessToReadonlyFieldLike(fieldRefOp.Field.IsStatic, fieldRefOp.Instance, fieldRefOp.Field.ContainingType, isInAllowMutateThisMember, typeContainingAnalysisCctor);

			var inst = fieldRefOp.Instance;
			if (inst == null) return false;
			if (inst.Type?.IsReferenceType == true) return false;
			lhs = inst;
			goto again;
		}

		//check if either operand of a ternary is applicable
		if (lhs is IConditionalOperation conditionalOp && conditionalOp.IsRef)
		{
			if (IsLHSRefReadonly(conditionalOp.WhenTrue, isReadOnlyMember, isInAllowMutateThisMember, typeContainingAnalysisCctor, cancellationToken)) return true;
			cancellationToken.ThrowIfCancellationRequested();
			return IsLHSRefReadonly(conditionalOp.WhenFalse, isReadOnlyMember, isInAllowMutateThisMember, typeContainingAnalysisCctor, cancellationToken);
		}

		//check if this is passed by ref readonly
		if (lhs is IInstanceReferenceOperation iro && iro.ReferenceKind == InstanceReferenceKind.ContainingTypeInstance) return isReadOnlyMember;

		//check for inline array access
		if (lhs is IInlineArrayAccessOperation inlineArrayAccessOp)
		{
			if (inlineArrayAccessOp.Argument.Type.Is_System_Range()) return false;
			lhs = inlineArrayAccessOp.Instance;
			goto again;
		}

		//check for implicit indexer
		if (lhs is IImplicitIndexerReferenceOperation implicitIndexerRefOp)
		{
			var indexerSymbol = implicitIndexerRefOp.IndexerSymbol;
			if (indexerSymbol is IMethodSymbol method) return method.ReturnsByRefReadonly;
			else if (indexerSymbol is IPropertySymbol prop) return prop.ReturnsByRefReadonly;
			return false;
		}

		//otherwise, not a ref readonly
		return false;
	}

	private static string GetOperationSymbolName(IOperation op)
	{
		//read name for most, or use a hard coded name for a few, or (unideally - probably indicating a bug) fallback to type name
		if (op is IInvocationOperation invocationOp) return invocationOp.TargetMethod.Name;
		if (op is IPropertyReferenceOperation propertyRefOp) return propertyRefOp.Property.IsIndexer ? "this[]" : propertyRefOp.Property.Name;
		if (op is ILocalReferenceOperation localRefOp) return localRefOp.Local.Name;
		if (op is IParameterReferenceOperation paramRefOp) return paramRefOp.Parameter.Name;
		if (op is IFieldReferenceOperation fieldRefOp) return fieldRefOp.Field.Name;
		if (op is ISimpleAssignmentOperation simpleAssignOp && simpleAssignOp.IsRef) return "assignment";
		if (op is IConditionalOperation) return "conditional";
		if (op is IInstanceReferenceOperation) return "this";
		if (op is IInlineArrayAccessOperation) return "this[]";
		if (op is IImplicitIndexerReferenceOperation) return "this[]";
		return op.GetType().ToString();
	}


	//Code to check [CompilerGenerated]:

	private static bool HasCompilerGeneratedAttribute(ISymbol symbol)
	{
		return symbol.GetAttributes().Any((x) => x.AttributeClass.Is_System_Runtime_CompilerServices_CompilerGeneratedAttribute());
	}

	private static bool HasCompilerGeneratedAttributeOrParent(ISymbol? symbol, CancellationToken cancellationToken = default)
	{
		if (symbol is null) return false;
		cancellationToken.ThrowIfCancellationRequested();

		if (HasCompilerGeneratedAttribute(symbol)) return true;

		if (symbol is IMethodSymbol s1)
		{
			var assocSymbol = s1.AssociatedSymbol;
			return (assocSymbol != null && HasCompilerGeneratedAttribute(assocSymbol)) || HasCompilerGeneratedAttributeOrParent(s1.ContainingSymbol);
		}
		else if (symbol is ITypeSymbol s2)
		{
			return HasCompilerGeneratedAttributeOrParent(s2.ContainingType ?? s2.ContainingModule ?? (ISymbol)s2.ContainingAssembly);
		}
		else if (symbol is IPropertySymbol s3)
		{
			return HasCompilerGeneratedAttributeOrParent(s3.ContainingType);
		}
		else if (symbol is IEventSymbol s4)
		{
			return HasCompilerGeneratedAttributeOrParent(s4.ContainingType);
		}
		else if (symbol is IFieldSymbol s5)
		{
			var assocSymbol = s5.AssociatedSymbol;
			return (assocSymbol != null && HasCompilerGeneratedAttribute(assocSymbol)) || HasCompilerGeneratedAttributeOrParent(s5.ContainingType);
		}
		else if (symbol is IModuleSymbol s6)
		{
			return HasCompilerGeneratedAttributeOrParent(s6.ContainingAssembly);
		}
		else
		{
			return false;
		}
	}
}
