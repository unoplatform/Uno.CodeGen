# Uno CodeGen

`Uno.CodeGen` is a set of tools to generate C# code in msbuild based projects.

## Generate **Equality Members** for your _C#_ classes

![Equality Members generation snippet](doc/assets/equality-snippet.png)

Features:

* Amazingly fast: absolutely **zero reflection at runtime**
* Generates both `.Equals()` and `.GetHashCode()` overrrides
* Generates equality (`==` and `!=`) operators
* Implements `IEquatable<T>`
* Works with derived classes
* **Custom comparers** supported
* Works with collection members (both same order and _unsorted_ equality)
* Works with dictionary members (both same order and _unsorted_ equality)
* Optional case insentive comparisons for strings
* Optional support for _KeyEquality_ (see doc for more details)
* Debuggable: You can put a breakpoint directly in the generated code
* Highly configureable: Generated code provides a lot of useful tips (stripped in previous snippet)
* [Documentation here](doc/Equality%20Generation.md) for Equality Members Generator

## Create **truly Immutable Entities** in _C#_

![Equality Members generation snippet](doc/assets/immutability-snippet.png)

Features:

* Automatic _Null object pattern_ generation
* Automatic generation of `<YourClass>.Builder` nested class
* Fluent `.With<field/property name>()` generation of every members of your class
* Amazingly fast: absolutely **zero reflection at runtime**
* Works with generics & derived classes (even if they are from external assembly)
* Optional support (_on_ by default) for `[GeneratedEquality]`
* Transparent support for _Newtonsoft's JSON.NET_ (activated when detected, can be turned off)
* Transparent support for _System.Text.Json_ (activated when detected, can be turned off)
* Debuggable: You can put a breakpoint directly in the generated code
* Validation to avoid mutable code in your class
* Highly configureable: Generated code provides a lot of useful tips (stripped in previous snippet)
* [Documentation here](doc/Immutable%20Generation.md) for Immutable Entities Generator

## Available Generators

| Generator | Triggering Attributes | Usage |    |
| --------- | -------------------- | ----- | -- |
| `ClassLifecycleGenerator` | `[ConstructorMethod]` `[DisposeMethod]` `[FinalizerMethod]` | Generate code to extend the lifecyle of a class. | [Documentation](doc/Class%20Lifecycle%20Generation.md) |
| `CompilationReferencesListingGenerator` | _none_ | Generate a file without _useful_ code, containing only comments detailing references used to compile the project. | [Documentation](doc/Compilation%20References.md) |
| `EqualityGenerator` | `[GenerateEquality]` | Generate code for efficient `.Equals()` members generation. | [Documentation](doc/Equality%20Generation.md) |
| `ImmutableGenerator` | `[GenerateImmutable]` | Generate code to build truly immutable entities. | [Documentation](doc/Immutable%20Generation.md) |
| `InjectableGenerator` | `[Inject]` | Generate code to resolve and inject dependencies. | [Documentation](doc/Injectable%20Generation.md) |

## Nuget

[![NuGet](https://buildstats.info/nuget/Uno.CodeGen?includePreReleases=true)](https://www.nuget.org/packages/Uno.CodeGen/)

## FAQ
[Read our FAQ here](doc/faq.md)

# Have questions? Feature requests? Issues?

Make sure to visit our [FAQ](doc/faq.md), [StackOverflow](https://stackoverflow.com/questions/tagged/uno-platform), [create an issue](https://github.com/unoplatform/Uno.CodeGen/issues) or [visit our gitter](https://gitter.im/uno-platform/Lobby).

# Contributing

Read our [contributing guide](CONTRIBUTING.md) to learn about our development process and how to propose bug fixes and improvements.
