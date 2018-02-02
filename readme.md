# Uno CodeGen

`Uno.CodeGen` is a set of tools to generate C# code in msbuild based projects.

## Build status

| Target | Branch | Status | Recommended Nuget packages version |
| ------ | ------ | ------ | ------ |
| `development` | [master](https://github.com/nventive/Uno.CodeGen/tree/master) | [![Build status](https://ci.appveyor.com/api/projects/status/bh83u4i2lp0hrg8r/branch/master?svg=true)](https://ci.appveyor.com/project/nventivedevops/uno-codegen/branch/master) | [![NuGet](https://img.shields.io/nuget/v/Uno.CodeGen.svg)](https://www.nuget.org/packages/Uno.CodeGen/) |
| `stable` | [stable](https://github.com/nventive/Uno.CodeGen/tree/stable) | (no build yet) | (not published yet) |

## Available Generators

| Generator | Triggering Attribute | Usage |    |
| --------- | -------------------- | ----- | -- |
| `EqualityGenerator` | `[GenerateEquality]` | Generate code for efficient `.Equals()` members generation. | [Documentation](doc/Equality%20Generation.md) |
| `ImmutableGenerator` | `[GenerateImmutable]` | Generate code to build truly immutable entities. | [Documentation](doc/Immutable%20Generation.md) |