using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using VerifyVB = hamarb123.Analyzers.Test.VisualBasicAnalyzerVerifier<
	hamarb123.Analyzers.StringNonOrdinal.StringNonOrdinalAnalyzer>;
using VerifyCS = hamarb123.Analyzers.Test.CSharpAnalyzerVerifier<
	hamarb123.Analyzers.StringNonOrdinal.StringNonOrdinalAnalyzer>;

namespace hamarb123.Analyzers.Test.StringNonOrdinal
{
	public class StringNonOrdinalTests
	{
		[Fact]
		public async Task VerifyCSharp()
		{
			const string source = """
				using System;

				public class C
				{
					public void M(string str, S1 s1)
					{
						{|#0:string.Compare(str, "")|};
						{|#1:str.CompareTo("")|};
						str.Contains('\0');
						str.Contains("");
						str.EndsWith('\0');
						{|#2:str.EndsWith("")|};
						str.Equals("");
						string.Equals(str, "");
						str.IndexOf('\0');
						{|#3:str.IndexOf("")|};
						str.LastIndexOf('\0');
						{|#4:str.LastIndexOf("")|};
						{|#5:str.StartsWith("")|};
						str.StartsWith('\0');
						_ = str == "";

						s1.EndsWith("");
						{|#6:str.IndexOf("", 0)|};
						str.EndsWith("", true, null);
						str.EndsWith("", StringComparison.Ordinal);
					}
				}

				public struct S1
				{
					public bool EndsWith(string value) => false;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0004").WithLocation(0).WithArguments("Compare");
			var expected1 = VerifyCS.Diagnostic("HAM0004").WithLocation(1).WithArguments("CompareTo");
			var expected2 = VerifyCS.Diagnostic("HAM0004").WithLocation(2).WithArguments("EndsWith");
			var expected3 = VerifyCS.Diagnostic("HAM0004").WithLocation(3).WithArguments("IndexOf");
			var expected4 = VerifyCS.Diagnostic("HAM0004").WithLocation(4).WithArguments("LastIndexOf");
			var expected5 = VerifyCS.Diagnostic("HAM0004").WithLocation(5).WithArguments("StartsWith");
			var expected6 = VerifyCS.Diagnostic("HAM0004").WithLocation(6).WithArguments("IndexOf");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6);
		}

		[Fact]
		public async Task VerifyVisualBasic()
		{
			const string source = """
				Imports Microsoft.VisualBasic
				Imports System

				Public Class C
					Public Sub M(str As String, s1 As S1)
						{|#0:String.Compare(str, "")|}
						{|#1:str.CompareTo("")|}
						str.Contains(ChrW(0))
						str.Contains("")
						str.EndsWith(ChrW(0))
						{|#2:str.EndsWith("")|}
						str.Equals("")
						string.Equals(str, "")
						str.IndexOf(ChrW(0))
						{|#3:str.IndexOf("")|}
						str.LastIndexOf(ChrW(0))
						{|#4:str.LastIndexOf("")|}
						{|#5:str.StartsWith("")|}
						str.StartsWith(ChrW(0))
						Dim a = str = ""

						s1.EndsWith("")
						{|#6:str.IndexOf("", 0)|}
						str.EndsWith("", True, Nothing)
						str.EndsWith("", StringComparison.Ordinal)
					End Sub
				End Class

				Public Structure S1
					Public Function EndsWith(value As String) As Boolean
						Return False
					End Function
				End Structure
				""";

			var expected0 = VerifyVB.Diagnostic("HAM0004").WithLocation(0).WithArguments("Compare");
			var expected1 = VerifyVB.Diagnostic("HAM0004").WithLocation(1).WithArguments("CompareTo");
			var expected2 = VerifyVB.Diagnostic("HAM0004").WithLocation(2).WithArguments("EndsWith");
			var expected3 = VerifyVB.Diagnostic("HAM0004").WithLocation(3).WithArguments("IndexOf");
			var expected4 = VerifyVB.Diagnostic("HAM0004").WithLocation(4).WithArguments("LastIndexOf");
			var expected5 = VerifyVB.Diagnostic("HAM0004").WithLocation(5).WithArguments("StartsWith");
			var expected6 = VerifyVB.Diagnostic("HAM0004").WithLocation(6).WithArguments("IndexOf");

			await VerifyVB.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6);
		}
	}
}
