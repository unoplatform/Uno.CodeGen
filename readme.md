# Uno CodeGen

`Uno.CodeGen` is a set of tools to generate C# code in msbuild based projects.

## Build status

| Target | Branch | Status | Recent builds | Recommended Nuget packages version |
| ------ | ------ | ------ | ------ | ------ |
| `development` | [master](https://github.com/nventive/Uno.CodeGen/tree/master) | [![Build status](https://ci.appveyor.com/api/projects/status/bh83u4i2lp0hrg8r/branch/master?svg=true)](https://ci.appveyor.com/project/nventivedevops/uno-codegen/branch/master) | ![Build Stats](https://buildstats.info/appveyor/chart/nventivedevops/uno-codegen?branch=master&includeBuildsFromPullRequest=false) | [![NuGet](https://buildstats.info/nuget/Uno.CodeGen?includePreReleases=true)](https://www.nuget.org/packages/Uno.CodeGen/) |
| `stable` | [stable](https://github.com/nventive/Uno.CodeGen/tree/stable) | [![Build status](https://ci.appveyor.com/api/projects/status/bh83u4i2lp0hrg8r/branch/stable?svg=true)](https://ci.appveyor.com/project/nventivedevops/uno-codegen/branch/stable) | ![Build Stats](https://buildstats.info/appveyor/chart/nventivedevops/uno-codegen?branch=stable&includeBuildsFromPullRequest=false) | [![NuGet](https://buildstats.info/nuget/Uno.CodeGen?includePreReleases=false)](https://www.nuget.org/packages/Uno.CodeGen/) |


## Available Generators

| Generator | Triggering Attributes | Usage |    |
| --------- | -------------------- | ----- | -- |
| `ClassLifecycleGenerator` | `[ConstructorMethod]` `[DisposeMethod]` `[FinalizerMethod]` | Generate code to extend the lifecyle of a class. | [Documentation](doc/Class%20Lifecycle%20Generation.md) |
| `CompilationReferencesListingGenerator` | _none_ | Generate a file without _useful_ code, containing only comments detailing references used to compile the project. | [Documentation](doc/Compilation%20References.md) |
| `EqualityGenerator` | `[GenerateEquality]` | Generate code for efficient `.Equals()` members generation. | [Documentation](doc/Equality%20Generation.md) |
| `ImmutableGenerator` | `[GenerateImmutable]` | Generate code to build truly immutable entities. | [Documentation](doc/Immutable%20Generation.md) |
