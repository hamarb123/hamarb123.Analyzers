using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using VerifyVB = hamarb123.Analyzers.Test.VisualBasicAnalyzerVerifier<
	hamarb123.Analyzers.NullableConditions.NullableConditionAnalyzer>;

namespace hamarb123.Analyzers.Test.NullableConditions
{
	public class NullableConditionTests
	{
		[Fact]
		public async Task VerifyIfStatement()
		{
			const string source = """
				Imports System

				Public Class C
					Public Shared Operator IsTrue(x As C) As Boolean
						Return True
					End Operator

					Public Shared Operator IsFalse(x As C) As Boolean
						Return False
					End Operator

					Public Function NullableBool() As Boolean?
						Return Nothing
					End Function

					Public Sub M1()
						'No nullable if statements
						If True Then
						End If

						If True Then
						ElseIf True Then
						End If

						Dim i = If(True, 0, 1)

						If New C() Then
						End If

						If True Then
						ElseIf New C() Then
						End If

						i = If(New C(), 0, 1)

						If New D1() Then
						End If

						If New D2() Then
						End If

						If CType(True, Boolean?).GetValueOrDefault() Then
						End If

						If CType(New E1(), Boolean?).GetValueOrDefault() Then
						End If
					End Sub

					Public Sub M2()
						'All nullable if statements
						If {|#0:CType(True, Boolean?)|} Then
						End If

						If True Then
						ElseIf {|#1:CType(True, Boolean?)|} Then
						End If

						Dim i = If({|#2:CType(True, Boolean?)|}, 0, 1)

						Dim str = 1.ToString()

						If {|#3:str?.GetHashCode() = 0|} Then
						End If

						If True Then
						Else If {|#4:str?.GetHashCode() = 0|} Then
						End If

						i = If({|#5:str?.GetHashCode() = 0|}, 0, 1)

						If {|#6:CType(True, Boolean?) = True|} Then
						End If

						If True Then
						Else If {|#7:CType(True, Boolean?) = True|} Then
						End If

						i = If({|#8:CType(True, Boolean?) = True|}, 0, 1)

						If {|#9:Nothing|} Then
						End If

						If {|#10:"1"|} Then
						End If

						If {|#11:New Object()|} Then
						End If

						If {|#12:1|} Then
						End If

						If {|#13:(1)|} Then
						End If

						Dim c As New C()

						If {|#14:c.NullableBool()|} Then
						End If

						If {|#15:New E1()|} Then
						End If

						If {|#16:New E2()|} Then
						End If
					End Sub
				End Class

				Public Class D1
					Public Shared Widening Operator CType(v as D1) As Boolean
						Return True
					End Operator
				End Class

				Public Class D2
					Public Shared Narrowing Operator CType(v as D2) As Boolean
						Return True
					End Operator
				End Class

				Public Class E1
					Public Shared Widening Operator CType(v as E1) As Boolean?
						Return True
					End Operator
				End Class

				Public Class E2
					Public Shared Narrowing Operator CType(v as E2) As Boolean?
						Return True
					End Operator
				End Class
				""";

			var expected0 = VerifyVB.Diagnostic("HAM0002").WithLocation(0).WithArguments("CType(True, Boolean?)");
			var expected1 = VerifyVB.Diagnostic("HAM0002").WithLocation(1).WithArguments("CType(True, Boolean?)");
			var expected2 = VerifyVB.Diagnostic("HAM0002").WithLocation(2).WithArguments("CType(True, Boolean?)");
			var expected3 = VerifyVB.Diagnostic("HAM0002").WithLocation(3).WithArguments("str?.GetHashCode() = 0");
			var expected4 = VerifyVB.Diagnostic("HAM0002").WithLocation(4).WithArguments("str?.GetHashCode() = 0");
			var expected5 = VerifyVB.Diagnostic("HAM0002").WithLocation(5).WithArguments("str?.GetHashCode() = 0");
			var expected6 = VerifyVB.Diagnostic("HAM0002").WithLocation(6).WithArguments("CType(True, Boolean?) = True");
			var expected7 = VerifyVB.Diagnostic("HAM0002").WithLocation(7).WithArguments("CType(True, Boolean?) = True");
			var expected8 = VerifyVB.Diagnostic("HAM0002").WithLocation(8).WithArguments("CType(True, Boolean?) = True");
			var expected9 = VerifyVB.Diagnostic("HAM0002").WithLocation(9).WithArguments("Nothing");
			var expected10 = VerifyVB.Diagnostic("HAM0002").WithLocation(10).WithArguments("\"1\"");
			var expected11 = VerifyVB.Diagnostic("HAM0002").WithLocation(11).WithArguments("New Object()");
			var expected12 = VerifyVB.Diagnostic("HAM0002").WithLocation(12).WithArguments("1");
			var expected13 = VerifyVB.Diagnostic("HAM0002").WithLocation(13).WithArguments("(1)");
			var expected14 = VerifyVB.Diagnostic("HAM0002").WithLocation(14).WithArguments("c.NullableBool()");
			var expected15 = VerifyVB.Diagnostic("HAM0002").WithLocation(15).WithArguments("New E1()");
			var expected16 = VerifyVB.Diagnostic("HAM0002").WithLocation(16).WithArguments("New E2()");

			await VerifyVB.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16);
		}
	}
}
