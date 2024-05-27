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
		//Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net80
		//Or <add key="dotnet-tools" value="https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json" />
		public static readonly ReferenceAssemblies Net80 = new(
			"net8.0",
			new PackageIdentity(
				"Microsoft.NETCore.App.Ref",
				"8.0.0"),
			Path.Combine("ref", "net8.0"));
	}
}
