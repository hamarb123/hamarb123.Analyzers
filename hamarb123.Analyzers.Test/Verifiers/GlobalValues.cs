using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;

namespace hamarb123.Analyzers.Test
{
	public static class GlobalValues
	{
		//todo: use actual one when it's released in an update
		//Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net90
		//Or <add key="dotnet-tools" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" />
		public static readonly ReferenceAssemblies Net90 = new(
			"net9.0",
			new PackageIdentity(
				"Microsoft.NETCore.App.Ref",
				"9.0.0"),
			Path.Combine("ref", "net9.0"));
	}
}
