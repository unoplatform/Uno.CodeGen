# Uno.CodeGen FAQ

## How it works?

Generators are based on the [Uno.SourceGeneration](https://github.com/nventive/Uno.SourceGeneration) project.

At compile time, they analyze the _Roslyn_ compilation and use it to generate relevant
code. Generated code are placed in the `<project>\obj\<config>\<platform>\g\<generatorname>`
folder­ and they are dynamically added to the project compilation.

## Is there a way to commit generated code with my project in source control?

You can set the `UnoSourceGeneratorOutputPath` in your `csproj` file to the desired location
(relative to your project folder).

If you are using _Sdk_ projects - projects were all `.cs` files are automatically compiled
in your project, the generated files will be part of your project.

Example:

``` xml
<UnoSourceGeneratorOutputPath>GeneratedFiles</UnoSourceGeneratorOutputPath>
```

## Intellisense is broken for generated code. Is there a way to fix it?

It should work fine for _Sdk_ projects (new project format that comes with _dot net core_),
but for classical probjects (WPF, Xamarin, UWP, etc...) it seems you need to force VisualStudio
to reset its cache. A quick way to do that is to toggle the _Show All files_ in the _Solution Explorer_.
