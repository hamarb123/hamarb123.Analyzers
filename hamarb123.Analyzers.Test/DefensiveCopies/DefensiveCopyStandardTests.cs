using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using VerifyCS = hamarb123.Analyzers.Test.CSharpAnalyzerVerifier<
	hamarb123.Analyzers.DefensiveCopies.DefensiveCopyAnalyzer>;

namespace hamarb123.Analyzers.Test.DefensiveCopies
{
	public class DefensiveCopyStandardTests
	{
		[Fact]
		public async Task VerifyAllRHS()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in S1 value)
					{
						//No defensive copies
						_ = value.F1;
						_ = value.rF1;
						_ = value.P1;
						_ = value.P3;
						_ = value.rP1;
						_ = value.rP2;
						value.rE1 += null;
						value.rE1 -= null;
						value.rM1();
						_ = value.rM2();
						_ = ref value.rM2();
						value.rM2() = 1;
						_ = value[(long)0];
						value[(long)0] = 0;
					}
					public void M2(in S1 value)
					{
						//All defensive copies
						_ = {|#0:value.P2|};
						_ = {|#1:value.rP3|};
						{|#2:value.E1|} += null;
						{|#3:value.E1|} -= null;
						{|#4:value.E2|} += null;
						{|#5:value.E2|} -= null;
						{|#6:value.M1()|};
						_ = {|#7:value.M2()|};
						_ = ref {|#8:value.M2()|};
						{|#9:value.M2()|} = 1;
						_ = {|#10:value[(int)0]|};
					}
				}
				public struct S1
				{
					public int F1;
					public readonly int rF1;
					public int P1 { get; set; } //readonly get
					public int P2 { get => F1; set => F1 = value; }
					public int P3 { get; } //readonly get
					public readonly int rP1 { get => 0; set { } }
					public int rP2 { readonly get => 0; set { } }
					public int rP3 { get => 0; readonly set { } }
					public event Action E1;
					public event Action E2 { add { } remove { } }
					public readonly event Action rE1 { add { } remove { } }
					public void M1() { }
					public readonly void rM1() { }
					public ref int M2() => ref Unsafe.NullRef<int>();
					public readonly ref int rM2() => ref Unsafe.NullRef<int>();
					public int this[int x] { get => 0; set { } }
					public readonly int this[long x] { get => 0; set { } }
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("P2", "value");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("rP3", "value");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("E1", "value");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("E1", "value");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("E2", "value");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("E2", "value");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M1", "value");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M2", "value");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("M2", "value");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("M2", "value");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("this[]", "value");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10);
		}

		[Fact]
		public async Task VerifyAllRHSThroughMultipleFieldAccess()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in S3 value)
					{
						//No defensive copies
						_ = value.Val.Val.F1;
						_ = value.Val.Val.rF1;
						_ = value.Val.Val.P1;
						_ = value.Val.Val.P3;
						_ = value.Val.Val.rP1;
						_ = value.Val.Val.rP2;
						value.Val.Val.rE1 += null;
						value.Val.Val.rE1 -= null;
						value.Val.Val.rM1();
						_ = value.Val.Val.rM2();
						_ = ref value.Val.Val.rM2();
						value.Val.Val.rM2() = 1;
					}
					public void M2(in S3 value)
					{
						//All defensive copies
						_ = {|#0:value.Val.Val.P2|};
						_ = {|#1:value.Val.Val.rP3|};
						{|#2:value.Val.Val.E1|} += null;
						{|#3:value.Val.Val.E1|} -= null;
						{|#4:value.Val.Val.E2|} += null;
						{|#5:value.Val.Val.E2|} -= null;
						{|#6:value.Val.Val.M1()|};
						_ = {|#7:value.Val.Val.M2()|};
						_ = ref {|#8:value.Val.Val.M2()|};
						{|#9:value.Val.Val.M2()|} = 1;
					}
				}
				public struct S1
				{
					public int F1;
					public readonly int rF1;
					public int P1 { get; set; } //readonly get
					public int P2 { get => F1; set => F1 = value; }
					public int P3 { get; } //readonly get
					public readonly int rP1 { get => 0; set { } }
					public int rP2 { readonly get => 0; set { } }
					public int rP3 { get => 0; readonly set { } }
					public event Action E1;
					public event Action E2 { add { } remove { } }
					public readonly event Action rE1 { add { } remove { } }
					public void M1() { }
					public readonly void rM1() { }
					public ref int M2() => ref Unsafe.NullRef<int>();
					public readonly ref int rM2() => ref Unsafe.NullRef<int>();
				}
				public struct S2
				{
					public S1 Val;
				}
				public struct S3
				{
					public S2 Val;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("P2", "Val");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("rP3", "Val");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("E1", "Val");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("E1", "Val");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("E2", "Val");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("E2", "Val");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M1", "Val");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M2", "Val");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("M2", "Val");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("M2", "Val");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9);
		}

		[Fact]
		public async Task VerifyBracketedAccess()
		{
			const string source = """
				using System;

				public class C
				{
					public void M1(in S3 value)
					{
						//No defensive copies

						(value.Val.Val.rE) += null;
						(value.Val.Val.rE) -= null;
						((((value)).Val.Val).rE) += null;
						((((value.Val).Val)).rE) -= null;

						(value.Val.Val.rP1) = 0;
						((((value)).Val.Val).rP1) = 0;
						_ = (value.Val.Val.rP1);
						_ = ((((value)).Val.Val).rP1);

						(value.Val.Val[(uint)0]) = 0;
						((((value)).Val.Val)[(uint)0]) = 0;
						_ = (value.Val.Val[(uint)0]);
						_ = ((((value)).Val.Val)[(uint)0]);
					}
					public void M2(in S3 value)
					{
						//All defensive copies

						({|#0:value.Val.Val.E|}) += null;
						({|#1:value.Val.Val.E|}) -= null;
						(({|#2:(((value)).Val.Val).E|})) += null;
						(({|#3:(((value.Val).Val)).E|})) -= null;

						//(value.Val.Val.P1) = 0;
						//((((value)).Val.Val).P1) = 0;
						_ = ({|#4:value.Val.Val.P1|});
						_ = ({|#5:(((value)).Val.Val).P1|});
						{|#6:(value.Val.Val).M()|};
						{|#7:((((value)).Val.Val)).M()|};

						_ = ({|#8:value.Val.Val[(long)0]|});
						_ = ({|#9:(((value)).Val.Val)[(long)0]|});
					}
				}
				public struct S1
				{
					public event Action E;
					public readonly event Action rE { add { } remove { } }
					public int P1 { get => 0; set { } }
					public readonly int rP1 { get => 0; set { } }
					public void M() { }
					public readonly void rM() { }
					public readonly int this[uint a] { get => 0; set { } }
					public int this[long a] { get => 0; set { } }
				}
				public struct S2
				{
					public S1 Val;
				}
				public struct S3
				{
					public S2 Val;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("E", "Val");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("E", "Val");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("E", "Val");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("E", "Val");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("P1", "Val");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("P1", "Val");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M", "Val");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M", "Val");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("this[]", "Val");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("this[]", "Val");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9);
		}

		[Fact]
		public async Task VerifyRefProperties()
		{
			const string source = """
				using System;
				using System.Diagnostics.CodeAnalysis;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in S3 value)
					{
						//No defensive copies

						_ = value.Val.Val.S_RO_R;
						_ = value.Val.Val.S_RO_R_RO;
						value.Val.Val.S_RO_R = default;

						_ = value.Val.Val.U_RO_R;
						_ = value.Val.Val.U_RO_R_RO;
						value.Val.Val.U_RO_R = default;
					}

					public void M2(in S3 value)
					{
						//All defensive copies

						_ = {|#0:value.Val.Val.S_R|};
						_ = {|#1:value.Val.Val.S_R_RO|};
						{|#2:value.Val.Val.S_R|} = default;

						_ = {|#3:value.Val.Val.U_R|};
						_ = {|#4:value.Val.Val.U_R_RO|};
						{|#5:value.Val.Val.U_R|} = default;
					}

					public void M3(in S3 value)
					{
						//No defensive copies

						value.Val.Val.S_RO_R.M();
						value.Val.Val.S_RO_R.rM();
						value.Val.Val.S_RO_R_RO.rM();

						value.Val.Val.U_RO_R.M();
						value.Val.Val.U_RO_R.rM();
						value.Val.Val.U_RO_R_RO.rM();
					}

					public void M4(in S3 value)
					{
						//All defensive copies

						{|#6:value.Val.Val.S_R|}.M(); //S1
						{|#7:value.Val.Val.S_R|}.rM(); //S1
						{|#9:{|#8:value.Val.Val.S_R_RO|}.M()|}; //S1 and S2
						{|#10:value.Val.Val.S_R_RO|}.rM(); //S1
						{|#11:value.Val.Val.S_RO_R_RO.M()|}; //S2

						{|#12:value.Val.Val.U_R|}.M(); //S1
						{|#13:value.Val.Val.U_R|}.rM(); //S1
						{|#15:{|#14:value.Val.Val.U_R_RO|}.M()|}; //S1 and S2
						{|#16:value.Val.Val.U_R_RO|}.rM(); //S1
						{|#17:value.Val.Val.U_RO_R_RO.M()|}; //S2
					}
				}

				public struct S0
				{
					public void M() { }
					public readonly void rM() { }
				}

				public struct S1
				{
					public S0 F;

					//scoped refs
					public ref S0 S_R => ref Unsafe.NullRef<S0>();
					public readonly ref S0 S_RO_R => ref Unsafe.NullRef<S0>();
					public ref readonly S0 S_R_RO => ref Unsafe.NullRef<S0>();
					public readonly ref readonly S0 S_RO_R_RO => ref Unsafe.NullRef<S0>();

					//unscoped refs
					[UnscopedRef] public ref S0 U_R => ref F;
					[UnscopedRef] public readonly ref S0 U_RO_R => ref Unsafe.NullRef<S0>();
					[UnscopedRef] public ref readonly S0 U_R_RO => ref F;
					[UnscopedRef] public readonly ref readonly S0 U_RO_R_RO => ref F;
				}
				public struct S2
				{
					public S1 Val;
				}
				public struct S3
				{
					public S2 Val;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("S_R", "Val");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("S_R_RO", "Val");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("S_R", "Val");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("U_R", "Val");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("U_R_RO", "Val");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("U_R", "Val");

			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("S_R", "Val");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("S_R", "Val");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("S_R_RO", "Val");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("M", "S_R_RO");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("S_R_RO", "Val");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("M", "S_RO_R_RO");

			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("U_R", "Val");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("U_R", "Val");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("U_R_RO", "Val");
			var expected15 = VerifyCS.Diagnostic("HAM0001").WithLocation(15).WithArguments("M", "U_R_RO");
			var expected16 = VerifyCS.Diagnostic("HAM0001").WithLocation(16).WithArguments("U_R_RO", "Val");
			var expected17 = VerifyCS.Diagnostic("HAM0001").WithLocation(17).WithArguments("M", "U_RO_R_RO");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16, expected17);
		}

		[Fact]
		public async Task VerifyIndexers()
		{
			const string source = """
				using System;
				using System.Diagnostics.CodeAnalysis;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in S3 value)
					{
						//No defensive copies

						_ = value.Val.Val[(long)0];
						_ = value.Val.Val[(ulong)0];
						_ = value.Val.Val[(sbyte)0];

						value.Val.Val[(ulong)0] = 0;
						value.Val.Val[(byte)0] = 0;
						value.Val.Val[(ushort)0] = 0;

						_ = value.Val.Val[0, 1, 2, 3];
						value.Val.Val[0, 1, 2, 3] = 0;

						_ = value.Val.Val[(object)null, (object)null];
						value.Val.Val[(object)null, (object)null].rM();
					}

					public void M2(in S3 value)
					{
						//All defensive copies

						_ = {|#0:value.Val.Val[(int)0]|};
						_ = {|#1:value.Val.Val[(uint)0]|};
						_ = {|#2:value.Val.Val[(byte)0]|};

						_ = {|#3:value.Val.Val[0, 1, 2]|};

						{|#4:value.Val.Val[(object)null, (object)null].M()|};
						_ = {|#5:value.Val.Val[(object)null, (object)null, (object)null]|};
						{|#6:value.Val.Val[(object)null, (object)null, (object)null]|} = 0;
					}
				}

				public struct S0
				{
					public void M() { }
					public readonly void rM() { }
				}
				public struct S1
				{
					public int this[int x] => 0;
					public readonly int this[long x] => 0;
					public int this[uint x] { get => 0; set { } }
					public readonly int this[ulong x] { get => 0; set { } }
					public int this[sbyte x] { readonly get => 0; set { } }
					public int this[byte x] { get => 0; readonly set { } }
					public readonly int this[ushort x] { set { } }
					public int this[int a, int b, object c] { get => 0; set { } }
					public readonly int this[int a, int b, int c, object d] { get => 0; set { } }
					public readonly ref readonly S0 this[object o1, object o2] => ref Unsafe.NullRef<S0>();
					public ref int this[object o1, object o2, object o3] => ref Unsafe.NullRef<int>();
				}
				public struct S2
				{
					public S1 Val;
				}
				public struct S3
				{
					public S2 Val;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("this[]", "Val");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("this[]", "Val");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("this[]", "Val");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("this[]", "Val");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M", "this[]");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("this[]", "Val");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("this[]", "Val");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6);
		}

		[Fact]
		public async Task VerifyAllLHS()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in S1 value1, S2 value2)
					{
						//No defensive copies
						value1.rM();

						value2.Value_R.rM();
						value2.Value_R_RO.rM();
						value2.Value_RO_R.rM();
						value2.Value_RO_R_RO.rM();
						value2.Value_R.M();
						value2.Value_RO_R.M();

						value2.Prop_R.M();
						value2.Prop_R.rM();
						value2.Prop_R_RO.rM();

						value2.M_R().M();
						value2.M_R().rM();
						value2.M_R_RO().rM();

						ref readonly S1 local1 = ref value1;
						local1.rM();

						value2.Field.rM();
						value2.Field.M();
						value2.Field_RO.rM();
					}
					public void M2(in S1 value1, S2 value2)
					{
						//All defensive copies
						{|#0:value1.M()|};

						{|#1:value2.Value_R_RO.M()|};
						{|#2:value2.Value_RO_R_RO.M()|};

						{|#3:value2.Prop_R_RO.M()|};

						{|#4:value2.M_R_RO().M()|};

						ref readonly S1 local1 = ref value1;
						{|#5:local1.M()|};

						{|#6:value2.Field_RO.M()|};
					}
				}
				public struct S1
				{
					public void M() { }
					public readonly void rM() { }
				}
				public ref struct S2
				{
					public ref S1 Value_R;
					public ref readonly S1 Value_R_RO;
					public readonly ref S1 Value_RO_R;
					public readonly ref readonly S1 Value_RO_R_RO;

					public readonly ref S1 Prop_R => ref Unsafe.NullRef<S1>();
					public readonly ref readonly S1 Prop_R_RO => ref Unsafe.NullRef<S1>();

					public readonly ref S1 M_R() => ref Unsafe.NullRef<S1>();
					public readonly ref readonly S1 M_R_RO() => ref Unsafe.NullRef<S1>();

					public S1 Field;
					public readonly S1 Field_RO;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M", "value1");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M", "Value_R_RO");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M", "Value_RO_R_RO");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("M", "Prop_R_RO");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M", "M_R_RO");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("M", "local1");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M", "Field_RO");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6);
		}

		[Fact]
		public async Task VerifyRefFieldAssignment()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in S1 value1, S1 value2)
					{
						//No defensive copies
						ref int local = ref Unsafe.NullRef<int>();

						value1.Field_R = 1;
						value1.Field_RO_R = 1;
						value2.Field_R = 1;
						value2.Field_RO_R = 1;

						_ = value1.Field_R;
						_ = value1.Field_RO_R;
						_ = value2.Field_R;
						_ = value2.Field_RO_R;

						_ = value1.Field_R_RO;
						_ = value1.Field_RO_R_RO;
						_ = value2.Field_R_RO;
						_ = value2.Field_RO_R_RO;

						value2.Field_R = ref local;
						value2.Field_R_RO = ref local;

						_ = ref value1.Field_R;
						_ = ref value1.Field_RO_R;
						_ = ref value2.Field_R;
						_ = ref value2.Field_RO_R;

						_ = ref value1.Field_R_RO;
						_ = ref value1.Field_RO_R_RO;
						_ = ref value2.Field_R_RO;
						_ = ref value2.Field_RO_R_RO;
					}
				}
				public ref struct S1
				{
					public ref int Field_R;
					public ref readonly int Field_R_RO;
					public readonly ref int Field_RO_R;
					public readonly ref readonly int Field_RO_R_RO;
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyDelegates()
		{
			const string source = """
				using System;
				using System.Diagnostics.CodeAnalysis;
				using System.Runtime.CompilerServices;

				public unsafe class C
				{
					public void M1(in S3 value1, in S1 value2)
					{
						//No defensive copies
						Action x;

						x = value1.Val.Val.M;
						x = value1.Val.Val.rM;

						x = value2.M;
						x = value2.rM;
					}
				}

				public struct S1
				{
					public void M() { }
					public readonly void rM() { }
				}
				public struct S2
				{
					public S1 Val;
				}
				public struct S3
				{
					public S2 Val;
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyPointers()
		{
			const string source = """
				using System;
				using System.Diagnostics.CodeAnalysis;
				using System.Runtime.CompilerServices;

				public unsafe class C
				{
					public unsafe void M1(in S3* value1, in S1* value2)
					{
						//No defensive copies

						value1->Val.Val.rM();
						value1->Val.Val.M();

						value1->Val.RO_Val->rM();
						value1->Val.RO_Val->M();

						value2->rM();
						value2->M();

						(*value1).Val.Val.rM();
						(*value1).Val.Val.M();

						(*value1).Val.RO_Val->rM();
						(*value1).Val.RO_Val->M();

						(*value2).rM();
						(*value2).M();

						value1[0].Val.Val.rM();
						value1[0].Val.Val.M();

						value1[0].Val.RO_Val->rM();
						value1[0].Val.RO_Val->M();

						value2[0].rM();
						value2[0].M();
					}
				}

				public struct S1
				{
					public void M() { }
					public readonly void rM() { }
				}
				public unsafe struct S2
				{
					public S1 Val;
					public readonly S1* RO_Val;
				}
				public struct S3
				{
					public S2 Val;
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyAllPositionsNeedingCheck()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				//All defensive copies
				{|#0:C.X().M1()|};

				public class C
				{
					//The ref we will be acting on
					public static ref readonly S1 X() => ref Unsafe.NullRef<S1>();

					//All defensive copies
					public C() => {|#1:X().M1()|};
					public C(int a) { {|#2:X().M1()|}; }
					public void M() => {|#3:X().M1()|};
					public void M(int a) { {|#4:X().M1()|}; }
					public int P1 => {|#5:X().M2()|};
					public int P2
					{
						get => {|#6:X().M2()|};
						set => {|#7:X().M1()|};
					}
					public int P3
					{
						get { return {|#8:X().M2()|}; }
						set { {|#9:X().M1()|}; }
					}
					public event Action E1
					{
						add => {|#10:X().M1()|};
						remove => {|#11:X().M1()|};
					}
					public event Action E2
					{
						add { {|#12:X().M1()|}; }
						remove { {|#13:X().M1()|}; }
					}
					public int F1 = {|#14:X().M2()|};
					public static int F2 = {|#15:X().M2()|};
					public static int operator +(C a) => {|#16:X().M2()|};
					public static int operator +(C a, C b) => {|#17:X().M2()|};
					public static implicit operator int(C a) => {|#18:X().M2()|};
					public static explicit operator long(C a) => {|#19:X().M2()|};
					public int this[int a] => {|#20:X().M2()|};
					public int this[int a, int b]
					{
						get => {|#21:X().M2()|};
						set => {|#22:X().M1()|};
					}
					public int this[int a, int b, int c]
					{
						get { return {|#23:X().M2()|}; }
						set { {|#24:X().M1()|}; }
					}
					static C() { {|#25:X().M1()|}; }
					~C() { {|#26:X().M1()|}; }
					class D
					{
						static D() => {|#27:X().M1()|};
						~D() => {|#28:X().M1()|};
					}
					delegate void DelType(in S1 val);
					public void LocalMethodTest()
					{
						DelType a = (in S1 x) => {|#29:x.M1()|};
						void M(in S1 x) { {|#30:x.M1()|}; }
					}
					public int P4 { get; set; } = {|#31:X().M2()|};
					public int F3 = {|#32:X().M2()|}, F4 = {|#33:X().M2()|}, F5;
				}

				public struct S1
				{
					public void M1() { }
					public int M2() => 0;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "X");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "X");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "X");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("M1", "X");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M1", "X");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("M2", "X");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M2", "X");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M1", "X");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("M2", "X");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("M1", "X");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("M1", "X");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("M1", "X");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("M1", "X");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("M1", "X");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("M2", "X");
			var expected15 = VerifyCS.Diagnostic("HAM0001").WithLocation(15).WithArguments("M2", "X");
			var expected16 = VerifyCS.Diagnostic("HAM0001").WithLocation(16).WithArguments("M2", "X");
			var expected17 = VerifyCS.Diagnostic("HAM0001").WithLocation(17).WithArguments("M2", "X");
			var expected18 = VerifyCS.Diagnostic("HAM0001").WithLocation(18).WithArguments("M2", "X");
			var expected19 = VerifyCS.Diagnostic("HAM0001").WithLocation(19).WithArguments("M2", "X");
			var expected20 = VerifyCS.Diagnostic("HAM0001").WithLocation(20).WithArguments("M2", "X");
			var expected21 = VerifyCS.Diagnostic("HAM0001").WithLocation(21).WithArguments("M2", "X");
			var expected22 = VerifyCS.Diagnostic("HAM0001").WithLocation(22).WithArguments("M1", "X");
			var expected23 = VerifyCS.Diagnostic("HAM0001").WithLocation(23).WithArguments("M2", "X");
			var expected24 = VerifyCS.Diagnostic("HAM0001").WithLocation(24).WithArguments("M1", "X");
			var expected25 = VerifyCS.Diagnostic("HAM0001").WithLocation(25).WithArguments("M1", "X");
			var expected26 = VerifyCS.Diagnostic("HAM0001").WithLocation(26).WithArguments("M1", "X");
			var expected27 = VerifyCS.Diagnostic("HAM0001").WithLocation(27).WithArguments("M1", "X");
			var expected28 = VerifyCS.Diagnostic("HAM0001").WithLocation(28).WithArguments("M1", "X");
			var expected29 = VerifyCS.Diagnostic("HAM0001").WithLocation(29).WithArguments("M1", "x");
			var expected30 = VerifyCS.Diagnostic("HAM0001").WithLocation(30).WithArguments("M1", "x");
			var expected31 = VerifyCS.Diagnostic("HAM0001").WithLocation(31).WithArguments("M2", "X");
			var expected32 = VerifyCS.Diagnostic("HAM0001").WithLocation(32).WithArguments("M2", "X");
			var expected33 = VerifyCS.Diagnostic("HAM0001").WithLocation(33).WithArguments("M2", "X");

			await VerifyCS.VerifyAnalyzerAsync(source, new VerifyCS.Options() { IsLibrary = false },
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16, expected17, expected18, expected19,
				expected20, expected21, expected22, expected23,
				expected24, expected25, expected26, expected27,
				expected28, expected29, expected30, expected31,
				expected32, expected33);
		}

		[Fact]
		public async Task VerifyRefReadonlyParameter()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					//All defensive copies
					public void A1(in S1 a) => {|#0:a.M1()|};
					public void B1(ref readonly S1 a) => {|#1:a.M1()|};

					//No defensive copies
					public void A2(in S1 a) => a.M2();
					public void B2(ref readonly S1 a) => a.M2();
					public void C1(ref S1 a) => a.M1();
					public void C2(ref S1 a) => a.M2();
					public void D1(out S1 a) { a = default; a.M1(); }
					public void D2(out S1 a) { a = default; a.M2(); }
				}

				public struct S1
				{
					public void M1() { }
					public readonly void M2() { }
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "a");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "a");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1);
		}

		[Fact]
		public async Task VerifyPrimaryConstructors()
		{
			const string source = """
				using System;

				public readonly struct S1(S3 a)
				{
					//All defensive copies
					public void M11() => {|#0:a.M1()|};
					public void M21() { {|#1:a.M1()|}; }
					public string P11 => {|#2:a.M1()|};
					public string P21
					{
						get => {|#3:a.M1()|};
						set => {|#4:a.M1()|};
					}
					public string P31
					{
						get { return {|#5:a.M1()|}; }
						set { {|#6:a.M1()|}; }
					}
					public event Action A11
					{
						add => {|#7:a.M1()|};
						remove => {|#8:a.M1()|};
					}
					public event Action A21
					{
						add { {|#9:a.M1()|}; }
						remove { {|#10:a.M1()|}; }
					}
					public string this[int x] => {|#11:a.M1()|};
					public string this[long x]
					{
						get => {|#12:a.M1()|};
						set => {|#13:a.M1()|};
					}
					public string this[short x]
					{
						get { return {|#14:a.M1()|}; }
						set { {|#15:a.M1()|}; }
					}

					//No defensive copies
					public void M12() => a.M2();
					public void M22() { a.M2(); }
					public readonly string F1 = a.M1();
					public readonly string F2 = a.M2();
					public string P12 => a.M2();
					public string P22
					{
						get => a.M2();
						set => a.M2();
					}
					public string P32
					{
						get { return a.M2(); }
						set { a.M2(); }
					}
					public string P41 { get; } = a.M1();
					public string P51 { get; init; } = a.M1();
					public string P42 { get; } = a.M2();
					public string P52 { get; init; } = a.M2();
					public event Action A12
					{
						add => a.M2();
						remove => a.M2();
					}
					public event Action A22
					{
						add { a.M2(); }
						remove { a.M2(); }
					}
					public string this[uint x] => a.M2();
					public string this[ulong x]
					{
						get => a.M2();
						set => a.M2();
					}
					public string this[ushort x]
					{
						get { return a.M2(); }
						set { a.M2(); }
					}
				}

				public struct S2(S3 a)
				{
					//No defensive copies
					public void M11() => a.M1();
					public void M21() { a.M1(); }
					public readonly string F1 = a.M1();
					public string P11 => a.M1();
					public string P21
					{
						get => a.M1();
						set => a.M1();
					}
					public string P31
					{
						get { return a.M1(); }
						set { a.M1(); }
					}
					public string P41 { get; } = a.M1();
					public string P51 { get; init; } = a.M1();
					public event Action A11
					{
						add => a.M1();
						remove => a.M1();
					}
					public event Action A21
					{
						add { a.M1(); }
						remove { a.M1(); }
					}
					public string this[int x] => a.M1();
					public string this[long x]
					{
						get => a.M1();
						set => a.M1();
					}
					public string this[short x]
					{
						get { return a.M1(); }
						set { a.M1(); }
					}

					public void M12() => a.M2();
					public void M22() { a.M2(); }
					public readonly string F2 = a.M2();
					public string P12 => a.M2();
					public string P22
					{
						get => a.M2();
						set => a.M2();
					}
					public string P32
					{
						get { return a.M2(); }
						set { a.M2(); }
					}
					public string P42 { get; } = a.M2();
					public string P52 { get; init; } = a.M2();
					public event Action A12
					{
						add => a.M2();
						remove => a.M2();
					}
					public event Action A22
					{
						add { a.M2(); }
						remove { a.M2(); }
					}
					public string this[uint x] => a.M2();
					public string this[ulong x]
					{
						get => a.M2();
						set => a.M2();
					}
					public string this[ushort x]
					{
						get { return a.M2(); }
						set { a.M2(); }
					}

					//All defensive copies
					public readonly void M31() => {|#16:a.M1()|};
				}

				public class C1(S3 a)
				{
					//No defensive copies
					public void M11() => a.M1();
				}

				public struct S3
				{
					public string M1() => "";
					public readonly string M2() => "";
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "a");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "a");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "a");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("M1", "a");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M1", "a");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("M1", "a");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M1", "a");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M1", "a");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("M1", "a");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("M1", "a");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("M1", "a");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("M1", "a");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("M1", "a");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("M1", "a");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("M1", "a");
			var expected15 = VerifyCS.Diagnostic("HAM0001").WithLocation(15).WithArguments("M1", "a");
			var expected16 = VerifyCS.Diagnostic("HAM0001").WithLocation(16).WithArguments("M1", "a");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16);
		}

		[Fact]
		public async Task VerifyFieldKeyword()
		{
			const string source = """
				using System;

				public struct S1
				{
					public void M1() { }
					public readonly void M2() { }
				}

				public struct S2
				{
					public S1 P1
					{
						get
						{
							field.M1();
							return field;
						}
						set
						{
							field.M1();
							value.M1();
							field = value;
						}
					}

					public readonly S1 P2
					{
						get
						{
							{|#0:field.M1()|};
							field.M2();
							return field;
						}
						set
						{
							{|#1:field.M1()|};
							field.M2();
							value.M1();
						}
					}

					public S1 P3
					{
						readonly get
						{
							{|#2:field.M1()|};
							field.M2();
							return field;
						}
						set
						{
							field.M1();
							value.M1();
							field = value;
						}
					}

					public S1 P4
					{
						get
						{
							field.M1();
							return field;
						}
						readonly set
						{
							{|#3:field.M1()|};
							field.M2();
							value.M1();
						}
					}
				}

				public struct S3
				{
					public readonly S1 field;

					public S1 P1
					{
						get
						{
							field.M1();
							{|#4:this.field.M1()|};
							this.field.M2();
							return field;
						}
						set
						{
							field.M1();
							{|#5:this.field.M1()|};
							this.field.M2();
							value.M1();
						}
					}

					public readonly S1 P2
					{
						get
						{
							{|#6:field.M1()|};
							field.M2();
							{|#7:this.field.M1()|};
							this.field.M2();
							return field;
						}
						set
						{
							{|#8:field.M1()|};
							field.M2();
							{|#9:this.field.M1()|};
							this.field.M2();
							value.M1();
						}
					}

					public void M1()
					{
						{|#10:field.M1()|};
						field.M2();
						{|#11:this.field.M1()|};
						this.field.M2();
					}

					public readonly void M2()
					{
						{|#12:field.M1()|};
						field.M2();
						{|#13:this.field.M1()|};
						this.field.M2();
					}
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M1", "field");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M1", "field");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "field");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("M1", "field");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M1", "field");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("M1", "field");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M1", "field");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M1", "field");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("M1", "field");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("M1", "field");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("M1", "field");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("M1", "field");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("M1", "field");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("M1", "field");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13);
		}
	}
}
