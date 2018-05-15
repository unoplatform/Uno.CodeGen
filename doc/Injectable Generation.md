# Injectable Generation

An injectable (`IInjectable`) is a type that will populate its `[Inject]` attributed members (properties and fields) when `IInjectable.Inject(DependencyResolver resolver)` is called. This is typically used for dependency injection.

`InjectableGeneration` will generate a partial class that implements `IInjectable` for every class that contains at least one `[Inject]` attributed member. The generated `IInjectable.Inject` implementation will automatically resolve dependencies and populate members.

You should never have to implement `IInjectable` yourself, but you must manually call `IInjectable.Inject(DependencyResolver resolver)` with the appropritate dependency resolver (e.g., `ServiceLocator`).

## Quick Start

1. Add a reference to the `Uno.CodeGen` _NuGet_ package in your project.
   [![NuGet](https://img.shields.io/nuget/v/Uno.CodeGen.svg)](https://www.nuget.org/packages/Uno.CodeGen/)
1. Add the `[Inject]` attribute to members (fields and properties) of a class that should be injected. Make sure the class is partial.
    ```csharp
    public partial class MyViewModel
    {
        [Inject] IMyService MyService;
        [Inject("name")] IMyService MyNamedService;
        [Inject] Func<IMyService> MyLazyService;
    }
    ```
1. Compile (this is important to generate the partial portion of the class).
1. A partial class that implements `IInjectable` will be generated:
    ```csharp
    partial class MyViewModel : IInjectable
    {
        void IInjectable.Inject(DependencyResolver resolver)
        {
            MyService = (IMyService)resolver(type: typeof(IMyService), name: null);
            MyNamedService = (IMyService)resolver(type: typeof(IMyService), name: "name");
        } 
    }
    ```
1. Inject a dependency resolver to initialize `[Inject]` attributed members:
    ```csharp
    if (myViewModel is IInjectable injectable)
    {        
        injectable.Inject(resolver: (type, name) => serviceLocator.GetInstance(type, name));
    }
    ```
1. All `[Inject]` attributed members should now be populated with the appropriate dependencies.