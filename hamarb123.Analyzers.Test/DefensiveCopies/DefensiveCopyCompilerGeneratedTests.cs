using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = hamarb123.Analyzers.Test.CSharpAnalyzerVerifier<
	hamarb123.Analyzers.DefensiveCopies.DefensiveCopyAnalyzer>;

namespace hamarb123.Analyzers.Test.DefensiveCopies
{
	public class DefensiveCopyCompilerGeneratedTests
	{
		[Fact]
		public async Task VerifyCompilerGenerated()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					//The ref we will be acting on
					public static ref readonly S1 X() => ref Unsafe.NullRef<S1>();

					//All defensive copies
					[CompilerGenerated] public C() => X().M1();
					[CompilerGenerated] public C(int a) { X().M1(); }
					[CompilerGenerated] public void M() => X().M1();
					[CompilerGenerated] public void M(int a) { X().M1(); }
					[CompilerGenerated] public int P1 => X().M2();
					public int P2
					{
						[CompilerGenerated] get => X().M2();
						[CompilerGenerated] set => X().M1();
					}
					[CompilerGenerated] public int P3
					{
						get { return X().M2(); }
						set { X().M1(); }
					}
					public event Action E1
					{
						[CompilerGenerated] add => X().M1();
						[CompilerGenerated] remove => X().M1();
					}
					[CompilerGenerated] public event Action E2
					{
						add { X().M1(); }
						remove { X().M1(); }
					}
					[CompilerGenerated] public int F1 = X().M2();
					[CompilerGenerated] public static int F2 = X().M2();
					[CompilerGenerated] public static int operator +(C a) => X().M2();
					[CompilerGenerated] public static int operator +(C a, C b) => X().M2();
					[CompilerGenerated] public static implicit operator int(C a) => X().M2();
					[CompilerGenerated] public static explicit operator long(C a) => X().M2();
					[CompilerGenerated] public int this[int a] => X().M2();
					public int this[int a, int b]
					{
						[CompilerGenerated] get => X().M2();
						[CompilerGenerated] set => X().M1();
					}
					[CompilerGenerated] public int this[int a, int b, int c]
					{
						get { return X().M2(); }
						set { X().M1(); }
					}
					[CompilerGenerated] static C() { X().M1(); }
					[CompilerGenerated] ~C() { X().M1(); }
					[CompilerGenerated] class D
					{
						static D() => X().M1();
						~D() => X().M1();
					}
					delegate void DelType1(in S1 val);
					delegate void DelType2(S2 val);
					[CompilerGenerated] public void LocalMethodTest()
					{
						DelType1 a1 = (in S1 x) => x.M1();
						DelType1 b1 = (in S1 x) =>
						{
							x.M1();
						};
						DelType1 d1 = delegate (in S1 x)
						{
							x.M1();
						};
						DelType2 a2 = x => x.X.M1();
						DelType2 b2 = x =>
						{
							x.X.M1();
						};
						DelType2 c2 = delegate (S2 x)
						{
							x.X.M1();
						};
						void M1(in S1 x) { x.M1(); }
						void M2(in S1 x) => x.M1();
					}
					public void LocalMethodTest2()
					{
						DelType1 a1 = [CompilerGenerated] (in S1 x) => x.M1();
						DelType1 b1 = [CompilerGenerated] (in S1 x) =>
						{
							x.M1();
						};
						DelType2 a2 = [CompilerGenerated] (x) => x.X.M1();
						DelType2 b2 = [CompilerGenerated] (x) =>
						{
							x.X.M1();
						};
						[CompilerGenerated] void M1(in S1 x) { x.M1(); }
						[CompilerGenerated] void M2(in S1 x) => x.M1();
					}
					[CompilerGenerated] public int P4 { get; set; } = X().M2();
					[CompilerGenerated] public int F3 = X().M2(), F4 = X().M2(), F5;
				}

				[CompilerGenerated]
				public class D
				{
					private static ref readonly S1 s1 => throw null;
					public int a = s1.M2();
					public void X(in S1 x) => x.M1();
					public class D2
					{
						public void X(in S1 x) => x.M1();
					}
				}

				public class E
				{
					[CompilerGenerated]
					public class E2
					{
						public void X(in S1 x) => x.M1();
					}
				}

				[CompilerGenerated]
				struct F
				{
					public void X(in S1 x) => x.M1();
				}

				[CompilerGenerated]
				interface G
				{
					public static void X(in S1 x) => x.M1();
				}

				[CompilerGenerated]
				record H()
				{
					public void X(in S1 x) => x.M1();
				}

				[CompilerGenerated]
				record struct I()
				{
					public void X(in S1 x) => x.M1();
				}

				[CompilerGenerated]
				record class J()
				{
					public void X(in S1 x) => x.M1();
				}

				public struct S1
				{
					public void M1() { }
					public int M2() => 0;
				}

				public ref struct S2
				{
					public ref readonly S1 X => throw null;
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyAssembly()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				[assembly: CompilerGenerated]

				public class C
				{
					public void M1(in S1 value)
					{
						value.M1();
					}
				}

				public struct S1
				{
					public void M1() { }
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyModule()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				[module: CompilerGenerated]

				public class C
				{
					public void M1(in S1 value)
					{
						value.M1();
					}
				}

				public struct S1
				{
					public void M1() { }
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyIgnored()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					[CompilerGenerated] delegate void DelType(in S1 value);

					[return: CompilerGenerated]
					public void M1<[CompilerGenerated] T>([CompilerGenerated] in S1 value1, in T value2)
					{
						{|#0:value1.M1()|};
						{|#1:value2.ToString()|};
						DelType d = (in S1 value) => {|#2:value.M1()|};
					}
				}

				public struct S1
				{
					public void M1() { }
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "value1");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("ToString", "value2");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "value");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2);
		}
	}
}
