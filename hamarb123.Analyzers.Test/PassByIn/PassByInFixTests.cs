using Xunit;
using VerifyCS = hamarb123.Analyzers.Test.CSharpCodeFixVerifier<
	hamarb123.Analyzers.PassByIn.PassByInAnalyzer,
	hamarb123.Analyzers.PassByIn.PassByInCodeFix>;

namespace hamarb123.Analyzers.Test.PassByIn
{
	public class PassByInFixTests
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
						int i = 0;
						M1({|HAM0008:i|});
						int* ptr = &i;
						M1({|HAM0008:*ptr|});
						M1({|HAM0008:x|});
						M1({|HAM0008:y|});
						M1({|HAM0008:z|});
						((delegate* managed<in int, void>)null)({|HAM0008:i|});
						M1(x: {|HAM0008:i|});
						M1({|HAM0008:P|});
						M1({|HAM0008:M()|});
						bool b = true;
						M1({|HAM0008:b ? i : y|});
						M1({|HAM0008:b ? ref i : ref y|});
						M1({|HAM0008:b ? i : i == y ? ref i : ref y|});
						M1({|HAM0008:b ? i : i == y ? i : y|});
					}

					public void M1(in int x)
					{
					}
				}

				public struct S
				{
					public void M()
					{
						M4({|HAM0008:this|});
						M5({|HAM0008:this|});
					}

					public void M4(in S x)
					{
					}

					public void M5<T>(in T x)
					{
					}
				}
				""";

			const string fixedSource = """
				public class C(int x)
				{
					int y = 0;

					ref int P => ref y;
					ref int M() => ref y;

					public unsafe void M(int z)
					{
						int i = 0;
						M1(in i);
						int* ptr = &i;
						M1(in *ptr);
						M1(in x);
						M1(in y);
						M1(in z);
						((delegate* managed<in int, void>)null)(in i);
						M1(x: in i);
						M1(in P);
						M1(in M());
						bool b = true;
						M1(in b ? ref i : ref y);
						M1(in b ? ref i : ref y);
						M1(in b ? ref i : ref i == y ? ref i : ref y);
						M1(in b ? ref i : ref i == y ? ref i : ref y);
					}

					public void M1(in int x)
					{
					}
				}

				public struct S
				{
					public void M()
					{
						M4(in this);
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

			await VerifyCS.VerifyCodeFixAsync(source, fixedSource);
		}
	}
}
