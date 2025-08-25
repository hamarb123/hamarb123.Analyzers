using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace hamarb123.Analyzers
{
	public static class CustomExtensions
	{
		//gets the method implementation that will be used for a virtual base method
		public static IMethodSymbol? GetOverrideFor(this ITypeSymbol? type, IMethodSymbol method)
		{
			while (type != null)
			{
				//loop through all the members that are methods
				var members = type.GetMembers();
				foreach (var member in members)
				{
					if (member is IMethodSymbol ms)
					{
						//check if the overridden method (or any method that it overrides) is the one we're searching for
						var overridden = ms.OverriddenMethod;
						while (overridden != null)
						{
							if (SymbolEqualityComparer.Default.Equals(overridden, method)) return ms;
							overridden = overridden.OverriddenMethod;
						}
					}
				}

				//go to next base type
				type = type.BaseType;
			}
			return null;
		}

		public static bool Is_System_Index(this INamespaceOrTypeSymbol? t)
		{
			return t is INamedTypeSymbol { Name: "Index", Arity: 0, ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };
		}

		public static bool Is_System_Range(this INamespaceOrTypeSymbol? t)
		{
			return t is INamedTypeSymbol { Name: "Range", Arity: 0, ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };
		}

		public static bool Is_System_StringComparison(this INamespaceOrTypeSymbol? t)
		{
			return t is INamedTypeSymbol { Name: "StringComparison", Arity: 0, ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };
		}

		public static bool Is_System_Globalization_CultureInfo(this INamespaceOrTypeSymbol? t)
		{
			return t is INamedTypeSymbol { Name: "CultureInfo", Arity: 0, ContainingNamespace: { Name: "Globalization", ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } } };
		}

		public static bool Is_System_Nullable_1(this INamespaceOrTypeSymbol? t, [NotNullWhen(true)] out ITypeSymbol? argument)
		{
			argument = null;
			if (t is INamedTypeSymbol ts)
			{
				var result = ts is INamedTypeSymbol { Name: "Nullable", Arity: 1, ContainingNamespace: { Name: "System", ContainingNamespace.IsGlobalNamespace: true } };
				if (result) argument = ts.TypeArguments[0];
				return result;
			}
			return false;
		}

		public static bool Is_System_Runtime_CompilerServices_CompilerGeneratedAttribute(this INamespaceOrTypeSymbol? t)
		{
			return t is INamedTypeSymbol
			{
				Name: "CompilerGeneratedAttribute",
				Arity: 0,
				ContainingNamespace:
				{
					Name: "CompilerServices",
					ContainingNamespace:
					{
						Name: "Runtime",
						ContainingNamespace:
						{
							Name: "System",
							ContainingNamespace.IsGlobalNamespace: true,
						},
					},
				},
			};
		}

		//determines if the msbuild project properties specify that we are in the include list (or true if no include list)
		public static bool ShouldRun(this AnalyzerOptions options, string diagnosticCode)
		{
			if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.Hamarb123AnalyzersDiagnosticsIncludeList", out string? value))
			{
				var sp = value.AsSpan().Trim();
				if (sp.Length > 0)
				{
					do
					{
						//find next separator (or end), compare the trimmed version to our expected diagnostic code, then move to next if different
						var idx = sp.IndexOfAny(',', ';');
						if (idx < 0) idx = sp.Length;
						if (sp.Slice(0, idx).Trim().Equals(diagnosticCode.AsSpan(), StringComparison.Ordinal)) return true;
						sp = sp.Slice(Math.Min(idx + 1, sp.Length));
					}
					while (sp.Length > 0);
					return false;
				}
			}

			//no include-list specified
			return true;
		}
	}
}
