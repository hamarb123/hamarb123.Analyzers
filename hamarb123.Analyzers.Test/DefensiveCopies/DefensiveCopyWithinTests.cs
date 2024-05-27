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
	public class DefensiveCopyWithinTests
	{
		[Fact]
		public async Task VerifyThroughField()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public struct C
				{
					public S1 Value;
					public void M1()
					{
						//No defensive copies
						this.Value.rM();
						Value.rM();

						_ = Value.rP;
						Value.rP = 0;

						Value.F = 0;

						_ = Value.P_RO_R;
						_ = ref Value.P_RO_R;
						Value.P_RO_R = 0;

						_ = Value.P_RO_R_RO;
						_ = ref Value.P_RO_R_RO;

						_ = Value[(long)0];
						Value[(long)0] = 0;
					}
					public void M2()
					{
						//No defensive copies
						this.Value.M();
						Value.M();

						_ = Value.P;
						Value.P = 0;

						_ = Value.P_R;
						_ = ref Value.P_R;
						Value.P_R = 0;

						_ = Value.P_R_RO;
						_ = ref Value.P_R_RO;

						_ = Value[(uint)0];
						Value[(uint)0] = 0;
					}
					public readonly void rM1()
					{
						//No defensive copies
						this.Value.rM();
						Value.rM();

						_ = Value.rP;
						Value.rP = 0;

						_ = Value.rF;
						_ = Value.F;

						_ = ref Value.rF;
						_ = ref Value.F;

						_ = Value.P_RO_R;
						_ = ref Value.P_RO_R;
						Value.P_RO_R = 0;

						_ = Value.P_RO_R_RO;
						_ = ref Value.P_RO_R_RO;

						_ = Value[(long)0];
						Value[(long)0] = 0;
					}
					public readonly void rM2()
					{
						//All defensive copies
						{|#0:this.Value.M()|};
						{|#1:Value.M()|};

						_ = {|#2:Value.P|};

						_ = {|#3:Value.P_R|};
						_ = ref {|#4:Value.P_R|};
						{|#5:Value.P_R|} = 0;

						_ = {|#6:Value.P_R_RO|};
						_ = ref {|#7:Value.P_R_RO|};

						_ = {|#8:Value[(uint)0]|};
					}
				}

				public struct S1
				{
					public readonly void rM() { }
					public void M() { }
					public readonly int rP { get => 0; set { } }
					public int P { get => 0; set { } }
					public readonly int rF;
					public int F;
					public ref int P_R => ref Unsafe.NullRef<int>();
					public ref readonly int P_R_RO => ref Unsafe.NullRef<int>();
					public readonly ref int P_RO_R => ref Unsafe.NullRef<int>();
					public readonly ref readonly int P_RO_R_RO => ref Unsafe.NullRef<int>();
					public event Action E { add { } remove { } }
					public readonly event Action rE { add { } remove { } }
					public int this[uint i] { get => 0; set { } }
					public readonly int this[long i] { get => 0; set { } }
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M", "Value");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M", "Value");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("P", "Value");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("P_R", "Value");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("P_R", "Value");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("P_R", "Value");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("P_R_RO", "Value");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("P_R_RO", "Value");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("this[]", "Value");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8);
		}

		[Fact]
		public async Task VerifyThroughSelf()
		{
			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public struct C
				{
					public void M1()
					{
						//No defensive copies
						rM();
						this.rM();

						_ = rP;
						rP = 0;
						_ = this.rP;
						this.rP = 0;

						F = 0;
						this.F = 0;

						_ = P_RO_R;
						_ = ref P_RO_R;
						P_RO_R = 0;
						_ = this.P_RO_R;
						_ = ref this.P_RO_R;
						this.P_RO_R = 0;

						_ = P_RO_R_RO;
						_ = ref P_RO_R_RO;
						_ = this.P_RO_R_RO;
						_ = ref this.P_RO_R_RO;

						_ = this[(long)0];
						this[(long)0] = 0;
					}
					public void M2()
					{
						//No defensive copies
						M();
						this.M();

						_ = P;
						P = 0;
						_ = this.P;
						this.P = 0;

						_ = P_R;
						_ = ref P_R;
						P_R = 0;
						_ = this.P_R;
						_ = ref this.P_R;
						this.P_R = 0;

						_ = P_R_RO;
						_ = ref P_R_RO;
						_ = this.P_R_RO;
						_ = ref this.P_R_RO;

						_ = this[(uint)0];
						this[(uint)0] = 0;
					}
					public readonly void rM1()
					{
						//No defensive copies
						rM();
						this.rM();

						_ = rP;
						rP = 0;
						_ = this.rP;
						this.rP = 0;

						_ = rF;
						_ = F;
						_ = this.rF;
						_ = this.F;

						_ = ref rF;
						_ = ref F;
						_ = ref this.rF;
						_ = ref this.F;

						_ = P_RO_R;
						_ = ref P_RO_R;
						P_RO_R = 0;
						_ = this.P_RO_R;
						_ = ref this.P_RO_R;
						this.P_RO_R = 0;

						_ = P_RO_R_RO;
						_ = ref P_RO_R_RO;

						_ = this.P_RO_R_RO;
						_ = ref this.P_RO_R_RO;

						_ = this[(long)0];
						this[(long)0] = 0;
					}
					public readonly void rM2()
					{
						//All defensive copies
						{|#0:M()|};
						{|#1:this.M()|};

						_ = {|#2:P|};
						_ = {|#3:this.P|};

						_ = {|#4:P_R|};
						_ = ref {|#5:P_R|};
						{|#6:P_R|} = 0;
						_ = {|#7:this.P_R|};
						_ = ref {|#8:this.P_R|};
						{|#9:this.P_R|} = 0;

						_ = {|#10:P_R_RO|};
						_ = ref {|#11:P_R_RO|};
						_ = {|#12:this.P_R_RO|};
						_ = ref {|#13:this.P_R_RO|};

						_ = {|#14:this[(uint)0]|};
					}

					public readonly void rM() { }
					public void M() { }
					public readonly int rP { get => 0; set { } }
					public int P { get => 0; set { } }
					public readonly int rF;
					public int F;
					public ref int P_R => ref Unsafe.NullRef<int>();
					public ref readonly int P_R_RO => ref Unsafe.NullRef<int>();
					public readonly ref int P_RO_R => ref Unsafe.NullRef<int>();
					public readonly ref readonly int P_RO_R_RO => ref Unsafe.NullRef<int>();
					public event Action E { add { } remove { } }
					public readonly event Action rE { add { } remove { } }
					public int this[uint i] { get => 0; set { } }
					public readonly int this[long i] { get => 0; set { } }
				}
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M", "this");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M", "this");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("P", "this");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("P", "this");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("P_R", "this");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("P_R", "this");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("P_R", "this");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("P_R", "this");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("P_R", "this");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("P_R", "this");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("P_R_RO", "this");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("P_R_RO", "this");
			var expected12 = VerifyCS.Diagnostic("HAM0001").WithLocation(12).WithArguments("P_R_RO", "this");
			var expected13 = VerifyCS.Diagnostic("HAM0001").WithLocation(13).WithArguments("P_R_RO", "this");
			var expected14 = VerifyCS.Diagnostic("HAM0001").WithLocation(14).WithArguments("this[]", "this");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11,
				expected12, expected13, expected14);
		}

		[Fact]
		public async Task VerifyPossibleFalsePositives()
		{
			//this could probably do with some more tests

			const string source = """
				using System;
				using System.Runtime.CompilerServices;

				public struct C
				{
					public readonly void M1(ref C value1, C value2, in C value3)
					{
						//No defensive copies
						rM();
						this.rM();

						value1.rM();
						value1.M();
						value2.rM();
						value2.M();
						value3.rM();
					}

					public readonly void rM() { }
					public void M() { }
				}
				""";

			await VerifyCS.VerifyAnalyzerAsync(source);
		}

		//CS8656
		[Fact]
		public async Task VerifyWithinAccessors()
		{
			const string source = """
				#pragma warning disable CS8656 // Call to non-readonly member from a 'readonly' member results in an implicit copy.
				using System;

				public struct C
				{
					//No defensive copies
					public int P1 => M2();
					public int P2
					{
						get => M2();
						set => M1();
					}
					public event Action E1
					{
						add => M1();
						remove => M1();
					}
					public int this[object o0] => M2();
					public int this[int i0]
					{
						get => M2();
						set => M1();
					}

					//All defensive copies
					public readonly int rP1 => {|#0:M2()|};
					public readonly int rP2
					{
						get => {|#1:M2()|};
						set => {|#2:M1()|};
					}
					public int rP2_
					{
						readonly get => {|#3:M2()|};
						set => M1();
					}
					public int rP2__
					{
						get => M2();
						readonly set => {|#4:M1()|};
					}
					public readonly event Action rE1
					{
						add => {|#5:M1()|};
						remove => {|#6:M1()|};
					}
					public readonly int this[object o0, object o1] => {|#7:M2()|};
					public readonly int this[int i0, int i1]
					{
						get => {|#8:M2()|};
						set => {|#9:M1()|};
					}
					public int this[int i0, int i1, int i2]
					{
						readonly get => {|#10:M2()|};
						set => M1();
					}
					public int this[int i0, int i1, int i2, int i3]
					{
						get => M2();
						readonly set => {|#11:M1()|};
					}

					//Helper methods
					public void M1() { }
					public int M2() => 0;
				}
				#pragma warning restore CS8656 // Call to non-readonly member from a 'readonly' member results in an implicit copy.
				""";

			var expected0 = VerifyCS.Diagnostic("HAM0001").WithLocation(0).WithArguments("M2", "this");
			var expected1 = VerifyCS.Diagnostic("HAM0001").WithLocation(1).WithArguments("M2", "this");
			var expected2 = VerifyCS.Diagnostic("HAM0001").WithLocation(2).WithArguments("M1", "this");
			var expected3 = VerifyCS.Diagnostic("HAM0001").WithLocation(3).WithArguments("M2", "this");
			var expected4 = VerifyCS.Diagnostic("HAM0001").WithLocation(4).WithArguments("M1", "this");
			var expected5 = VerifyCS.Diagnostic("HAM0001").WithLocation(5).WithArguments("M1", "this");
			var expected6 = VerifyCS.Diagnostic("HAM0001").WithLocation(6).WithArguments("M1", "this");
			var expected7 = VerifyCS.Diagnostic("HAM0001").WithLocation(7).WithArguments("M2", "this");
			var expected8 = VerifyCS.Diagnostic("HAM0001").WithLocation(8).WithArguments("M2", "this");
			var expected9 = VerifyCS.Diagnostic("HAM0001").WithLocation(9).WithArguments("M1", "this");
			var expected10 = VerifyCS.Diagnostic("HAM0001").WithLocation(10).WithArguments("M2", "this");
			var expected11 = VerifyCS.Diagnostic("HAM0001").WithLocation(11).WithArguments("M1", "this");

			await VerifyCS.VerifyAnalyzerAsync(source,
				expected0, expected1, expected2, expected3,
				expected4, expected5, expected6, expected7,
				expected8, expected9, expected10, expected11);
		}
	}
}
