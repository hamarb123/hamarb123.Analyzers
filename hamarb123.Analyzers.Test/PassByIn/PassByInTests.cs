using Xunit;
using VerifyCS = hamarb123.Analyzers.Test.CSharpAnalyzerVerifier<
	hamarb123.Analyzers.PassByIn.PassByInAnalyzer>;

namespace hamarb123.Analyzers.Test.PassByIn
{
	public class PassByInTests
	{
		[Fact]
		public async Task Verify()
		{
			const string source = """
				public class C(int x)
				{
					int y = 0;

					ref int P => ref y;
					ref int M() => ref y;

					public unsafe void M(int z)
					{
						M1(1);
						int i = 0;
						M1({|#0:i|});
						M1(in i);
						M2(i); // Up to roslyn to warn here.
						M3(); // Default value is okay.
						int* ptr = &i;
						M1({|#1:*ptr|});
						M1(in *ptr);
						M1(ref *ptr); // Passing by ref also okay.
						M1({|#2:x|});
						M1({|#3:y|});
						M1({|#4:z|});
						((delegate* managed<in int, void>)null)({|#5:i|});
						((delegate* managed<in int, void>)null)(in i);
						M1(x: {|#6:i|});
						M1(x: in i);
						M1({|#7:P|});
						M1(in P);
						M1({|#8:M()|});
						M1(in M());
						bool b = true;
						M1({|#9:b ? i : y|});
						M1(in b ? ref i : ref y);
						M1({|#10:b ? ref i : ref y|});
						M1(b ? i : 0);
						M4(this);
						M5(this);
					}

					public void M1(in int x)
					{
					}

					public void M2(ref readonly int x)
					{
					}

					public void M3(in int x = 0)
					{
					}

					public void M4(in C x)
					{
					}

					public void M5<T>(in T x)
					{
					}
				}

				public struct S
				{
					public void M()
					{
						M4({|#11:this|});
						M4(in this);
						M5({|#12:this|});
						M5(in this);
					}

					public void M4(in S x)
					{
					}

					public void M5<T>(in T x)
					{
					}
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0008").WithLocation(0).WithArguments("x");
			var expected1 = VerifyCS.Diagnostic("HAM0008").WithLocation(1).WithArguments("x");
			var expected2 = VerifyCS.Diagnostic("HAM0008").WithLocation(2).WithArguments("x");
			var expected3 = VerifyCS.Diagnostic("HAM0008").WithLocation(3).WithArguments("x");
			var expected4 = VerifyCS.Diagnostic("HAM0008").WithLocation(4).WithArguments("x");
			var expected5 = VerifyCS.Diagnostic("HAM0008").WithLocation(5).WithArguments("");
			var expected6 = VerifyCS.Diagnostic("HAM0008").WithLocation(6).WithArguments("x");
			var expected7 = VerifyCS.Diagnostic("HAM0008").WithLocation(7).WithArguments("x");
			var expected8 = VerifyCS.Diagnostic("HAM0008").WithLocation(8).WithArguments("x");
			var expected9 = VerifyCS.Diagnostic("HAM0008").WithLocation(9).WithArguments("x");
			var expected10 = VerifyCS.Diagnostic("HAM0008").WithLocation(10).WithArguments("x");
			var expected11 = VerifyCS.Diagnostic("HAM0008").WithLocation(11).WithArguments("x");
			var expected12 = VerifyCS.Diagnostic("HAM0008").WithLocation(12).WithArguments("x");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12);
		}
	}
}
