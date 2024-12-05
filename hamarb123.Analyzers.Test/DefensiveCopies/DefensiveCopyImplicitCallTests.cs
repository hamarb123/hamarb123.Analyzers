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
	public class DefensiveCopyImplicitCallTests
	{
		[Fact]
		public async Task VerifyStringAddition()
		{
			//https://github.com/dotnet/roslyn/issues/72044
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					public void M1(in S1 value1, in S3 value2, in S4 value3, in S5<S1> value4, in S5<S3> value5, in S5<S4> value6)
					{
						//No defensive copies necessary (all operations are readonly)
						string result;

						result = "" + {|#0:value1|};
						result = "" + {|#1:value2|};
						result = "" + value3;

						result = "" + 0 + {|#2:value2|};
						result += {|#3:value2|};

						result = "" + {|#4:value4.Value|};
						result = "" + {|#5:value5.Value|};
						result = "" + value6.Value;

						result = "" + 0 + {|#6:value5.Value|};
						result += {|#7:value5.Value|};
					}

					public void M2(in S2 value1, in S5<S2> value2)
					{
						//All defensive copies
						string result;

						result = "" + {|#8:value1|};
						result = "" + 0 + {|#9:value1|};
						result += {|#10:value1|};

						result = "" + {|#11:value2.Value|};
						result = "" + 0 + {|#12:value2.Value|};
						result += {|#13:value2.Value|};

						result += {|#14:(value2).Value|};
						result += ({|#15:value2.Value|});
						result += ({|#16:(value2).Value|});
					}

					public void M3<T1, T2, T3>(in T1 value1, in T2 value2, in T3 value3) where T2 : class where T3 : struct
					{
						string result;

						//All defensive copies
						result = "" + {|#17:value1|};
						result = "" + 0 + {|#18:value1|};
						result += {|#19:value1|};

						//No defensive copies necessary
						result = "" + value2;
						result = "" + 0 + value2;
						result += value2;

						//All defensive copies
						result = "" + {|#20:value3|};
						result = "" + 0 + {|#21:value3|};
						result += {|#22:value3|};
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

				public struct S4
				{
					public override string ToString() => "";
					public static string operator+(string left, S4 right) => "";
				}

				public struct S5<T>
				{
					public T Value;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0003").WithLocation(0).WithArguments("ToString", "value1");
			var expected1 = VerifyCS.Diagnostic("HAM0003").WithLocation(1).WithArguments("ToString", "value2");
			var expected2 = VerifyCS.Diagnostic("HAM0003").WithLocation(2).WithArguments("ToString", "value2");
			var expected3 = VerifyCS.Diagnostic("HAM0003").WithLocation(3).WithArguments("ToString", "value2");
			var expected4 = VerifyCS.Diagnostic("HAM0003").WithLocation(4).WithArguments("ToString", "Value");
			var expected5 = VerifyCS.Diagnostic("HAM0003").WithLocation(5).WithArguments("ToString", "Value");
			var expected6 = VerifyCS.Diagnostic("HAM0003").WithLocation(6).WithArguments("ToString", "Value");
			var expected7 = VerifyCS.Diagnostic("HAM0003").WithLocation(7).WithArguments("ToString", "Value");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("ToString", "value1");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("ToString", "value1");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("ToString", "value1");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("ToString", "Value");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("ToString", "Value");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("ToString", "Value");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("ToString", "Value");
			var expected15 = VerifyCS.Diagnostic("HAM0001").WithLocation(15).WithArguments("ToString", "Value");
			var expected16 = VerifyCS.Diagnostic("HAM0001").WithLocation(16).WithArguments("ToString", "Value");
			var expected17 = VerifyCS.Diagnostic("HAM0001").WithLocation(17).WithArguments("ToString", "value1");
			var expected18 = VerifyCS.Diagnostic("HAM0001").WithLocation(18).WithArguments("ToString", "value1");
			var expected19 = VerifyCS.Diagnostic("HAM0001").WithLocation(19).WithArguments("ToString", "value1");
			var expected20 = VerifyCS.Diagnostic("HAM0001").WithLocation(20).WithArguments("ToString", "value3");
			var expected21 = VerifyCS.Diagnostic("HAM0001").WithLocation(21).WithArguments("ToString", "value3");
			var expected22 = VerifyCS.Diagnostic("HAM0001").WithLocation(22).WithArguments("ToString", "value3");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16, expected17, expected18, expected19,
				expected20, expected21, expected22);
		}

		[Fact]
		public async Task VerifyUsingStatement()
		{
			const string source = """
				using System;

				public class C
				{
					public void M1(
						in S1 val1, in S2 val2, in S3 val3, in S4 val4,
						in S5 val5, in S6 val6, in S7 val7, in S8 val8)
					{
						//No defensive copies

						using (var x1 = val1) { }
						using (var x2 = val2) { }
						using (var x3 = val3) { }
						using (var x4 = val4) { }
						using (var x5 = val5) { }
						using (var x6 = val6) { }
						using (var x7 = val7) { }
						using (var x8 = val8) { }

						using var y1 = val1;
						using var y2 = val2;
						using var y3 = val3;
						using var y4 = val4;
						using var y5 = val5;
						using var y6 = val6;
						using var y7 = val7;
						using var y8 = val8;

						using (var z1 = RO1()) { }
						using (var z2 = RO2()) { }
						using (var z3 = RO3()) { }
						using (var z4 = RO4()) { }
						using (var z5 = RO5()) { }
						using (var z6 = RO6()) { }
						using (var z7 = RO7()) { }
						using (var z8 = RO8()) { }

						using var w1 = RO1();
						using var w2 = RO2();
						using var w3 = RO3();
						using var w4 = RO4();
						using var w5 = RO5();
						using var w6 = RO6();
						using var w7 = RO7();
						using var w8 = RO8();
					}

					public unsafe ref readonly S1 RO1() => ref *(S1*)null;
					public unsafe ref readonly S2 RO2() => ref *(S2*)null;
					public unsafe ref readonly S3 RO3() => ref *(S3*)null;
					public unsafe ref readonly S4 RO4() => ref *(S4*)null;
					public unsafe ref readonly S5 RO5() => ref *(S5*)null;
					public unsafe ref readonly S6 RO6() => ref *(S6*)null;
					public unsafe ref readonly S7 RO7() => ref *(S7*)null;
					public unsafe ref readonly S8 RO8() => ref *(S8*)null;
				}

				public struct S1 : IDisposable
				{
					public void Dispose() { }
				}
				public struct S2 : IDisposable
				{
					public readonly void Dispose() { }
				}
				public readonly struct S3 : IDisposable
				{
					public void Dispose() { }
				}
				public struct S4 : IDisposable
				{
					void IDisposable.Dispose() { }
				}
				public struct S5 : IDisposable
				{
					readonly void IDisposable.Dispose() { }
				}
				public ref struct S6
				{
					public void Dispose() { }
				}
				public ref struct S7
				{
					public readonly void Dispose() { }
				}
				public readonly ref struct S8
				{
					public void Dispose() { }
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyForeach()
		{
			//https://github.com/dotnet/roslyn/issues/72004
			//https://github.com/dotnet/roslyn/issues/72008
			const string source = """
				using System;
				using System.Collections;
				using System.Collections.Generic;

				public class C
				{
					public void M1
					(
						in S4 value4, in S5 value5, in S6 value6, in S8 value8, in S9<S4> _value4, in S9<S5> _value5, in S9<S6> _value6, in S9<S8> _value8, in S9<List<object>> value9, in S10 value10,
						in S11 value11, in S12 value12, in S14 value14
					)
					{
						//No defensive copies necessary (compiler still emits for S4 and S5 though)
						foreach (var x in {|#0:value4|}) { }
						foreach (var x in {|#1:value5|}) { }
						foreach (var x in value6) { }
						foreach (var x in value8) { }
						foreach (var x in {|#2:_value4.Field|}) { }
						foreach (var x in {|#3:_value5.Field|}) { }
						foreach (var x in _value6.Field) { }
						foreach (var x in _value8.Field) { }
						foreach (var x in value9.Field) { }
						foreach (var x in value9.Prop) { }
						foreach (var x in value10.Field) { }
						foreach (var x in value11) { }
						foreach (var x in value12) { }
						foreach (var x in value14) { }
					}
					public void M2
					(
						in S1 value1, in S2 value2, in S3 value3, in S7 value7, in S9<S1> _value1, in S9<S2> _value2, in S9<S3> _value3, in S9<S7> _value7,
						in S13 value13
					)
					{
						//All defensive copies
						foreach (var x in {|#4:value1|}) { }
						foreach (var x in {|#5:value2|}) { }
						foreach (var x in {|#6:value3|}) { }
						foreach (var x in {|#7:value7|}) { }
						foreach (var x in {|#8:_value1.Field|}) { }
						foreach (var x in {|#9:_value2.Field|}) { }
						foreach (var x in {|#10:_value3.Field|}) { }
						foreach (var x in {|#11:_value7.Field|}) { }
						foreach (var x in {|#12:value13|}) { }
					}
				}

				public struct EnumImpl : IEnumerator<int>
				{
					public int Current => 0;
					object IEnumerator.Current => 0;
					public bool MoveNext() => false;
					public void Reset() { }
					public void Dispose() { }
					public static readonly EnumImpl Instance;
				}
				public struct S1 : IEnumerable
				{
					IEnumerator IEnumerable.GetEnumerator() => new EnumImpl();
				}
				public struct S2 : IEnumerable<int>
				{
					IEnumerator<int> IEnumerable<int>.GetEnumerator() => new EnumImpl();
					IEnumerator IEnumerable.GetEnumerator() => new EnumImpl();
				}
				public struct S3 : IEnumerable<int>
				{
					IEnumerator<int> IEnumerable<int>.GetEnumerator() => new EnumImpl();
					IEnumerator IEnumerable.GetEnumerator() => new EnumImpl();
					public EnumImpl GetEnumerator() => new EnumImpl();
				}
				public struct S4 : IEnumerable
				{
					readonly IEnumerator IEnumerable.GetEnumerator() => new EnumImpl();
				}
				public struct S5 : IEnumerable<int>
				{
					readonly IEnumerator<int> IEnumerable<int>.GetEnumerator() => new EnumImpl();
					readonly IEnumerator IEnumerable.GetEnumerator() => new EnumImpl();
				}
				public struct S6 : IEnumerable<int>
				{
					readonly IEnumerator<int> IEnumerable<int>.GetEnumerator() => new EnumImpl();
					readonly IEnumerator IEnumerable.GetEnumerator() => new EnumImpl();
					readonly public EnumImpl GetEnumerator() => new EnumImpl();
				}
				public struct S7 : IEnumerable<int>
				{
					readonly IEnumerator<int> IEnumerable<int>.GetEnumerator() => new EnumImpl();
					readonly IEnumerator IEnumerable.GetEnumerator() => new EnumImpl();
					public ref readonly EnumImpl GetEnumerator() => ref EnumImpl.Instance;
				}
				public struct S8 : IEnumerable<int>
				{
					readonly IEnumerator<int> IEnumerable<int>.GetEnumerator() => new EnumImpl();
					readonly IEnumerator IEnumerable.GetEnumerator() => new EnumImpl();
					public readonly ref readonly EnumImpl GetEnumerator() => ref EnumImpl.Instance;
				}
				public struct S9<T>
				{
					public T Field;
					public T Prop { get; }
				}
				public struct S10
				{
					public List<object> Field;
				}
				public struct S11
				{
				}
				public struct S12
				{
				}
				public ref struct S13
				{
					public IEnumerator<int> GetEnumerator() => throw null;
				}
				public ref struct S14
				{
					public readonly IEnumerator<int> GetEnumerator() => throw null;
				}
				public static class Extensions
				{
					public static IEnumerator<int> GetEnumerator(this in S11 value) => throw null;
					public static IEnumerator<int> GetEnumerator(this S12 value) => throw null;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0003").WithLocation(0).WithArguments("GetEnumerator", "value4");
			var expected1 = VerifyCS.Diagnostic("HAM0003").WithLocation(1).WithArguments("GetEnumerator", "value5");
			var expected2 = VerifyCS.Diagnostic("HAM0003").WithLocation(2).WithArguments("GetEnumerator", "Field");
			var expected3 = VerifyCS.Diagnostic("HAM0003").WithLocation(3).WithArguments("GetEnumerator", "Field");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("GetEnumerator", "value1");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("GetEnumerator", "value2");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("GetEnumerator", "value3");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("GetEnumerator", "value7");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("GetEnumerator", "Field");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("GetEnumerator", "Field");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("GetEnumerator", "Field");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("GetEnumerator", "Field");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("GetEnumerator", "value13");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12);
		}

		[Fact]
		public async Task VerifyDeconstruction()
		{
			//https://github.com/dotnet/roslyn/issues/72008
			const string source = """
				using System;

				public class C
				{
					public void M1(in S1 value1, in S2 value2)
					{
						//No defensive copies
						var (a1, b1) = value1;
						var (a2, b2) = value2;
					}
				}

				public struct S1
				{
					public void Deconstruct(out int a, out int b)
					{
						a = 0;
						b = 0;
					}
				}

				public struct S2
				{
					public readonly void Deconstruct(out int a, out int b)
					{
						a = 0;
						b = 0;
					}
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyFixed()
		{
			const string source = """
				using System;

				public class C
				{
					public unsafe void M1<T1, T2, T3, T4, T1B, T2B, T3B, RT1, RT2, RT1B, RT2B>
					(
						in S2 value2RO, ref S1 value1, ref S2 value2, ref S3 value3, in S4 value4RO, ref S4 value4, in S5 value5RO, ref S5 value5,
						ref T1 valueT1, ref T2 valueT2, in T3 valueT3RO, ref T3 valueT3, in T4 valueT4RO, ref T4 valueT4, C1 valueC1, ref C1 refValueC1, ref readonly C1 refROValueC1,
						ref T1B valueT1B, ref T2B valueT2B, in T3B valueT3BRO, ref T3B valueT3B,
						in S6 value6RO, in S7 value7RO,
						in RS2 rsValue2RO, ref RS1 rsValue1, ref RS2 rsValue2, ref RS3 rsValue3, in RS4 rsValue4RO, ref RS4 rsValue4, in RS5 rsValue5RO, ref RS5 rsValue5,
						ref RT1 rsValueRT1, ref RT2 rsValueRT2,
						ref RT1B rsValueRT1B, ref RT2B rsValueRT2B,
						in RS6 rsValue6RO, in RS7 rsValue7RO
					)
						where T1 : I1 where T2 : struct, I1 where T3 : class, I1 where T4 : C1 where T1B : I2 where T2B : struct, I2 where T3B : class, I2
						where RT1 : I1, allows ref struct where RT2 : struct, I1, allows ref struct where RT1B : I2, allows ref struct where RT2B : struct, I2, allows ref struct
					{
						//No defensive copies
						fixed (int* ptr = value2RO) { }
						fixed (int* ptr = value1) { }
						fixed (int* ptr = value2) { }
						fixed (int* ptr = value3) { }
						fixed (int* ptr = value4RO) { }
						fixed (int* ptr = value4) { }
						fixed (int* ptr = value5RO) { }
						fixed (int* ptr = value5) { }
						fixed (int* ptr = valueT1) { }
						fixed (int* ptr = valueT2) { }
						fixed (int* ptr = valueT3RO) { }
						fixed (int* ptr = valueT3) { }
						fixed (int* ptr = valueT4RO) { }
						fixed (int* ptr = valueT4) { }
						fixed (int* ptr = valueC1) { }
						fixed (int* ptr = refValueC1) { }
						fixed (int* ptr = refROValueC1) { }
						fixed (int* ptr = valueT1B) { }
						fixed (int* ptr = valueT2B) { }
						fixed (int* ptr = valueT3BRO) { }
						fixed (int* ptr = valueT3B) { }
						fixed (int* ptr = value6RO) { }
						fixed (int* ptr = value7RO) { }

						fixed (int* ptr = rsValue2RO) { }
						fixed (int* ptr = rsValue1) { }
						fixed (int* ptr = rsValue2) { }
						fixed (int* ptr = rsValue3) { }
						fixed (int* ptr = rsValue4RO) { }
						fixed (int* ptr = rsValue4) { }
						fixed (int* ptr = rsValue5RO) { }
						fixed (int* ptr = rsValue5) { }
						fixed (int* ptr = rsValueRT1) { }
						fixed (int* ptr = rsValueRT2) { }
						fixed (int* ptr = rsValueRT1B) { }
						fixed (int* ptr = rsValueRT2B) { }
						fixed (int* ptr = rsValue6RO) { }
						fixed (int* ptr = rsValue7RO) { }

						fixed (void* ptr = value1) { }
						fixed (int* ptr1 = value1, ptr2 = value2) { }

						//No defensive copies (pointer)
						fixed (void* ptr = &value2RO) { }
						fixed (void* ptr = &value1) { }
						fixed (void* ptr = &value2) { }
						fixed (void* ptr = &value3) { }
						fixed (void* ptr = &value4RO) { }
						fixed (void* ptr = &value4) { }
						fixed (void* ptr = &value5RO) { }
						fixed (void* ptr = &value5) { }
						fixed (void* ptr = &valueT1) { }
						fixed (void* ptr = &valueT2) { }
						fixed (void* ptr = &valueT3RO) { }
						fixed (void* ptr = &valueT3) { }
						fixed (void* ptr = &valueT4RO) { }
						fixed (void* ptr = &valueT4) { }
						//fixed (void* ptr = &valueC1) { }
						fixed (void* ptr = &refValueC1) { }
						fixed (void* ptr = &refROValueC1) { }
						fixed (void* ptr = &valueT1B) { }
						fixed (void* ptr = &valueT2B) { }
						fixed (void* ptr = &valueT3BRO) { }
						fixed (void* ptr = &valueT3B) { }
						fixed (void* ptr = &value6RO) { }
						fixed (void* ptr = &value7RO) { }

						fixed (void* ptr = &rsValue2RO) { }
						fixed (void* ptr = &rsValue1) { }
						fixed (void* ptr = &rsValue2) { }
						fixed (void* ptr = &rsValue3) { }
						fixed (void* ptr = &rsValue4RO) { }
						fixed (void* ptr = &rsValue4) { }
						fixed (void* ptr = &rsValue5RO) { }
						fixed (void* ptr = &rsValue5) { }
						fixed (void* ptr = &rsValueRT1) { }
						fixed (void* ptr = &rsValueRT2) { }
						fixed (void* ptr = &rsValueRT1B) { }
						fixed (void* ptr = &rsValueRT2B) { }
						fixed (void* ptr = &rsValue6RO) { }
						fixed (void* ptr = &rsValue7RO) { }

						fixed (void* ptr1 = &value1, ptr2 = &value2) { }
					}

					public unsafe void M2<T1, T2, T1B, T2B, RT1, RT2, RT1B, RT2B>
					(
						in S2 value2RO, ref S1 value1, in S1 value1RO, in T1 valueT1RO, in T2 valueT2RO, in T1B valueT1BRO, in T2B valueT2BRO,
						in RS2 rsValue2RO, ref RS1 rsValue1, in RS1 rsValue1RO, in RT1 rsValueRT1RO, in RT2 rsValueRT2RO, in RT1B rsValueRT1BRO, in RT2B rsValueRT2BRO
					)
						where T1 : I1 where T2 : struct, I1 where T1B : I2 where T2B : struct, I2
						where RT1 : I1, allows ref struct where RT2 : struct, I1, allows ref struct where RT1B : I2, allows ref struct where RT2B : struct, I2, allows ref struct
					{
						//All defensive copies
						fixed (int* ptr = {|#0:value1RO|}) { }
						fixed (int* ptr = {|#1:valueT1RO|}) { }
						fixed (int* ptr = {|#2:valueT2RO|}) { }
						fixed (int* ptr = {|#3:valueT1BRO|}) { }
						fixed (int* ptr = {|#4:valueT2BRO|}) { }

						//Some defensive copies (first declaration always, second for first line also)
						fixed (int* ptr1 = {|#5:value1RO|}, ptr2 = {|#6:valueT1RO|}) { }
						fixed (void* ptr1 = {|#7:value1RO|}) { }
						fixed (void* ptr1 = {|#8:value1RO|}, ptr2 = &value1RO) { }
						fixed (void* ptr1 = {|#9:value1RO|}, ptr2 = value1) { }
						fixed (void* ptr1 = {|#10:value1RO|}, ptr2 = value2RO) { }

						//No defensive copies (pointer)
						fixed (void* ptr = &value1RO) { }
						fixed (void* ptr = &valueT1RO) { }
						fixed (void* ptr = &valueT2RO) { }
						fixed (void* ptr = &valueT1BRO) { }
						fixed (void* ptr = &valueT2BRO) { }

						//All defensive copies
						fixed (int* ptr = {|#11:rsValue1RO|}) { }
						fixed (int* ptr = {|#12:rsValueRT1RO|}) { }
						fixed (int* ptr = {|#13:rsValueRT2RO|}) { }
						fixed (int* ptr = {|#14:rsValueRT1BRO|}) { }
						fixed (int* ptr = {|#15:rsValueRT2BRO|}) { }

						//Some defensive copies (first declaration always, second for first line also)
						fixed (int* ptr1 = {|#16:rsValue1RO|}, ptr2 = {|#17:rsValueRT1RO|}) { }
						fixed (void* ptr1 = {|#18:rsValue1RO|}) { }
						fixed (void* ptr1 = {|#19:rsValue1RO|}, ptr2 = &rsValue1RO) { }
						fixed (void* ptr1 = {|#20:rsValue1RO|}, ptr2 = rsValue1) { }
						fixed (void* ptr1 = {|#21:rsValue1RO|}, ptr2 = rsValue2RO) { }

						//No defensive copies (pointer)
						fixed (void* ptr = &rsValue1RO) { }
						fixed (void* ptr = &rsValueRT1RO) { }
						fixed (void* ptr = &rsValueRT2RO) { }
						fixed (void* ptr = &rsValueRT1BRO) { }
						fixed (void* ptr = &rsValueRT2BRO) { }
					}
				}

				public struct S1
				{
					public ref int GetPinnableReference() => throw null;
				}

				public struct S2
				{
					public readonly ref int GetPinnableReference() => throw null;
				}

				public struct S3
				{
				}

				public struct S4
				{
				}

				public struct S5
				{
				}

				public struct S6
				{
					private ref int GetPinnableReference() => throw null;
					public static unsafe void M2(in S6 value6RO)
					{
						//All defensive copies
						fixed (int* ptr = {|#22:value6RO|}) { }

						//No defensive copies (pointer)
						fixed (void* ptr = &value6RO) { }
					}
				}

				public struct S7 : I1
				{
					ref int I1.GetPinnableReference() => throw null;
				}

				public ref struct RS1
				{
					public ref int GetPinnableReference() => throw null;
				}

				public ref struct RS2
				{
					public readonly ref int GetPinnableReference() => throw null;
				}

				public ref struct RS3
				{
				}

				public ref struct RS4
				{
				}

				public ref struct RS5
				{
				}

				public ref struct RS6
				{
					private ref int GetPinnableReference() => throw null;
					public static unsafe void M2(in RS6 rsValue6RO)
					{
						//All defensive copies
						fixed (int* ptr = {|#23:rsValue6RO|}) { }

						//No defensive copies (pointer)
						fixed (void* ptr = &rsValue6RO) { }
					}
				}

				public ref struct RS7 : I1
				{
					ref int I1.GetPinnableReference() => throw null;
				}

				public static class Extensions
				{
					public static ref int GetPinnableReference(this ref S3 value) => throw null;
					public static ref int GetPinnableReference(this in S4 value) => throw null;
					public static ref int GetPinnableReference(this S5 value) => throw null;
					public static ref int GetPinnableReference(this in S6 value) => throw null;
					public static ref int GetPinnableReference(this in S7 value) => throw null;
					public static ref int GetPinnableReference(this ref RS3 value) => throw null;
					public static ref int GetPinnableReference(this in RS4 value) => throw null;
					public static ref int GetPinnableReference(this RS5 value) => throw null;
					public static ref int GetPinnableReference(this in RS6 value) => throw null;
					public static ref int GetPinnableReference(this in RS7 value) => throw null;
				}

				public interface I1
				{
					ref int GetPinnableReference();
				}

				public interface I2 : I1
				{
				}

				public class C1
				{
					public virtual ref int GetPinnableReference() => throw null;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("GetPinnableReference", "value1RO");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("GetPinnableReference", "valueT1RO");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("GetPinnableReference", "valueT2RO");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("GetPinnableReference", "valueT1BRO");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("GetPinnableReference", "valueT2BRO");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("GetPinnableReference", "value1RO");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("GetPinnableReference", "valueT1RO");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("GetPinnableReference", "value1RO");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("GetPinnableReference", "value1RO");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("GetPinnableReference", "value1RO");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("GetPinnableReference", "value1RO");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("GetPinnableReference", "rsValue1RO");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("GetPinnableReference", "rsValueRT1RO");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("GetPinnableReference", "rsValueRT2RO");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("GetPinnableReference", "rsValueRT1BRO");
			var expected15 = VerifyCS.Diagnostic("HAM0001").WithLocation(15).WithArguments("GetPinnableReference", "rsValueRT2BRO");
			var expected16 = VerifyCS.Diagnostic("HAM0001").WithLocation(16).WithArguments("GetPinnableReference", "rsValue1RO");
			var expected17 = VerifyCS.Diagnostic("HAM0001").WithLocation(17).WithArguments("GetPinnableReference", "rsValueRT1RO");
			var expected18 = VerifyCS.Diagnostic("HAM0001").WithLocation(18).WithArguments("GetPinnableReference", "rsValue1RO");
			var expected19 = VerifyCS.Diagnostic("HAM0001").WithLocation(19).WithArguments("GetPinnableReference", "rsValue1RO");
			var expected20 = VerifyCS.Diagnostic("HAM0001").WithLocation(20).WithArguments("GetPinnableReference", "rsValue1RO");
			var expected21 = VerifyCS.Diagnostic("HAM0001").WithLocation(21).WithArguments("GetPinnableReference", "rsValue1RO");
			var expected22 = VerifyCS.Diagnostic("HAM0001").WithLocation(22).WithArguments("GetPinnableReference", "value6RO");
			var expected23 = VerifyCS.Diagnostic("HAM0001").WithLocation(23).WithArguments("GetPinnableReference", "rsValue6RO");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16, expected17, expected18, expected19,
				expected20, expected21, expected22, expected23);
		}

		[Fact]
		public async Task VerifyRangeIndex()
		{
			//https://github.com/dotnet/roslyn/issues/72008
			//Compiler emits defensive copy for ones passed by in, even though members are readonly
			const string source = """
				using System;

				public class C
				{
					public void M1(ref S1 a, ref S2 b, ref S3 c, ref S4 d, Index i, Range r)
					{
						//No defensive copy
						_ = a[4..8];
						_ = a[4..^8];
						_ = a[^1];
						_ = a[0];
						_ = a[i];
						_ = a[0..i];
						_ = a[r];
						_ = a[0..(Index)1];

						//No defensive copy
						_ = b[4..8];
						_ = b[4..^8];
						_ = b[^1];
						_ = b[0];
						_ = b[i];
						_ = b[0..i];
						_ = b[r];
						_ = b[0..(Index)1];

						//No defensive copy
						_ = c[4..8];
						_ = c[4..^8];
						_ = c[^1];
						_ = c[0];
						_ = c[i];
						_ = c[0..i];
						_ = c[r];
						_ = c[0..(Index)1];

						//No defensive copy
						_ = d[4..8];
						_ = d[4..^8];
						_ = d[^1];
						_ = d[0];
						_ = d[i];
						_ = d[0..i];
						_ = d[r];
						_ = d[0..(Index)1];
					}

					public void M2(in S1 a, in S2 b, in S3 c, in S4 d, Index i, Range r)
					{
						//Non-mutating defensive copy
						_ = {|#0:a[4..8]|};
						_ = {|#1:a[4..^8]|};
						_ = {|#2:a[^1]|};
						_ = a[0]; //No defensive copy
						_ = {|#3:a[i]|};
						_ = {|#4:a[0..i]|};
						_ = {|#5:a[r]|};
						_ = {|#6:a[0..(Index)1]|};

						//Defensive copy
						_ = {|#7:b[4..8]|};
						_ = {|#8:b[4..^8]|};
						_ = {|#9:b[^1]|};
						_ = {|#10:b[0]|};
						_ = {|#11:b[i]|};
						_ = {|#12:b[0..i]|};
						_ = {|#13:b[r]|};
						_ = {|#14:b[0..(Index)1]|};

						//Defensive copy
						_ = {|#15:c[4..8]|};
						_ = {|#16:c[4..^8]|};
						_ = {|#17:c[^1]|};
						_ = {|#18:c[0]|};
						_ = {|#19:c[i]|};
						_ = {|#20:c[0..i]|};
						_ = {|#21:c[r]|};
						_ = {|#22:c[0..(Index)1]|};

						//Defensive copy
						_ = {|#23:d[4..8]|}; //Unnecessary defensive copy
						_ = {|#24:d[4..^8]|};
						_ = {|#25:d[^1]|};
						_ = d[0]; //No defensive copy
						_ = {|#26:d[i]|};
						_ = {|#27:d[0..i]|};
						_ = {|#28:d[r]|};
						_ = {|#29:d[0..(Index)1]|};
					}
				}

				public struct S1
				{
					public readonly int Length => 0;
					public readonly int this[int index] => 0;
					public readonly S1 Slice(int start, int length) => this;
				}

				public struct S2
				{
					public int Length => 0;
					public int this[int index] => 0;
					public S2 Slice(int start, int length) => this;
				}

				public struct S3
				{
					public readonly int Length => 0;
					public int this[int index] => 0;
					public S3 Slice(int start, int length) => this;
				}

				public struct S4
				{
					public int Length => 0;
					public readonly int this[int index] => 0;
					public readonly S4 Slice(int start, int length) => this;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0003").WithLocation(0).WithArguments("this[]", "a");
			var expected1 = VerifyCS.Diagnostic("HAM0003").WithLocation(1).WithArguments("this[]", "a");
			var expected2 = VerifyCS.Diagnostic("HAM0003").WithLocation(2).WithArguments("this[]", "a");
			var expected3 = VerifyCS.Diagnostic("HAM0003").WithLocation(3).WithArguments("this[]", "a");
			var expected4 = VerifyCS.Diagnostic("HAM0003").WithLocation(4).WithArguments("this[]", "a");
			var expected5 = VerifyCS.Diagnostic("HAM0003").WithLocation(5).WithArguments("this[]", "a");
			var expected6 = VerifyCS.Diagnostic("HAM0003").WithLocation(6).WithArguments("this[]", "a");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("this[]", "b");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("this[]", "b");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("this[]", "b");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("this[]", "b");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("this[]", "b");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("this[]", "b");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("this[]", "b");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("this[]", "b");
			var expected15 = VerifyCS.Diagnostic("HAM0001").WithLocation(15).WithArguments("this[]", "c");
			var expected16 = VerifyCS.Diagnostic("HAM0001").WithLocation(16).WithArguments("this[]", "c");
			var expected17 = VerifyCS.Diagnostic("HAM0001").WithLocation(17).WithArguments("this[]", "c");
			var expected18 = VerifyCS.Diagnostic("HAM0001").WithLocation(18).WithArguments("this[]", "c");
			var expected19 = VerifyCS.Diagnostic("HAM0001").WithLocation(19).WithArguments("this[]", "c");
			var expected20 = VerifyCS.Diagnostic("HAM0001").WithLocation(20).WithArguments("this[]", "c");
			var expected21 = VerifyCS.Diagnostic("HAM0001").WithLocation(21).WithArguments("this[]", "c");
			var expected22 = VerifyCS.Diagnostic("HAM0001").WithLocation(22).WithArguments("this[]", "c");
			var expected23 = VerifyCS.Diagnostic("HAM0003").WithLocation(23).WithArguments("this[]", "d");
			var expected24 = VerifyCS.Diagnostic("HAM0001").WithLocation(24).WithArguments("Length", "d");
			var expected25 = VerifyCS.Diagnostic("HAM0001").WithLocation(25).WithArguments("Length", "d");
			var expected26 = VerifyCS.Diagnostic("HAM0001").WithLocation(26).WithArguments("Length", "d");
			var expected27 = VerifyCS.Diagnostic("HAM0001").WithLocation(27).WithArguments("Length", "d");
			var expected28 = VerifyCS.Diagnostic("HAM0001").WithLocation(28).WithArguments("Length", "d");
			var expected29 = VerifyCS.Diagnostic("HAM0001").WithLocation(29).WithArguments("Length", "d");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16, expected17, expected18, expected19,
				expected20, expected21, expected22, expected23,
				expected24, expected25, expected26, expected27,
				expected28, expected29);
		}
	}
}
