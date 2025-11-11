using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace hamarb123.Analyzers
{
	// Header from original file/s:
	// Licensed to the .NET Foundation under one or more agreements.
	// The .NET Foundation licenses this file to you under the MIT license.
	// See the LICENSE file in the project root for more information.

	/*
		License from original project:

		The MIT License (MIT)

		Copyright (c) .NET Foundation and Contributors

		All rights reserved.

		Permission is hereby granted, free of charge, to any person obtaining a copy
		of this software and associated documentation files (the "Software"), to deal
		in the Software without restriction, including without limitation the rights
		to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
		copies of the Software, and to permit persons to whom the Software is
		furnished to do so, subject to the following conditions:

		The above copyright notice and this permission notice shall be included in all
		copies or substantial portions of the Software.

		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
		IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
		FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
			AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
			LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
			OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
			SOFTWARE.
	*/

	//https://github.com/dotnet/roslyn/issues/25057


	//https://github.com/dotnet/roslyn/blob/d5cd11097e5f9e3d0f5799fb1774e93fc6b31b5b/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/OperationExtensions.cs

	internal static class OperationExtensions
	{
		/// <summary>
		/// Returns the <see cref="ValueUsageInfo"/> for the given operation.
		/// This extension can be removed once https://github.com/dotnet/roslyn/issues/25057 is implemented.
		/// </summary>
		/// <remarks>
		/// When referring to a variable, this method should only return a 'write' result if the variable is entirely
		/// overwritten.  Not if the variable is written <em>through</em>.  For example, a write to a property on a struct
		/// variable is not a write to the struct variable (though at runtime it might impact the value in some fashion).
		/// <para/> Put another way, this only returns 'write' when certain that the entire value <em>is</em> absolutely
		/// entirely overwritten.
		/// </remarks>
		public static ValueUsageInfo GetValueUsageInfo(this IOperation operation, ISymbol containingSymbol)
		{
			/*
			|    code                  | Read | Write | ReadableRef | WritableRef | NonReadWriteRef |
			| x.Prop = 1               |      |  ✔️   |             |             |                 |
			| x.Prop += 1              |  ✔️  |  ✔️   |             |             |                 |
			| x.Prop++                 |  ✔️  |  ✔️   |             |             |                 |
			| Foo(x.Prop)              |  ✔️  |       |             |             |                 |
			| Foo(x.Prop),             |      |       |     ✔️      |             |                 |
			where void Foo(in T v)
			| Foo(out x.Prop)          |      |       |             |     ✔️      |                 |
			| Foo(ref x.Prop)          |      |       |     ✔️      |     ✔️      |                 |
			| nameof(x)                |      |       |             |             |       ✔️        | ️
			| sizeof(x)                |      |       |             |             |       ✔️        | ️
			| typeof(x)                |      |       |             |             |       ✔️        | ️
			| out var x                |      |  ✔️   |             |             |                 | ️
			| case X x:                |      |  ✔️   |             |             |                 | ️
			| obj is X x               |      |  ✔️   |             |             |                 |
			| obj is { } x             |      |  ✔️   |             |             |                 |
			| obj is [] x              |      |  ✔️   |             |             |                 |
			| ref var x =              |      |       |     ✔️      |     ✔️      |                 |
			| ref readonly var x =     |      |       |     ✔️      |             |                 |

			*/
			// Workaround for https://github.com/dotnet/roslyn/issues/30753
			if (operation is ILocalReferenceOperation { IsDeclaration: true, IsImplicit: false })
			{
				// Declaration expression is a definition (write) for the declared local.
				return ValueUsageInfo.Write;
			}
			else if (operation is IDeclarationPatternOperation)
			{
				while (operation.Parent is IBinaryPatternOperation or INegatedPatternOperation or IRelationalPatternOperation)
					operation = operation.Parent;

				switch (operation.Parent)
				{
					case IPatternCaseClauseOperation:
						// A declaration pattern within a pattern case clause is a
						// write for the declared local.
						// For example, 'x' is defined and assigned the value from 'obj' below:
						//      switch (obj)
						//      {
						//          case X x:
						//
						return ValueUsageInfo.Write;

					case IRecursivePatternOperation:
						// A declaration pattern within a recursive pattern is a
						// write for the declared local.
						// For example, 'x' is defined and assigned the value from 'obj' below:
						//      (obj) switch
						//      {
						//          (X x) => ...
						//      };
						//
						return ValueUsageInfo.Write;

					case ISwitchExpressionArmOperation:
						// A declaration pattern within a switch expression arm is a
						// write for the declared local.
						// For example, 'x' is defined and assigned the value from 'obj' below:
						//      obj switch
						//      {
						//          X x => ...
						//
						return ValueUsageInfo.Write;

					case IIsPatternOperation:
						// A declaration pattern within an is pattern is a
						// write for the declared local.
						// For example, 'x' is defined and assigned the value from 'obj' below:
						//      if (obj is X x)
						//
						return ValueUsageInfo.Write;

					case IPropertySubpatternOperation:
						// A declaration pattern within a property sub-pattern is a
						// write for the declared local.
						// For example, 'x' is defined and assigned the value from 'obj.Property' below:
						//      if (obj is { Property : int x })
						//
						return ValueUsageInfo.Write;

					default:
						Debug.Fail("Unhandled declaration pattern context");

						// Conservatively assume read/write.
						return ValueUsageInfo.ReadWrite;
				}
			}
			else if (operation is IRecursivePatternOperation or IListPatternOperation)
			{
				return ValueUsageInfo.Write;
			}

			if (operation.Parent is IAssignmentOperation assignmentOperation &&
				assignmentOperation.Target == operation)
			{
				return operation.Parent.IsAnyCompoundAssignment()
					? ValueUsageInfo.ReadWrite
					: ValueUsageInfo.Write;
			}
			else if (operation.Parent is ISimpleAssignmentOperation { IsRef: true } simpleAssignmentOperation &&
				simpleAssignmentOperation.Value == operation)
			{
				return ValueUsageInfo.ReadableWritableReference;
			}
			else if (operation.Parent is IIncrementOrDecrementOperation)
			{
				return ValueUsageInfo.ReadWrite;
			}
			else if (operation.Parent is IForToLoopOperation forToLoopOperation && forToLoopOperation.LoopControlVariable.Equals(operation))
			{
				return ValueUsageInfo.ReadWrite;
			}
			else if (operation.Parent is IParenthesizedOperation parenthesizedOperation)
			{
				// Note: IParenthesizedOperation is specific to VB, where the parens cause a copy, so this cannot be classified as a write.
				Debug.Assert(parenthesizedOperation.Language == LanguageNames.VisualBasic);

				return parenthesizedOperation.GetValueUsageInfo(containingSymbol) &
					~(ValueUsageInfo.Write | ValueUsageInfo.Reference);
			}
			else if (operation.Parent is INameOfOperation or ITypeOfOperation or ISizeOfOperation)
			{
				return ValueUsageInfo.Name;
			}
			else if (operation.Parent is IArgumentOperation argumentOperation)
			{
				switch (argumentOperation.Parameter?.RefKind)
				{
					case RefKind.RefReadOnly:
						return ValueUsageInfo.ReadableReference;

					case RefKind.Out:
						return ValueUsageInfo.WritableReference;

					case RefKind.Ref:
						return ValueUsageInfo.ReadableWritableReference;

					default:
						return ValueUsageInfo.Read;
				}
			}
			else if (operation.Parent is IReturnOperation returnOperation)
			{
				return returnOperation.GetRefKind(containingSymbol) switch
				{
					RefKind.RefReadOnly => ValueUsageInfo.ReadableReference,
					RefKind.Ref => ValueUsageInfo.ReadableWritableReference,
					_ => ValueUsageInfo.Read,
				};
			}
			else if (operation.Parent is IConditionalOperation conditionalOperation)
			{
				if (operation == conditionalOperation.WhenTrue
					|| operation == conditionalOperation.WhenFalse)
				{
					return GetValueUsageInfo(conditionalOperation, containingSymbol);
				}
				else
				{
					return ValueUsageInfo.Read;
				}
			}
			else if (operation.Parent is IReDimClauseOperation reDimClauseOperation &&
				reDimClauseOperation.Operand == operation)
			{
				return reDimClauseOperation.Parent is IReDimOperation { Preserve: true }
					? ValueUsageInfo.ReadWrite
					: ValueUsageInfo.Write;
			}
			else if (operation.Parent is IDeclarationExpressionOperation declarationExpression)
			{
				return declarationExpression.GetValueUsageInfo(containingSymbol);
			}
			else if (operation.IsInLeftOfDeconstructionAssignment(out _))
			{
				return ValueUsageInfo.Write;
			}
			else if (operation.Parent is IVariableInitializerOperation variableInitializerOperation)
			{
				if (variableInitializerOperation.Parent is IVariableDeclaratorOperation variableDeclaratorOperation)
				{
					switch (variableDeclaratorOperation.Symbol.RefKind)
					{
						case RefKind.Ref:
							return ValueUsageInfo.ReadableWritableReference;

						case RefKind.RefReadOnly:
							return ValueUsageInfo.ReadableReference;
					}
				}
			}

			return ValueUsageInfo.Read;
		}

		private static RefKind GetRefKind(this IReturnOperation? operation, ISymbol containingSymbol)
		{
			var containingMethod = TryGetContainingAnonymousFunctionOrLocalFunction(operation) ?? (containingSymbol as IMethodSymbol);
			return containingMethod?.RefKind ?? RefKind.None;
		}

		private static bool IsInLeftOfDeconstructionAssignment(this IOperation operation, /*[NotNullWhen(true)]*/ out IDeconstructionAssignmentOperation? deconstructionAssignment)
		{
			deconstructionAssignment = null;

			var previousOperation = operation;
			var current = operation.Parent;

			while (current != null)
			{
				switch (current.Kind)
				{
					case OperationKind.DeconstructionAssignment:
						deconstructionAssignment = (IDeconstructionAssignmentOperation)current;
						return deconstructionAssignment.Target == previousOperation;

					case OperationKind.Tuple:
					case OperationKind.Conversion:
					case OperationKind.Parenthesized:
						previousOperation = current;
						current = current.Parent;
						continue;

					default:
						return false;
				}
			}

			return false;
		}

		private static IMethodSymbol? TryGetContainingAnonymousFunctionOrLocalFunction(this IOperation? operation)
		{
			operation = operation?.Parent;
			while (operation != null)
			{
				switch (operation.Kind)
				{
					case OperationKind.AnonymousFunction:
						return ((IAnonymousFunctionOperation)operation).Symbol;

					case OperationKind.LocalFunction:
						return ((ILocalFunctionOperation)operation).Symbol;
				}

				operation = operation.Parent;
			}

			return null;
		}

		/// <summary>
		/// Returns true if the given operation is a regular compound assignment,
		/// i.e. <see cref="ICompoundAssignmentOperation"/> such as <code>a += b</code>,
		/// or a special null coalescing compound assignment, i.e. <see cref="ICoalesceAssignmentOperation"/>
		/// such as <code>a ??= b</code>.
		/// </summary>
		public static bool IsAnyCompoundAssignment(this IOperation operation)
		{
			switch (operation)
			{
				case ICompoundAssignmentOperation:
				case ICoalesceAssignmentOperation:
					return true;

				default:
					return false;
			}
		}
	}


	//https://github.com/dotnet/roslyn/blob/ec6c8b40e23a595b1e75aec920e3fb29e55f61cf/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ValueUsageInfo.cs

	[Flags]
	internal enum ValueUsageInfo
	{
		/// <summary>
		/// Represents default value indicating no usage.
		/// </summary>
		None = 0x0,

		/// <summary>
		/// Represents a value read.
		/// For example, reading the value of a local/field/parameter.
		/// </summary>
		Read = 0x1,

		/// <summary>
		/// Represents a value write.
		/// For example, assigning a value to a local/field/parameter.
		/// </summary>
		Write = 0x2,

		/// <summary>
		/// Represents a reference being taken for the symbol.
		/// For example, passing an argument to an "in", "ref" or "out" parameter.
		/// </summary>
		Reference = 0x4,

		/// <summary>
		/// Represents a name-only reference that neither reads nor writes the underlying value.
		/// For example, 'nameof(x)' or reference to a symbol 'x' in a documentation comment
		/// does not read or write the underlying value stored in 'x'.
		/// </summary>
		Name = 0x8,

		/// <summary>
		/// Represents a value read and/or write.
		/// For example, an increment or compound assignment operation.
		/// </summary>
		ReadWrite = Read | Write,

		/// <summary>
		/// Represents a readable reference being taken to the value.
		/// For example, passing an argument to an "in" or "ref readonly" parameter.
		/// </summary>
		ReadableReference = Read | Reference,

		/// <summary>
		/// Represents a readable reference being taken to the value.
		/// For example, passing an argument to an "out" parameter.
		/// </summary>
		WritableReference = Write | Reference,

		/// <summary>
		/// Represents a value read or write.
		/// For example, passing an argument to a "ref" parameter.
		/// </summary>
		ReadableWritableReference = Read | Write | Reference
	}

	internal static class ValueUsageInfoExtensions
	{
		public static bool IsReadFrom(this ValueUsageInfo valueUsageInfo)
			=> (valueUsageInfo & ValueUsageInfo.Read) != 0;

		public static bool IsWrittenTo(this ValueUsageInfo valueUsageInfo)
			=> (valueUsageInfo & ValueUsageInfo.Write) != 0;

		public static bool IsNameOnly(this ValueUsageInfo valueUsageInfo)
			=> (valueUsageInfo & ValueUsageInfo.Name) != 0;

		public static bool IsReference(this ValueUsageInfo valueUsageInfo)
			=> (valueUsageInfo & ValueUsageInfo.Reference) != 0;
	}
}
