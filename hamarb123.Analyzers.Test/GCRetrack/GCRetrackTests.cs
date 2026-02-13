using Xunit;
using VerifyCS = hamarb123.Analyzers.Test.CSharpAnalyzerVerifier<
	hamarb123.Analyzers.GCRetrack.GCRetrackAnalyzer>;

namespace hamarb123.Analyzers.Test.GCRetrack
{
	public class GCRetrackTests
	{
		[Fact]
		public async Task Verify()
		{
			const string source = """
				using System;
				using System.Collections.Generic;
				using System.Globalization;
				using System.Runtime.CompilerServices;

				public class C
				{
					public ref byte Consume(ref byte b) => ref b;
					public ref string Consume(ref string s) => ref s;
					public void Consume(ref byte b, int i) { }
					public void Consume(ref byte b1, ref byte b2) { }
					public void Consume(ref string s, int i) { }
					public int GetInt() => 0;

					public unsafe void M()
					{
						byte* ptr = null;
						ValueTuple<byte>* ptrVT = null;
						ValueTuple<ValueTuple<byte>>* ptrVT2 = null;
						ValueTuple<ValueTuple<ValueTuple<byte>>>* ptrVT3 = null;
						byte local = 0;
						ValueTuple<byte> localVT = default;
						Consume(ref *ptr);
						Consume({|#0:ref *ptr|}, GetInt());
						Consume(ref *&local, GetInt());
						Consume(ref *(byte*)(int*)&local, GetInt());
						Consume(ref *(byte*)(void*)&local, GetInt());
						ref var local1 = ref *ptr;
						ref var local2 = ref 1 == 0 ? ref *ptr : ref *ptr;
						local1 = ref 1 == 0 ? ref *ptr : ref Consume(ref *ptr);
						local1 = ref 1 == 0 ? ref Consume(ref *ptr) : ref *ptr;
						local1 = ref *ptr;
						local1 = ref *(ptr);
						local1 = ref (*ptr);
						local1 = ref ptr[0];
						local1 = ref ptr[(int)1.ToString()[0]];
						local1 = ref ptr[new List<int>()[0]];
						local1 = ref ptrVT->Item1;
						int x = 0;
						Consume(ref *ptr, x);
						Consume(ref *ptr, x + 1);
						Consume(ref *ptr, x * 2);
						Consume(ref *ptr, checked(x * 2));
						Consume(ref *ptr, x switch
						{
							0 => x,
							1 => x + 1,
							_ => throw new Exception(),
						});
						Consume({|#1:ref *ptr|}, new List<int>()[0]);
						Consume(ref *ptr, (int)1.ToString()[0]);
						Consume(ref *ptr, (new int[] { 34 })[0]);
						Consume(ref *ptr, ((int[])[34])[0]);
						Consume(ref 1 == 0 ? ref Consume(ref *ptr) : ref *ptr);
						Consume(ref 1 == 0 ? ref *ptr : ref Consume(ref *ptr));
						Consume({|#2:ref *ptr|}, ref Consume(ref *ptr));
						Consume({|#3:ref *ptr|}, x++);
						Consume({|#4:ref *ptr|}, x = 1);
						Consume(ref *ptr, ((Span<int>)new int[1])[0]);
						Consume(ref *ptr, ((ReadOnlySpan<int>)new int[1])[0]);
						Consume(ref *ptr, (new int[1]).AsSpan()[0]);
						Consume(ref *ptr, ((ReadOnlySpan<int>)(new int[1]).AsSpan())[0]);
						Consume(ref *ptr, new Span<byte>(ptr, 0)[0]);
						Consume({|#5:ref *ptr|}, (new byte[1]).AsSpan().TryCopyTo(new Span<byte>(ptr, 0)) ? 1 : 0);
						Consume(ref *ptr, (new object[1])[0] is not null ? 1 : 0);
						Consume(ref *ptr, (new object[1]) is [not null] ? 1 : 0);
						Consume({|#6:ref *ptr|}, (new object[1]).Contains(null) ? 1 : 0);
						Consume(ref *ptr, (new int[1]).Contains(0) ? 1 : 0);
						Consume(ref *ptr, (new int[1]).IndexOf(0));
						Consume({|#7:ref *ptr|}, (new int[1]).IndexOf(0, null));
						Consume(ref *(byte*)(IntPtr)(void*)&local, GetInt());
						Consume(ref *(byte*)(nint)(void*)&local, GetInt());
						Consume(ref *(byte*)(long)&local, GetInt());
						Consume({|#8:ref *(byte*)(short)&local|}, GetInt());
						Consume(ref ((ValueTuple<byte>*)(long)&localVT)->Item1, GetInt());
						Consume(ref ((ValueTuple<byte>*)(long)&localVT + 1)->Item1, GetInt());
						Consume(ref ((ValueTuple<byte>*)0)->Item1, GetInt());
						Consume(ref ((ValueTuple<byte>*)0 + 1)->Item1, GetInt());
						Consume(ref (1 + (ValueTuple<byte>*)0)->Item1, GetInt());
						Consume({|#9:ref ((int)&local + (ValueTuple<byte>*)0)->Item1|}, GetInt());
						Consume({|#10:ref ((int)&local + (ValueTuple<byte>*)&local)->Item1|}, GetInt());
						Consume(ref ((short)(int)0 + (ValueTuple<byte>*)&local)->Item1, GetInt());
						Consume(ref *ptr, $"{0}abcdef"[0]);
						Consume(ref *ptr, new string((char*)ptr)[0]);
						Consume(ref *ptr, string.Empty[0]);
						Consume(ref *ptr, $"{0}abc".Length);
						Consume(ref *ptr++);
						Consume({|#11:ref 1 == 0 ? ref *ptr : ref *ptr|}, GetInt());
						Consume({|#12:ref 1 == 0 ? ref (1 == 0 ? ref *ptr : ref *ptr) : ref (1 == 0 ? ref *ptr : ref *ptr)|}, GetInt());
						_ = 1 == 0 ? ref *ptr : ref *ptr;
						Consume(ref *ptr, sizeof(int));
						Consume(ref *ptr, sizeof(nint));
						Consume(ref *ptr, nameof(ptr)[0]);
						Consume(ref *ptr, new object() is int ? 1 : 0);
						Consume(ref *ptr, (new object() as string)[0]);
						Consume(ref *ptr, local + local);
						Consume(ref *ptr, (int?)local ?? 0);
						Consume(ref *ptr, (1, 2).Item1);
						Consume(ref *ptr, int.TryParse("abc", out var tmp1) ? tmp1 : 0);
						Consume(ref *ptr, int.Parse("abc", CultureInfo.InvariantCulture));
						Consume(ref *ptr, int.Parse("abc", (CultureInfo)null));
						Consume({|#13:ref *ptr|}, int.TryParse("abc", out tmp1) ? tmp1 : 0);
						Consume({|#14:ref *ptr|}, ((Func<int>)(() => 0))());
						Consume(ref *ptr, (int)(object)(Func<int>)(() => 0));
						Consume(ref *ptr, (1, 2).ToString()[0]);
						Consume({|#15:ref *ptr|}, (1, new object()).ToString()[0]);
						Consume(ref *ptr, Unsafe.ReadUnaligned<int>(ref Unsafe.AsRef<byte>(ptr)));
						Consume(ref *ptr, Unsafe.ReadUnaligned<int>(ref Unsafe.As<int, byte>(ref *(int*)ptr)));
						S s = new();
						Consume(ref (&s)->Field->Item1);
						Consume(ref ptrVT2->Item1.Item1);
						Consume({|#16:ref ptrVT2->Item1.Item1|}, GetInt());
						Consume(ref ptrVT3->Item1.Item1.Item1);
						Consume({|#17:ref ptrVT3->Item1.Item1.Item1|}, GetInt());
						Consume(ref true ? ref ptrVT3->Item1.Item1.Item1 : ref local);
						Consume({|#18:ref true ? ref ptrVT3->Item1.Item1.Item1 : ref local|}, GetInt());
						Consume(ref true ? ref local : ref ptrVT3->Item1.Item1.Item1);
						Consume({|#19:ref true ? ref local : ref ptrVT3->Item1.Item1.Item1|}, GetInt());
						Consume(ref ((((*ptrVT3).Item1).Item1.Item1)));
						Consume({|#20:ref ((((*ptrVT3).Item1).Item1.Item1))|}, GetInt());
						Consume(ref ptrVT3[0].Item1.Item1.Item1);
						Consume({|#21:ref ptrVT3[0].Item1.Item1.Item1|}, GetInt());
						Consume(ref *(ptr));
						Consume({|#22:ref *(ptr)|}, GetInt());
						Consume(ref (*ptr));
						Consume({|#23:ref (*ptr)|}, GetInt());
						Consume({|#24:ref *(string*)ptr|}, GetInt());

				#nullable enable
						Consume({|#25:ref *(string?*)ptr!|}, GetInt());
						Consume({|#26:ref *((string?*)ptr!)|}, GetInt());
						var ptrString = (string?*)ptr;
						Consume({|#27:ref *ptrString!|}, GetInt());
						Consume({|#28:ref *(ptrString!)|}, GetInt());
						ValueTuple<ValueTuple<ValueTuple<string>>>* ptrVT4 = null;
						Consume({|#29:ref ((((*ptrVT4).Item1).Item1.Item1!))|}, GetInt());
						ref var localRefString1 = ref *(string*)ptr;
						ref var localRefString2 = ref *(string?*)ptr!;
						ref var localRefString3 = ref *((string?*)ptr!);
						ref var localRefString4 = ref *ptrString!;
						ref var localRefString5 = ref *(ptrString!);
						localRefString1 = ref *(string*)ptr;
						localRefString2 = ref *(string?*)ptr!;
						localRefString3 = ref *((string?*)ptr!);
						localRefString4 = ref *ptrString!;
						localRefString5 = ref *(ptrString!);
					}
				}

				struct S
				{
					public unsafe ValueTuple<byte>* Field;
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0007").WithLocation(0).WithArguments();
			var expected1 = VerifyCS.Diagnostic("HAM0007").WithLocation(1).WithArguments();
			var expected2 = VerifyCS.Diagnostic("HAM0007").WithLocation(2).WithArguments();
			var expected3 = VerifyCS.Diagnostic("HAM0007").WithLocation(3).WithArguments();
			var expected4 = VerifyCS.Diagnostic("HAM0007").WithLocation(4).WithArguments();
			var expected5 = VerifyCS.Diagnostic("HAM0007").WithLocation(5).WithArguments();
			var expected6 = VerifyCS.Diagnostic("HAM0007").WithLocation(6).WithArguments();
			var expected7 = VerifyCS.Diagnostic("HAM0007").WithLocation(7).WithArguments();
			var expected8 = VerifyCS.Diagnostic("HAM0007").WithLocation(8).WithArguments();
			var expected9 = VerifyCS.Diagnostic("HAM0007").WithLocation(9).WithArguments();
			var expected10 = VerifyCS.Diagnostic("HAM0007").WithLocation(10).WithArguments();
			var expected11 = VerifyCS.Diagnostic("HAM0007").WithLocation(11).WithArguments();
			var expected12 = VerifyCS.Diagnostic("HAM0007").WithLocation(12).WithArguments();
			var expected13 = VerifyCS.Diagnostic("HAM0007").WithLocation(13).WithArguments();
			var expected14 = VerifyCS.Diagnostic("HAM0007").WithLocation(14).WithArguments();
			var expected15 = VerifyCS.Diagnostic("HAM0007").WithLocation(15).WithArguments();
			var expected16 = VerifyCS.Diagnostic("HAM0007").WithLocation(16).WithArguments();
			var expected17 = VerifyCS.Diagnostic("HAM0007").WithLocation(17).WithArguments();
			var expected18 = VerifyCS.Diagnostic("HAM0007").WithLocation(18).WithArguments();
			var expected19 = VerifyCS.Diagnostic("HAM0007").WithLocation(19).WithArguments();
			var expected20 = VerifyCS.Diagnostic("HAM0007").WithLocation(20).WithArguments();
			var expected21 = VerifyCS.Diagnostic("HAM0007").WithLocation(21).WithArguments();
			var expected22 = VerifyCS.Diagnostic("HAM0007").WithLocation(22).WithArguments();
			var expected23 = VerifyCS.Diagnostic("HAM0007").WithLocation(23).WithArguments();
			var expected24 = VerifyCS.Diagnostic("HAM0007").WithLocation(24).WithArguments();
			var expected25 = VerifyCS.Diagnostic("HAM0007").WithLocation(25).WithArguments();
			var expected26 = VerifyCS.Diagnostic("HAM0007").WithLocation(26).WithArguments();
			var expected27 = VerifyCS.Diagnostic("HAM0007").WithLocation(27).WithArguments();
			var expected28 = VerifyCS.Diagnostic("HAM0007").WithLocation(28).WithArguments();
			var expected29 = VerifyCS.Diagnostic("HAM0007").WithLocation(29).WithArguments();

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
