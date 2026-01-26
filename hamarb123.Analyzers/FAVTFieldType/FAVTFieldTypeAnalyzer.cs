using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace hamarb123.Analyzers.FAVTFieldType;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public sealed class FAVTFieldTypeAnalyzer : DiagnosticAnalyzer
{
	private const string DiagnosticId1 = "HAM0005";
	private const string Title1 = "Field will not be pinned by FixedAddressValueType";
	private const string MessageFormat1 = "FixedAddressValueType will not pin the field '{0}', which is of type '{1}'";
	private const string Description1 = "Field will not be pinned by FixedAddressValueType - only static fields that are valuetypes other than primitives or enums will be pinned.";
	private const string Category1 = "Correctness";

	private const string DiagnosticId2 = "HAM0006";
	private const string Title2 = "Field might not be pinned by FixedAddressValueType";
	private const string MessageFormat2 = "FixedAddressValueType may not pin the field '{0}', which is of type '{1}'";
	private const string Description2 = "Field might not be pinned by FixedAddressValueType - only static fields that are valuetypes other than primitives or enums will be pinned.";
	private const string Category2 = "Correctness";

	private static readonly DiagnosticDescriptor _rule1 = new(DiagnosticId1, Title1, MessageFormat1, Category1, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description1);
	private static readonly DiagnosticDescriptor _rule2 = new(DiagnosticId2, Title2, MessageFormat2, Category2, DiagnosticSeverity.Info, isEnabledByDefault: true, description: Description2);

	private static readonly ImmutableArray<DiagnosticDescriptor> _rules = [_rule1, _rule2];
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
		bool runHAM0005 = context.Options.ShouldRun("HAM0005"), runHAM0006 = context.Options.ShouldRun("HAM0006");

		//if at least 1 is enabled, register them all
		if (runHAM0005 || runHAM0006)
		{
			ConcurrentDictionary<IPropertySymbol, IFieldSymbol?> backingFieldCache = [];
			context.RegisterSymbolAction((ctx) => AnalyzeFieldSymbol(ctx, runHAM0005, runHAM0006), SymbolKind.Field);
			context.RegisterSymbolAction((ctx) => AnalyzePropertySymbol(ctx, backingFieldCache, runHAM0005, runHAM0006), SymbolKind.Property);
		}
	}

	private void AnalyzeFieldSymbol(SymbolAnalysisContext ctx, bool runHAM0005, bool runHAM0006)
	{
		if (ctx.Symbol is not IFieldSymbol fieldSymbol) return;
		var location = fieldSymbol.Locations.FirstOrDefault((x) => x.IsInSource);
		if (location is null) return;
		if (HasFixedAddressValueTypeAttribute(fieldSymbol.GetAttributes(), ctx.CancellationToken))
		{
			AnalyzeFieldDeclaration(fieldSymbol.Type, fieldSymbol.IsStatic, location, fieldSymbol.Name, ctx.CancellationToken, ctx.ReportDiagnostic, runHAM0005, runHAM0006);
		}
	}

	private void AnalyzePropertySymbol(SymbolAnalysisContext ctx, ConcurrentDictionary<IPropertySymbol, IFieldSymbol?> backingFieldCache, bool runHAM0005, bool runHAM0006)
	{
		if (ctx.Symbol is not IPropertySymbol propertySymbol) return;
		var location = propertySymbol.Locations.FirstOrDefault((x) => x.IsInSource);
		if (location is null) return;
		if (!backingFieldCache.TryGetValue(propertySymbol, out var fieldSymbol))
		{
			fieldSymbol = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>().First((x) => SymbolEqualityComparer.Default.Equals(x.AssociatedSymbol, propertySymbol));
			backingFieldCache[propertySymbol] = fieldSymbol;
		}
		if (fieldSymbol is null) return;
		if (HasFixedAddressValueTypeAttribute(fieldSymbol.GetAttributes(), ctx.CancellationToken))
		{
			AnalyzeFieldDeclaration(fieldSymbol.Type, fieldSymbol.IsStatic, location, propertySymbol.Name, ctx.CancellationToken, ctx.ReportDiagnostic, runHAM0005, runHAM0006);
		}
	}

	private static bool HasFixedAddressValueTypeAttribute(ImmutableArray<AttributeData> attributes, CancellationToken cancellationToken)
	{
		//loop through all attributes, looking for FixedAddressValueTypeAttribute
		foreach (var attribute in attributes.AsSpan())
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (attribute.AttributeClass.Is_System_Runtime_CompilerServices_FixedAddressValueTypeAttribute()) return true;
		}
		return false;
	}

	private void AnalyzeFieldDeclaration(ITypeSymbol fieldType, bool isStatic, Location diagnosticLocation, string? fieldName, CancellationToken cancellationToken, Action<Diagnostic> reportDiagnostic, bool runHAM0005, bool runHAM0006)
	{
		//if error type, skip
		if (fieldType.TypeKind == TypeKind.Error) return;

		//determine if known wrong: not static, not struct / generic type, is primitive or enum, or is reference type
		bool definitelyWrong =
			!isStatic ||
			fieldType.TypeKind is not (TypeKind.Struct or TypeKind.TypeParameter) ||
			fieldType.SpecialType is SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_IntPtr or SpecialType.System_UIntPtr ||
			fieldType.IsReferenceType ||
			(fieldType.TypeKind == TypeKind.TypeParameter && ((ITypeParameterSymbol)fieldType).ConstraintTypes.Any((x) => x.SpecialType == SpecialType.System_Enum));

		//determine if possibly wrong: either not known to be a value type, or is a generic type parameter (we can't be sure it's not a primitive or enum)
		bool potentiallyWrong =
			definitelyWrong ||
			!fieldType.IsValueType ||
			fieldType.TypeKind == TypeKind.TypeParameter;

		//if it can't be wrong, exit
		if (!potentiallyWrong) return;

		//check if we've cancelled already
		cancellationToken.ThrowIfCancellationRequested();

		//report the diagnostic
		if (!(definitelyWrong ? runHAM0005 : runHAM0006)) return; //check if we're supposed to report this or not
		reportDiagnostic(Diagnostic.Create(definitelyWrong ? _rule1 : _rule2, diagnosticLocation, fieldName ?? "", fieldType.ToDisplayString()));
	}
}
