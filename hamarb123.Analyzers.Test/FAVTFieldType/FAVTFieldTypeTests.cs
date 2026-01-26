using Xunit;
using VerifyCS = hamarb123.Analyzers.Test.CSharpAnalyzerVerifier<
	hamarb123.Analyzers.FAVTFieldType.FAVTFieldTypeAnalyzer>;
using VerifyVB = hamarb123.Analyzers.Test.VisualBasicAnalyzerVerifier<
	hamarb123.Analyzers.FAVTFieldType.FAVTFieldTypeAnalyzer>;

namespace hamarb123.Analyzers.Test.FAVTFieldType
{
	public class FAVTFieldTypeTests
	{
		[Fact]
		public async Task VerifyIncorrect()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C<T1, T2>
					where T1 : class
					where T2 : Enum
				{
					public class N
					{
						[FixedAddressValueType]
						public static int {|#0:F0|};
					}

					[FixedAddressValueType]
					public static int {|#1:F1|};

					[FixedAddressValueType]
					public static string {|#2:F2|};

					[FixedAddressValueType]
					public static C<object, ConsoleColor> {|#3:F3|};

					[FixedAddressValueType]
					public static int[] {|#4:F4|};

					[FixedAddressValueType]
					public static unsafe int* {|#5:F5|};

					[FixedAddressValueType]
					public static dynamic {|#6:F6|};

					[FixedAddressValueType]
					public static unsafe delegate*<void> {|#7:F7|};

					[FixedAddressValueType]
					public static int[,] {|#8:F8|};

					[FixedAddressValueType]
					public static Action {|#9:F9|};

					[FixedAddressValueType]
					public static ConsoleColor {|#10:F10|};

					[FixedAddressValueType]
					public static IDisposable {|#11:F11|};

					[FixedAddressValueType]
					public static T1 {|#12:F12|};

					[FixedAddressValueType]
					public static T2 {|#13:F13|};

					[FixedAddressValueType]
					public static byte {|#14:F14|};

					[FixedAddressValueType]
					public static sbyte {|#15:F15|};

					[FixedAddressValueType]
					public static short {|#16:F16|};

					[FixedAddressValueType]
					public static ushort {|#17:F17|};

					[FixedAddressValueType]
					public static uint {|#18:F18|};

					[FixedAddressValueType]
					public static nint {|#19:F19|};

					[FixedAddressValueType]
					public static nuint {|#20:F20|};

					[FixedAddressValueType]
					public static long {|#21:F21|};

					[FixedAddressValueType]
					public static ulong {|#22:F22|};

					[FixedAddressValueType]
					public static float {|#23:F23|};

					[FixedAddressValueType]
					public static double {|#24:F24|};

					[FixedAddressValueType]
					public static bool {|#25:F25|};

					[FixedAddressValueType]
					public static char {|#26:F26|};

					[FixedAddressValueType]
					public static object {|#27:F27|};

					[FixedAddressValueType]
					public Guid {|#28:F28|};

					[FixedAddressValueType]
					public Array {|#29:F29|};

					[FixedAddressValueType]
					public N {|#30:F30|};
				}

				ref struct RS
				{
					[FixedAddressValueType]
					public ref Guid {|#31:F31|};

					[FixedAddressValueType]
					public unsafe fixed byte {|#32:F32|}[16];

					[FixedAddressValueType]
					public static int {|#33:F33|};
				}

				interface I
				{
					[FixedAddressValueType]
					public static int {|#34:F34|};
				}

				enum E
				{
					[FixedAddressValueType]
					{|#35:F35|},
				}

				record R
				{
					[FixedAddressValueType]
					public static int {|#36:F36|};
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0005").WithLocation(0).WithArguments("F0", "int");
			var expected1 = VerifyCS.Diagnostic("HAM0005").WithLocation(1).WithArguments("F1", "int");
			var expected2 = VerifyCS.Diagnostic("HAM0005").WithLocation(2).WithArguments("F2", "string");
			var expected3 = VerifyCS.Diagnostic("HAM0005").WithLocation(3).WithArguments("F3", "C<object, System.ConsoleColor>");
			var expected4 = VerifyCS.Diagnostic("HAM0005").WithLocation(4).WithArguments("F4", "int[]");
			var expected5 = VerifyCS.Diagnostic("HAM0005").WithLocation(5).WithArguments("F5", "int*");
			var expected6 = VerifyCS.Diagnostic("HAM0005").WithLocation(6).WithArguments("F6", "dynamic");
			var expected7 = VerifyCS.Diagnostic("HAM0005").WithLocation(7).WithArguments("F7", "delegate*<void>");
			var expected8 = VerifyCS.Diagnostic("HAM0005").WithLocation(8).WithArguments("F8", "int[*,*]");
			var expected9 = VerifyCS.Diagnostic("HAM0005").WithLocation(9).WithArguments("F9", "System.Action");
			var expected10 = VerifyCS.Diagnostic("HAM0005").WithLocation(10).WithArguments("F10", "System.ConsoleColor");
			var expected11 = VerifyCS.Diagnostic("HAM0005").WithLocation(11).WithArguments("F11", "System.IDisposable");
			var expected12 = VerifyCS.Diagnostic("HAM0005").WithLocation(12).WithArguments("F12", "T1");
			var expected13 = VerifyCS.Diagnostic("HAM0005").WithLocation(13).WithArguments("F13", "T2");
			var expected14 = VerifyCS.Diagnostic("HAM0005").WithLocation(14).WithArguments("F14", "byte");
			var expected15 = VerifyCS.Diagnostic("HAM0005").WithLocation(15).WithArguments("F15", "sbyte");
			var expected16 = VerifyCS.Diagnostic("HAM0005").WithLocation(16).WithArguments("F16", "short");
			var expected17 = VerifyCS.Diagnostic("HAM0005").WithLocation(17).WithArguments("F17", "ushort");
			var expected18 = VerifyCS.Diagnostic("HAM0005").WithLocation(18).WithArguments("F18", "uint");
			var expected19 = VerifyCS.Diagnostic("HAM0005").WithLocation(19).WithArguments("F19", "nint");
			var expected20 = VerifyCS.Diagnostic("HAM0005").WithLocation(20).WithArguments("F20", "nuint");
			var expected21 = VerifyCS.Diagnostic("HAM0005").WithLocation(21).WithArguments("F21", "long");
			var expected22 = VerifyCS.Diagnostic("HAM0005").WithLocation(22).WithArguments("F22", "ulong");
			var expected23 = VerifyCS.Diagnostic("HAM0005").WithLocation(23).WithArguments("F23", "float");
			var expected24 = VerifyCS.Diagnostic("HAM0005").WithLocation(24).WithArguments("F24", "double");
			var expected25 = VerifyCS.Diagnostic("HAM0005").WithLocation(25).WithArguments("F25", "bool");
			var expected26 = VerifyCS.Diagnostic("HAM0005").WithLocation(26).WithArguments("F26", "char");
			var expected27 = VerifyCS.Diagnostic("HAM0005").WithLocation(27).WithArguments("F27", "object");
			var expected28 = VerifyCS.Diagnostic("HAM0005").WithLocation(28).WithArguments("F28", "System.Guid");
			var expected29 = VerifyCS.Diagnostic("HAM0005").WithLocation(29).WithArguments("F29", "System.Array");
			var expected30 = VerifyCS.Diagnostic("HAM0005").WithLocation(30).WithArguments("F30", "C<T1, T2>.N");
			var expected31 = VerifyCS.Diagnostic("HAM0005").WithLocation(31).WithArguments("F31", "System.Guid");
			var expected32 = VerifyCS.Diagnostic("HAM0005").WithLocation(32).WithArguments("F32", "byte*");
			var expected33 = VerifyCS.Diagnostic("HAM0005").WithLocation(33).WithArguments("F33", "int");
			var expected34 = VerifyCS.Diagnostic("HAM0005").WithLocation(34).WithArguments("F34", "int");
			var expected35 = VerifyCS.Diagnostic("HAM0005").WithLocation(35).WithArguments("F35", "E");
			var expected36 = VerifyCS.Diagnostic("HAM0005").WithLocation(36).WithArguments("F36", "int");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14, expected15,
				expected16, expected17, expected18, expected19,
				expected20, expected21, expected22, expected23,
				expected24, expected25, expected26, expected27,
				expected28, expected29, expected30, expected31,
				expected32, expected33, expected34, expected35,
				expected36);
		}

		[Fact]
		public async Task VerifyPotentiallyIncorrect()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C<T1, T2>
					where T2 : struct
				{
					[FixedAddressValueType]
					public static T1 {|#0:F0|};

					[FixedAddressValueType]
					public static T2 {|#1:F1|};
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0006").WithLocation(0).WithArguments("F0", "T1");
			var expected1 = VerifyCS.Diagnostic("HAM0006").WithLocation(1).WithArguments("F1", "T2");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1);
		}

		[Fact]
		public async Task VerifyCorrect()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C<T>
				{
					[FixedAddressValueType]
					public static ValueTuple F0;

					[FixedAddressValueType]
					public static ValueTuple<int> F1;

					[FixedAddressValueType]
					public static (int, long) F2;

					[FixedAddressValueType]
					public static (int, object) F3;

					[FixedAddressValueType]
					public static ValueTuple<T> F4;

					[FixedAddressValueType]
					public const decimal F5 = 1.0m;
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		[Fact]
		public async Task VerifyProperty()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public class C
				{
					[field: FixedAddressValueType]
					public (int, long) {|#0:P0|} { get; set; }

					[field: FixedAddressValueType]
					public static object {|#1:P1|} { get; set; }

					[field: FixedAddressValueType]
					public static object {|#2:P2|} { get => field; set; }

					[field: FixedAddressValueType]
					public static object {|#3:P3|} { get; set => field = value; }

					[field: FixedAddressValueType]
					public static object {|#4:P4|} { get => field; set => field = value; }

					[field: FixedAddressValueType]
					public static object {|#5:P5|} => field;

					[field: FixedAddressValueType]
					public static (int, long) P6 { get; set; }
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0005").WithLocation(0).WithArguments("P0", "(int, long)");
			var expected1 = VerifyCS.Diagnostic("HAM0005").WithLocation(1).WithArguments("P1", "object");
			var expected2 = VerifyCS.Diagnostic("HAM0005").WithLocation(2).WithArguments("P2", "object");
			var expected3 = VerifyCS.Diagnostic("HAM0005").WithLocation(3).WithArguments("P3", "object");
			var expected4 = VerifyCS.Diagnostic("HAM0005").WithLocation(4).WithArguments("P4", "object");
			var expected5 = VerifyCS.Diagnostic("HAM0005").WithLocation(5).WithArguments("P5", "object");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5);
		}

		[Fact]
		public async Task VerifyVisualBasic()
		{
			const string source = """
				Imports System
				Imports System.Runtime.CompilerServices

				Public Class C(Of T)
					<FixedAddressValueType>
					Public Shared {|#0:F0|} As Integer
					<FixedAddressValueType>
					Public Shared {|#1:F1|} As Object
					<FixedAddressValueType>
					Public Shared {|#2:F2|} As T
					Public Shared F3 As ValueTuple(Of Integer)
				End Class

				Public Module M
					Public F4 As ValueTuple(Of Integer)
				End Module
				""";

			var expected0 = VerifyVB.Diagnostic("HAM0005").WithLocation(0).WithArguments("F0", "Integer");
			var expected1 = VerifyVB.Diagnostic("HAM0005").WithLocation(1).WithArguments("F1", "Object");
			var expected2 = VerifyVB.Diagnostic("HAM0006").WithLocation(2).WithArguments("F2", "T");

			await VerifyVB.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2);
		}
	}
}
