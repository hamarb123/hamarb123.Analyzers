using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hamarb123.Analyzers.DefensiveCopies;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = hamarb123.Analyzers.Test.CSharpAnalyzerVerifier<
	hamarb123.Analyzers.DefensiveCopies.DefensiveCopyAnalyzer>;

namespace hamarb123.Analyzers.Test.DefensiveCopies
{
	public class DefensiveCopyPrimitivesTests
	{
		const string PrimitiveDefensiveCopySourceCode = """
			using System;

			public unsafe class C
			{
				public void M1
				(
					ref bool value0, ref char value1,
					ref sbyte value2, ref byte value3,
					ref short value4, ref ushort value5,
					ref int value6, ref uint value7,
					ref long value8, ref ulong value9,
					ref float value10, ref double value11,
					ref IntPtr value12, ref UIntPtr value13,
					ref decimal value14, ref DateTime value15,
					ref nint value16, ref nuint value17,
					ref E1 value18, ref S1 value19,
					ref int? value20
				)
				{
					//No defensive copies
					value0.ToString();
					value1.ToString();
					value2.ToString();
					value3.ToString();
					value4.ToString();
					value5.ToString();
					value6.ToString();
					value7.ToString();
					value8.ToString();
					value9.ToString();
					value10.ToString();
					value11.ToString();
					value12.ToString();
					value13.ToString();
					value14.ToString();
					value15.ToString();
					value16.ToString();
					value17.ToString();
					value18.ToString();
					value19.ToString();
					_ = value20.HasValue;
					_ = value20.Value;
					value20.GetValueOrDefault();
					value20.GetValueOrDefault(0);
					value20.Equals(null);
					value20.GetHashCode();
					value20.ToString();
				}

				public void M2
				(
					in bool value0, in char value1,
					in sbyte value2, in byte value3,
					in short value4, in ushort value5,
					in int value6, in uint value7,
					in long value8, in ulong value9,
					in float value10, in double value11,
					in IntPtr value12, in UIntPtr value13,
					in decimal value14, in DateTime value15,
					in nint value16, in nuint value17,
					in E1 value18, in S1 value19,
					in int? value20
				)
				{
					//Defensive copy emitted on platforms (0-17) where it's not marked as readonly, even though it's a primitive.
					//Defensive copy also emitted on 18 always
					//value19 is always a real defensive copy, and value20 has real defensive copies for .Equals, .GetHashCode, and .ToString
					{|#0:value0.ToString()|};
					{|#1:value1.ToString()|};
					{|#2:value2.ToString()|};
					{|#3:value3.ToString()|};
					{|#4:value4.ToString()|};
					{|#5:value5.ToString()|};
					{|#6:value6.ToString()|};
					{|#7:value7.ToString()|};
					{|#8:value8.ToString()|};
					{|#9:value9.ToString()|};
					{|#10:value10.ToString()|};
					{|#11:value11.ToString()|};
					{|#12:value12.ToString()|};
					{|#13:value13.ToString()|};
					{|#14:value14.ToString()|};
					{|#15:value15.ToString()|};
					{|#16:value16.ToString()|};
					{|#17:value17.ToString()|};
					{|#18:value18.ToString()|};
					{|#19:value19.ToString()|};
					_ = {|#20:value20.HasValue|};
					_ = {|#21:value20.Value|};
					{|#22:value20.GetValueOrDefault()|};
					{|#23:value20.GetValueOrDefault(0)|};
					{|#24:value20.Equals(null)|};
					{|#25:value20.GetHashCode()|};
					{|#26:value20.ToString()|};
				}
			}

			public struct S1
			{
				public override string ToString() => "";
			}

			public enum E1
			{
			}
			""";

		private static readonly DiagnosticResult[] PrimitiveDefensiveCopyDiagnostics =
		[
			VerifyCS.Diagnostic("HAM0003").WithLocation(0).WithArguments("ToString", "value0"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(1).WithArguments("ToString", "value1"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(2).WithArguments("ToString", "value2"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(3).WithArguments("ToString", "value3"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(4).WithArguments("ToString", "value4"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(5).WithArguments("ToString", "value5"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(6).WithArguments("ToString", "value6"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(7).WithArguments("ToString", "value7"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(8).WithArguments("ToString", "value8"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(9).WithArguments("ToString", "value9"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(10).WithArguments("ToString", "value10"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(11).WithArguments("ToString", "value11"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(12).WithArguments("ToString", "value12"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(13).WithArguments("ToString", "value13"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(14).WithArguments("ToString", "value14"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(15).WithArguments("ToString", "value15"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(16).WithArguments("ToString", "value16"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(17).WithArguments("ToString", "value17"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(18).WithArguments("ToString", "value18"),
			VerifyCS.Diagnostic("HAM0001").WithLocation(19).WithArguments("ToString", "value19"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(20).WithArguments("HasValue", "value20"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(21).WithArguments("Value", "value20"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(22).WithArguments("GetValueOrDefault", "value20"),
			VerifyCS.Diagnostic("HAM0003").WithLocation(23).WithArguments("GetValueOrDefault", "value20"),
			VerifyCS.Diagnostic("HAM0001").WithLocation(24).WithArguments("Equals", "value20"),
			VerifyCS.Diagnostic("HAM0001").WithLocation(25).WithArguments("GetHashCode", "value20"),
			VerifyCS.Diagnostic("HAM0001").WithLocation(26).WithArguments("ToString", "value20"),
		];

		[Fact]
		public async Task VerifyPrimitiveDefensiveCopy()
		{
			await VerifyCS.VerifyAnalyzerAsync(PrimitiveDefensiveCopySourceCode,
				[.. PrimitiveDefensiveCopyDiagnostics[18..20], .. PrimitiveDefensiveCopyDiagnostics[24..]]);
		}

		[Fact]
		public async Task VerifyPrimitiveDefensiveCopyNetFramework()
		{
			await VerifyCS.VerifyAnalyzerAsync(PrimitiveDefensiveCopySourceCode, new VerifyCS.Options() { ReferenceAssemblies = ReferenceAssemblies.NetFramework.Net48.Default },
					PrimitiveDefensiveCopyDiagnostics);
		}
	}
}
