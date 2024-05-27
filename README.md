# hamarb123.Analyzers
A bunch of analysers I find useful - including a defensive copies analyser.

See [Shipped Analysers](hamarb123.Analyzers/AnalyzerReleases.Shipped.md) for the list of analysers included.

NuGet link:
[![NuGet version (hamarb123.Analyzers)](https://img.shields.io/nuget/v/hamarb123.Analyzers.svg?style=flat-square)](https://www.nuget.org/packages/hamarb123.Analyzers/)


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
	//A defensive copy is emitted for field since we call a mutating method on readonly memory:
	public static int GetValue() => field.Increment();
	private static readonly Struct1 field;
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
- A precise definition is given [here](hamarb123.Analyzers/DefensiveCopies/DefensiveCopyAnalyzer.cs) - before opening an issue, please check it against this definition.


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

In VB.NET, `Boolean?` can be used directly in `If`, ternary, etc. - this often leads to undesirable behaviour - this analyser (`HAM0002`) warns on those.

For example:
```vb
Class Class1
	Public Sub M1(str As String)
		If str?.GetHashCode() <> 0 Then
			'This branch is not taken when str is Nothing, even though `str?.GetHashCode()` looks like it should give `Nothing` which is indeed `<> 0`
		End If
	End Sub
End Class
```

