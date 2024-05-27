using System;
using System.Collections.Generic;
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

		//gets the full metadata name of a type (using . for namespace separation)
		public static string? GetFullMetadataName(this INamespaceOrTypeSymbol t)
		{
			if (t is INamespaceSymbol t1 && t1.IsGlobalNamespace) return null;
			var containing = ((INamespaceOrTypeSymbol)t.ContainingType ?? t.ContainingNamespace)?.GetFullMetadataName();
			return containing == null ? t.MetadataName : (containing + "." + t.MetadataName);
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
