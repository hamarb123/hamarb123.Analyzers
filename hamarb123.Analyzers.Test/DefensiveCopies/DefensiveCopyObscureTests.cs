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
	public class DefensiveCopyObscureTests
	{
		[Fact]
		public async Task VerifyGenericParameters()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					//All defensive copies
					public static void X1<T>(in T value) where T : IDisposable
					{
						{|#0:value.Dispose()|};
					}
					public static void X2<T>(in T value)
					{
						{|#1:value.ToString()|};
					}
					public static void X3<T>(in T value) where T : struct, IDisposable
					{
						{|#2:value.Dispose()|};
					}
					public static void X4<T>(in T value) where T : struct
					{
						{|#3:value.ToString()|};
					}
					public static void X5<T>(in T value) where T : unmanaged
					{
						{|#4:value.ToString()|};
					}
					public static void X6<T>(in T value) where T : IDisposable, allows ref struct
					{
						{|#5:value.Dispose()|};
					}
					public static void X7<T>(in T value) where T : struct, IDisposable, allows ref struct
					{
						{|#6:value.Dispose()|};
					}
					public static void X8<T>(in T value) where T : unmanaged, IDisposable, allows ref struct
					{
						{|#7:value.Dispose()|};
					}

					//No defensive copies
					public static void X9<T>(in T value) where T : class, IDisposable
					{
						value.Dispose();
					}
					public static void X10<T>(in T value) where T : Disposable
					{
						value.Dispose();
						value.X = 1;
					}
					public static void X11<T>(in T value) where T : class
					{
						value.ToString();
					}
				}

				public class Disposable
				{
					public virtual void Dispose() { }
					public int X;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("Dispose", "value");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("ToString", "value");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("Dispose", "value");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("ToString", "value");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("ToString", "value");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("Dispose", "value");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("Dispose", "value");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("Dispose", "value");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7);
		}

		[Fact]
		public async Task VerifyExtensionMethods()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in S1 value1)
					{
						//No defensive copies
						string result;

						//result = value1.X1();
						result = value1.X2();
					}
				}

				public struct S1
				{
				}

				public static class Ext
				{
					//public static string X1<T>(this in T value) where T : struct => "";
					public static string X2(this in S1 value) => "";
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyTernary()
		{
			const string source = """
				using System;

				public class C
				{
					public void M1(bool b, ref S1 value1, ref S1 value2, in S1 value3, in S1 value4)
					{
						//No defensive copies
						Console.WriteLine((b ? ref value1 : ref value2).M1());
						Console.WriteLine((b ? value1 : value2).M1());
						Console.WriteLine((b ? value3 : value2).M1());
						Console.WriteLine((b ? value1 : value4).M1());
						Console.WriteLine((b ? value3 : value4).M1());
					}

					public void M2(bool b, ref S1 value1, ref S1 value2, in S1 value3, in S1 value4)
					{
						//All defensive copies
						Console.WriteLine({|#0:(b ? ref value3 : ref value2).M1()|});
						Console.WriteLine({|#1:(b ? ref value1 : ref value4).M1()|});
						Console.WriteLine({|#2:(b ? ref value3 : ref value4).M1()|});
					}
				}

				public struct S1
				{
					public int M1() => 0;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "conditional");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "conditional");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "conditional");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2);
		}

		[Fact]
		public async Task VerifyRefAssign()
		{
			const string source = """
				using System;

				public class C
				{
					public void M1(in S1 value1, ref S1 value2, in S1 c, ref readonly S1 d, ref S1 e, out S1 f)
					{
						//No defensive copies
						ref readonly S1 a = ref value1;
						ref S1 b = ref value2;
						f = default;
						scoped RS1 g = default;

						(a = ref value1).M2();
						(a = ref value2).M2();
						(b = ref value2).M1();
						(b = ref value2).M2();
						(b = ref value2).M2();
						(b = ref value2).M2();
						(c = ref value1).M2();
						(c = ref value2).M2();
						(d = ref value1).M2();
						(d = ref value2).M2();
						(e = ref value2).M1();
						(e = ref value2).M2();
						(e = ref value2).M2();
						(e = ref value2).M2();
						(f = ref value2).M1();
						(f = ref value2).M2();
						(f = ref value2).M2();
						(f = ref value2).M2();
						(g.F1 = ref value2).M1();
						(g.F1 = ref value2).M2();
						(g.F1 = ref value2).M2();
						(g.F1 = ref value2).M2();
						(g.F2 = ref value1).M2();
						(g.F2 = ref value2).M2();
					}

					public void M2(in S1 value1, ref S1 value2, in S1 c, ref readonly S1 d)
					{
						//All defensive copies
						ref readonly S1 a = ref value1;
						scoped RS1 g = default;

						{|#0:(a = ref value1).M1()|};
						{|#1:(a = ref value2).M1()|};
						{|#2:(c = ref value1).M1()|};
						{|#3:(c = ref value2).M1()|};
						{|#4:(d = ref value1).M1()|};
						{|#5:(d = ref value2).M1()|};
						{|#6:(g.F2 = ref value1).M1()|};
						{|#7:(g.F2 = ref value2).M1()|};
					}
				}

				public struct S1
				{
					public void M1() { }
					public readonly void M2() { }
				}

				public ref struct RS1
				{
					public ref S1 F1;
					public ref readonly S1 F2;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "assignment");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "assignment");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "assignment");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("M1", "assignment");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M1", "assignment");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("M1", "assignment");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M1", "assignment");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M1", "assignment");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7);
		}

		[Fact]
		public async Task VerifyBaseMethodCall()
		{
			const string source = """
				using System;

				public struct S1
				{
					public readonly string ToString1()
					{
						return {|#0:ToString()|};
					}

					public readonly string ToString2()
					{
						return base.ToString();
					}

					public readonly string ToString3()
					{
						return {|#1:this.ToString()|};
					}
				}

				public struct S2
				{
					public readonly override string ToString()
					{
						return base.ToString();
					}

					public readonly string ToString1()
					{
						return ToString();
					}

					public readonly string ToString2()
					{
						return this.ToString();
					}
				}

				public struct S3
				{
					public override string ToString()
					{
						return base.ToString();
					}

					public readonly string ToString1()
					{
						return {|#2:ToString()|};
					}

					public readonly string ToString2()
					{
						return {|#3:this.ToString()|};
					}
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0003").WithLocation(0).WithArguments("ToString", "this");
			var expected1 = VerifyCS.Diagnostic("HAM0003").WithLocation(1).WithArguments("ToString", "this");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("ToString", "this");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("ToString", "this");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3);
		}

		[Fact]
		public async Task VerifyStringInterpolation()
		{
			//always derefs for the function call
			const string source = """
				using System;

				public class C
				{
					public string M1(ref S1 a, ref S2 b, ref S3 c)
					{
						return $"{a}{b}{c}";
					}

					public string M2(in S1 a, in S2 b, in S3 c)
					{
						return $"{a}{b}{c}";
					}
				}

				public struct S1
				{
				}

				public struct S2
				{
					public override string ToString() => "";
				}

				public struct S3
				{
					public readonly override string ToString() => "";
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyFalsePositiveLHS()
		{
			const string source = """
				using System;

				public class C
				{
					public void M1(S1[] arr, TypedReference typedref, out S1 outParam, in S1 value)
					{
						outParam = default;

						//No defensive copies
						arr[0].X();
						__refvalue(typedref, S1).X();
						outParam.X();
						new S1().X();
						((S1)value).X();
					}
				}

				public struct S1
				{
					public void X() { }
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifySpan()
		{
			const string source = """
				using System;

				public class C
				{
					public void M1
					(
						ReadOnlySpan<S1> rospan, in ReadOnlySpan<S1> rospanIn, ref ReadOnlySpan<S1> rospanRef,
						Span<S1> span, in Span<S1> spanIn, ref Span<S1> spanRef
					)
					{
						//No defensive copies
						rospan[0].M2();
						rospanIn[0].M2();
						rospanRef[0].M2();

						span[0].M1();
						spanIn[0].M1();
						spanRef[0].M1();

						span[0].M2();
						spanIn[0].M2();
						spanRef[0].M2();

						foreach (var x in rospan) { }
						foreach (var x in rospanIn) { }
						foreach (var x in rospanRef) { }
						foreach (var x in span) { }
						foreach (var x in spanIn) { }
						foreach (var x in spanRef) { }
					}

					public void M2(ReadOnlySpan<S1> rospan, in ReadOnlySpan<S1> rospanIn, ref ReadOnlySpan<S1> rospanRef)
					{
						//All defensive copies
						{|#0:rospan[0].M1()|};
						{|#1:rospanIn[0].M1()|};
						{|#2:rospanRef[0].M1()|};
					}
				}

				public struct S1
				{
					public void M1() { }
					public readonly void M2() { }
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "this[]");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "this[]");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "this[]");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2);
		}

		[Fact]
		public async Task VerifyInlineArray()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in Buffer buf1, ref Buffer buf2)
					{
						//No defensive copies
						buf1[0].M2();
						buf1[^1].M2();
						buf1[2..4][^1].M2();

						buf2[0].M1();
						buf2[^1].M1();
						buf2[2..4][^1].M1();

						buf2[0].M2();
						buf2[^1].M2();
						buf2[2..4][^1].M2();

						foreach (var x in buf1) { }
						foreach (var x in buf2) { }
					}

					public void M2(in Buffer buf1)
					{
						//All defensive copies
						{|#0:buf1[0].M1()|};
						{|#1:buf1[^1].M1()|};
						{|#2:buf1[2..4][^1].M1()|};
					}
				}

				public struct S1
				{
					public void M1() { }
					public readonly void M2() { }
				}

				[InlineArray(10)]
				public struct Buffer
				{
					private S1 _element0;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "this[]");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "this[]");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "this[]");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2);
		}

		[Fact]
		public async Task VerifyChecked()
		{
			const string source = """
				using System;

				public class C
				{
					public void M1(in S1 value1, ref S1 value2)
					{
						//No defensive copies
						unchecked(value1).M2();
						checked(value1).M2();

						unchecked(value1).M2();
						checked(value2).M1();

						unchecked(value2).M2();
						checked(value2).M2();
					}

					public void M2(in S1 value1)
					{
						//All defensive copies
						{|#0:unchecked(value1).M1()|};
						{|#1:checked(value1).M1()|};
					}
				}

				public struct S1
				{
					public void M1() { }
					public readonly void M2() { }
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "value1");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "value1");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1);
		}

		[Fact]
		public async Task VerifyFixedField()
		{
			const string source = """
				using System;

				public unsafe class C
				{
					public void M1(ref S1 value2)
					{
						//No defensive copies
						value2.X[0].ToString();
						(value2.X[0]).ToString();

						S1 value3 = default;
						(value3.X)[0].ToString();
					}

					public void M2(in S1 value1)
					{
						//All defensive copies (except that compiler doesn't emit them as such)
						value1.X[0].ToString();
						(value1.X[0]).ToString();

						int a = 0;
						(a == 0 ? ref a : ref value1.X[0]).ToString();
					}
				}

				public unsafe struct S1
				{
					public fixed int X[1];
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source, new VerifyCS.Options() { ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Default });
		}

		[Fact]
		public async Task VerifyEnums()
		{
			//https://github.com/dotnet/roslyn/issues/72016
			const string source = """
				using System;

				public class C
				{
					public void M1(ref E1 value1, E1 value2)
					{
						//No defensive copies
						value1.ToString();
						value2.ToString();
					}

					public void M2<T1, T2>(in E1 value1, in T1 value2, in T2 value3) where T1 : Enum where T2 : struct, Enum
					{
						//All defensive copies (unnecessary)
						{|#0:value1.ToString()|};
						{|#1:value2.ToString()|};
						{|#2:value3.ToString()|};
					}
				}

				public enum E1
				{
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0003").WithLocation(0).WithArguments("ToString", "value1");
			var expected1 = VerifyCS.Diagnostic("HAM0003").WithLocation(1).WithArguments("ToString", "value2");
			var expected2 = VerifyCS.Diagnostic("HAM0003").WithLocation(2).WithArguments("ToString", "value3");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2);
		}

		[Fact]
		public async Task VerifyNameof()
		{
			const string source = """
				using System;
				using System.Collections;
				using System.Collections.Generic;

				public class C
				{
					public unsafe void M1(in S1 value1, in RS1 value2)
					{
						//No defensive copies
						_ = nameof(S1);
						_ = nameof(value1);
						_ = nameof(value1.M1);
						_ = nameof(value1.P1);
						_ = nameof(value1.F1);
						_ = nameof(value1.E1);
						_ = nameof(value1.P2);
						_ = nameof(value1.P2.P1);
						fixed (char* ptr = nameof(value1.P2)) { }
						foreach (var c in nameof(value1.P2)) { }

						_ = nameof(RS1);
						_ = nameof(value2);
						_ = nameof(value2.M1);
						_ = nameof(value2.P1);
						_ = nameof(value2.F1);
						_ = nameof(value2.E1);
						_ = nameof(value2.P2);
						_ = nameof(value2.P2.P1);
						fixed (char* ptr = nameof(value2.P2)) { }
						foreach (var c in nameof(value2.P2)) { }
					}
				}

				public struct S1 : IEnumerable<S1>
				{
					public void M1() { }
					public int P1 => 0;
					public int F1;
					public event Action E1 { add { } remove { } }
					public ref readonly S1 P2 => throw null!;
					public ref readonly S1 GetPinnableReference() => throw null!;
					public IEnumerator<S1> GetEnumerator() => throw null!;
					IEnumerator IEnumerable.GetEnumerator() => throw null!;
				}

				public ref struct RS1 : IEnumerable<RS1>
				{
					public void M1() { }
					public int P1 => 0;
					public int F1;
					public event Action E1 { add { } remove { } }
					public ref readonly RS1 P2 => throw null!;
					public ref readonly RS1 GetPinnableReference() => throw null!;
					public IEnumerator<RS1> GetEnumerator() => throw null!;
					IEnumerator IEnumerable.GetEnumerator() => throw null!;
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyRefReturnUses()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					public unsafe void M1(in S1 value)
					{
						//No defensive copies
						ref int a1 = ref Unsafe.NullRef<int>();
						ref readonly int b1 = ref Unsafe.NullRef<int>();
						ref S1 a2 = ref Unsafe.NullRef<S1>();
						ref readonly S1 b2 = ref Unsafe.NullRef<S1>();

						_ = value.ROR1;
						value.ROR1 = 1;
						value.ROR1++;
						a1 = value.ROR1;
						a1 = ref value.ROR1;

						_ = value.RORRO1;
						a1 = value.RORRO1;
						b1 = ref value.RORRO1;

						_ = value.ROR2;
						value.ROR2 = new();
						value.ROR2.M1();
						value.ROR2.M2();
						a2 = value.ROR2;
						a2 = ref value.ROR2;

						_ = value.RORRO2;
						value.RORRO2.M2();
						a2 = value.RORRO2;
						b2 = ref value.RORRO2;
					}

					public unsafe void M2(in S1 value)
					{
						//All defensive copies
						ref int a1 = ref Unsafe.NullRef<int>();
						ref readonly int b1 = ref Unsafe.NullRef<int>();
						ref S1 a2 = ref Unsafe.NullRef<S1>();
						ref readonly S1 b2 = ref Unsafe.NullRef<S1>();

						_ = {|#0:value.R1|};
						{|#1:value.R1|} = 1;
						{|#2:value.R1|}++;
						a1 = {|#3:value.R1|};
						a1 = ref {|#4:value.R1|};

						_ = {|#5:value.RRO1|};
						a1 = {|#6:value.RRO1|};
						b1 = ref {|#7:value.RRO1|};

						_ = {|#8:value.R2|};
						{|#9:value.R2|} = new();
						{|#10:value.R2|}.M1();
						{|#11:value.R2|}.M2();
						a2 = {|#12:value.R2|};
						a2 = ref {|#13:value.R2|};

						_ = {|#14:value.RRO2|};
						{|#15:{|#16:value.RRO2|}.M1()|};
						{|#17:value.RRO2|}.M2();
						a2 = {|#18:value.RRO2|};
						b2 = ref {|#19:value.RRO2|};

						{|#20:value.RORRO2.M1()|};
					}
				}

				public struct S1
				{
					public void M1() { }
					public readonly void M2() { }

					public ref int R1 => throw null!;
					public readonly ref int ROR1 => throw null!;
					public ref readonly int RRO1 => throw null!;
					public readonly ref readonly int RORRO1 => throw null!;

					public ref S1 R2 => throw null!;
					public readonly ref S1 ROR2 => throw null!;
					public ref readonly S1 RRO2 => throw null!;
					public readonly ref readonly S1 RORRO2 => throw null!;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("R1", "value");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("R1", "value");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("R1", "value");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("R1", "value");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("R1", "value");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("RRO1", "value");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("RRO1", "value");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("RRO1", "value");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("R2", "value");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("R2", "value");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("R2", "value");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("R2", "value");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("R2", "value");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("R2", "value");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("RRO2", "value");
			var expected15 = VerifyCS.Diagnostic("HAM0001").WithLocation(15).WithArguments("M1", "RRO2");
			var expected16 = VerifyCS.Diagnostic("HAM0001").WithLocation(16).WithArguments("RRO2", "value");
			var expected17 = VerifyCS.Diagnostic("HAM0001").WithLocation(17).WithArguments("RRO2", "value");
			var expected18 = VerifyCS.Diagnostic("HAM0001").WithLocation(18).WithArguments("RRO2", "value");
			var expected19 = VerifyCS.Diagnostic("HAM0001").WithLocation(19).WithArguments("RRO2", "value");
			var expected20 = VerifyCS.Diagnostic("HAM0001").WithLocation(20).WithArguments("M1", "RORRO2");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16, expected17, expected18, expected19,
				expected20);
		}

		[Fact]
		public async Task VerifyMethodGroupDelegateConversion()
		{
			const string source = """
				using System;

				public class C
				{
					public unsafe void M1(in S1 value1, ref S1 value2, S1 value3)
					{
						//No defensive copies
						Consume(value1.M1);
						Consume(value1.M2);
						Consume(value2.M1);
						Consume(value2.M2);
						Consume(value3.M1);
						Consume(value3.M2);
					}

					private void Consume<T>(T value) where T : Delegate { }
				}

				public struct S1
				{
					public void M1() { }
					public readonly void M2() { }
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyAsyncGetAwaiter()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;
				using System.Threading.Tasks;

				public class C
				{
					public readonly S1 s1RO;
					public S1 s1;
					public readonly S2 s2RO;
					public S2 s2;
					public async Task M1RO() => {|#0:await s1RO|};
					public async Task M1() => await s1;
					public async Task M2RO() => await s2RO;
					public async Task M2() => await s2;
				}

				public struct S1
				{
					public TaskAwaiter GetAwaiter() => default;
				}

				public readonly struct S2
				{
					public TaskAwaiter GetAwaiter() => default;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("GetAwaiter", "s1RO");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0);
		}

		[Fact]
		public async Task VerifyInterpolatedStringParameterInReadOnlyStruct()
		{
			const string source = """
				using System;
				using System.Diagnostics;

				public readonly struct S1
				{
					public readonly int f1;
					public void X()
					{
						Debug.Assert(f1 <= 0, $"A: {f1}");
					}
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyStructOnStructOnROClassField()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C1
				{
					public S1 Field1;
					public S3 Field2;
					public static readonly C1 instance = new();

					public void M()
					{
						instance.Field1.M();
						instance.Field1.Field1.M();
						{|#0:instance.Field1.Field2.M()|};
						instance.Field2[1].M();
					}
				}

				public struct S1
				{
					public void M() { }
					public S2 Field1;
					public readonly S2 Field2;
				}

				public struct S2
				{
					public void M() { }
				}

				[InlineArray(2)]
				public struct S3
				{
					private S2 field;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M", "Field2");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0);
		}

		[Fact]
		public async Task VerifyCallMutableMemberOnReadonlyInAllowMutatingMembers()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C1
				{
					private readonly S3 _field1;
					private readonly S4 _field2;
					private readonly S5 _field3;
					private readonly S6 _field4;
					public C1()
					{
						_field1.M();
						_field2[0].M();
						_field3.Field.M();
						{|#0:_field4.Field.M()|};
					}
					public int P1
					{
						init
						{
							_field1.M();
							_field2[0].M();
							_field3.Field.M();
							{|#1:_field4.Field.M()|};
						}
					}
					public int P2
					{
						set
						{
							{|#2:_field1.M()|};
							{|#3:_field2[0].M()|};
							{|#4:_field3.Field.M()|};
							{|#5:_field4.Field.M()|};
						}
					}
				}

				public struct S1
				{
					private readonly S3 _field1;
					private readonly S4 _field2;
					private readonly S5 _field3;
					private readonly S6 _field4;
					public S1()
					{
						_field1.M();
						_field2[0].M();
						_field3.Field.M();
						{|#6:_field4.Field.M()|};
					}
					public int P1
					{
						init
						{
							_field1.M();
							_field2[0].M();
							_field3.Field.M();
							{|#7:_field4.Field.M()|};
						}
					}
					public int P2
					{
						set
						{
							{|#8:_field1.M()|};
							{|#9:_field2[0].M()|};
							{|#10:_field3.Field.M()|};
							{|#11:_field4.Field.M()|};
						}
					}
				}

				public readonly struct S2(S3 _field1, S4 _field2, S5 _field3, S6 _field4)
				{
					public S2() : this(default, default, default, default)
					{
						//_field1.M();
						//_field2[0].M();
						//_field3.Field.M();
						//_field4.Field.M();
					}
					public int P1
					{
						init
						{
							_field1.M();
							_field2[0].M();
							_field3.Field.M();
							{|#12:_field4.Field.M()|};
						}
					}
					public int P2
					{
						set
						{
							{|#13:_field1.M()|};
							{|#14:_field2[0].M()|};
							{|#15:_field3.Field.M()|};
							{|#16:_field4.Field.M()|};
						}
					}
					public readonly int _fieldExtra1 = _field1.M();
					public readonly int _fieldExtra2 = _field2[0].M();
					public readonly int _fieldExtra3 = _field3.Field.M();
					public readonly int _fieldExtra4 = {|#17:_field4.Field.M()|};
					public int PropExtra1 { get; } = _field1.M();
					public int PropExtra2 { get; } = _field2[0].M();
					public int PropExtra3 { get; } = _field3.Field.M();
					public int PropExtra4 { get; } = {|#18:_field4.Field.M()|};
				}

				public struct S3
				{
					public int M() => 0;
				}

				[InlineArray(2)]
				public struct S4
				{
					private S3 field;
				}

				public struct S5
				{
					public S3 Field;
				}

				public struct S6
				{
					public readonly S3 Field;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M", "Field");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M", "Field");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M", "_field1");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("M", "this[]");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M", "Field");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("M", "Field");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M", "Field");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M", "Field");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("M", "_field1");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("M", "this[]");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("M", "Field");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("M", "Field");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("M", "Field");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("M", "_field1");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("M", "this[]");
			var expected15 = VerifyCS.Diagnostic("HAM0001").WithLocation(15).WithArguments("M", "Field");
			var expected16 = VerifyCS.Diagnostic("HAM0001").WithLocation(16).WithArguments("M", "Field");
			var expected17 = VerifyCS.Diagnostic("HAM0001").WithLocation(17).WithArguments("M", "Field");
			var expected18 = VerifyCS.Diagnostic("HAM0001").WithLocation(18).WithArguments("M", "Field");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16, expected17, expected18);
		}

		[Fact]
		public async Task VerifyMutateReadonlyStaticFieldInStaticConstructor()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C1
				{
					private static readonly S1 _field1;
					private static readonly S2 _field2;
					private static readonly S3 _field3;
					private static readonly S4 _field4;
					public static int Field1 = _field1.M();
					public static int Field2 = _field2[0].M();
					public static int Field3 = _field3.Field.M();
					public static int Field4 = {|#0:_field4.Field.M()|};
					public int InstanceField1 = {|#1:_field1.M()|};
					public int InstanceField2 = {|#2:_field2[0].M()|};
					public int InstanceField3 = {|#3:_field3.Field.M()|};
					public int InstanceField4 = {|#4:_field4.Field.M()|};
					static C1()
					{
						_field1.M();
						_field2[0].M();
						_field3.Field.M();
						{|#5:_field4.Field.M()|};
					}
				}

				public struct S1
				{
					public int M() => 0;
				}

				[InlineArray(2)]
				public struct S2
				{
					private S1 field;
				}

				public struct S3
				{
					public S1 Field;
				}

				public struct S4
				{
					public readonly S1 Field;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M", "Field");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M", "_field1");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M", "this[]");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("M", "Field");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M", "Field");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("M", "Field");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5);
		}
	}
}
