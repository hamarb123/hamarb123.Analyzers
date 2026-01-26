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
					public void Consume(ref byte b, int i) { }
					public void Consume(ref byte b1, ref byte b2) { }
					public int GetInt() => 0;

					public unsafe void M()
					{
						byte* ptr = null;
						ValueTuple<byte>* ptrVT = null;
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

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15);
		}
	}
}
