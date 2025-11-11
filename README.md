# hamarb123.Analyzers
A bunch of analysers I find useful - including a defensive copies analyser.

See [Shipped Analysers](hamarb123.Analyzers/AnalyzerReleases.Shipped.md) on GitHub for the list of analysers included.

NuGet link:
[![NuGet version (hamarb123.Analyzers)](https://img.shields.io/nuget/v/hamarb123.Analyzers.svg?style=flat-square)](https://www.nuget.org/packages/hamarb123.Analyzers/)


## Table Of Contents
- [Configuration](#configuration)
- [Defensive Copies Analysers (C# Only)](#defensive-copies-analysers-c-only)
- [Non-Ordinal String APIs Analyser](#non-ordinal-string-apis-analyser)
- [Nullable If Analyser (VB.NET Only)](#nullable-if-analyser-vbnet-only)


## Configuration

You can use the MSBuild property `Hamarb123AnalyzersDiagnosticsIncludeList` to specify an include-list of analysers to run if that's your preference.

For example:
```xml
<Hamarb123AnalyzersDiagnosticsIncludeList>HAM0001;HAM0003</Hamarb123AnalyzersDiagnosticsIncludeList>
```


## Defensive Copies Analysers (C# Only)

A defensive copy is something the C# compiler emits in some scenarios to ensure that readonly memory is not mutated.

For example:
```csharp
class Class1
{
	//A defensive copy is emitted for field1 since we call a mutating method on readonly memory:
	public static int GetValue() => field1.Increment();
	private static readonly Struct1 field1;
}

struct Struct1
{
	private int i;
	public int Increment() => i++;
}
```

Analysers are included to catch where these occur:
- `HAM0001` (warning) where it's meaningfully different behaviour.
- `HAM0003` (info) where it's known to be only unnecessary.

Idea behind the below precise definition:
- I consider a defensive copy to basically be: whenever a copy is made as a result of memory not being mutable, that results in a meaningful behavioural difference (compared to if the memory was mutable).
- For classes, this is never the case, since `this` is not passed by-reference for them (therefore, whether a copy was made of `this` before calling is irrelevant).
- When calling members on structs cause a defensive copy to be made, there is a meaningful difference since `this` is passed by-reference, and therefore a different `this` is received.
- In practical terms though, members that are known to have `readonly` implementations on `struct`s only cause an IL size and/or performance difference (hence why these are `HAM0003` (info) instead of `HAM0001` (warning)).

Precise definition of defensive copy (`HAM0001`):
- A copy of some readonly memory (LHS) is made, so a potentially mutating member (RHS) (which will be a method in IL) can be called.
  - This requires that wrapping `Unsafe.AsRef` around LHS would elide the copy (otherwise, it's not a defensive copy, just a copy), ignoring safety and issue of `ref struct`s in generics for a moment.
- If call is on a guaranteed non-`valuetype`, then it's never a defensive copy (since call to method on defensive copy of `class` is not observable).
- If member implementation is known to be actually `readonly` (includes `constrained` implementations on known `struct` types that don't override it), then it is instead a `HAM0003` defensive copy.
- "Actually `readonly` member" definition:
  - Based on metadata information only, no source info or special knowledge (other than what is guaranteed by runtime) is allowed.
  - Not marked `readonly`, or otherwise causes a defensive copy.
  - It must be possible to safely elide the defensive copy with `Unsafe.AsRef` (ignoring issue of `ref struct`s in generics), when assuming that the memory location it's stored in is truly `readonly`.


## Non-Ordinal String APIs Analyser

Some APIs on `string` do not use ordinal string comparison - this analyser (`HAM0004`) warns on those.

For example:
```csharp
class Class1
{
	public bool M(string str)
	{
		//The following returns true for the empty string in latest .NET versions, even though it doesn't contain a null character - this API does not use ordinal string comparison by default.
		return str.EndsWith("\0");
	}
}
```


## Nullable If Analyser (VB.NET Only)

In VB.NET, `Boolean?` can be used directly in `If`, ternary, etc. - this often leads to unexpected behaviour for those familiar with C# - this analyser (`HAM0002`) warns on those.

For example:
```vb
Class Class1
	Public Sub M1(str As String)
		If str?.GetHashCode() <> 0 Then
			'This branch is not taken when str is Nothing, even though `str?.GetHashCode()` looks like it should give `Nothing` which is indeed not `0`
		End If
	End Sub
End Class
```

