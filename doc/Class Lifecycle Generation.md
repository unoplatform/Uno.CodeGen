# Class Lifecyle Generation

## Quick Start

1. Add a reference to the `Uno.CodeGen.ClassLifecycle` _Nuget_ package in your project.
   [![NuGet](https://img.shields.io/nuget/v/Uno.CodeGen.svg)](https://www.nuget.org/packages/Uno.CodeGen/)
1. Create a new class and add methods one of the following attributes 
	- `[ConstructorMethod]`
	- `[DisposeMethod]`
	- `[FinalizerMethod]`
   ``` csharp
   public partial class MyClass
   {
       [ConstructorMethod]
       private void MyConstructor()
	   {
	   }
	   
	   [DisposeMethod]
       private void MyDispose()
	   {
	   }
	   
	   [FinalizerMethod]
       private void MyFinalizer()
	   {
	   }
   }
   ```

1. Compile ([the generation process occurs at compile time](https://github.com/unoplatform/Uno.SourceGeneration/issues/9)).
1. It will generate the following public methods for you:
   ``` csharp
    partial class MyClass : IDisposable
	{
		// If none defined on your class, a default constructor which invokes the Initialize()
		public MyClass()
		{
			Initialize();
		}
		
		// The initialize method which must be invoked in each constructor
		private void Initialize()
		{
			MyConstructor();
		}
	
		// Disposable pattern implementation
		protected virtual void Dispose(bool isDisposing)
		{
			if (isDisposing)
			{
				this.MyDispose();
			}
		}

		// IDisposable implementation
		public void Dispose()
		{
			Dispose(true);
		}

		// Class finalizer
		~MyClass()
		{
			Dispose(false);
			this.MyFinalizer();
		}
	}
   ```

## Constructor Rules

If you have any method which is marked with the `[ConstructorMethod]` attribute, an `Initialize`
method will be generated.

1. *All* constructors of the class *must* invoke the `Initialize` method (either directly 
   in its body, or by invoking an other constructor.
1. Parameters:
   You can have some parameters on your constructor methods. They will be aggregated
   as parameters of the `Initialize()` method. 
   - To be consistent between builds, they are sorted using the following rules:
      1. Non optional parameters
      1. Alphabetically
   - If a parameter is optional, its default value will also be copied. 
   - If two methods are defining a parameter with the same name:
      - If they have the same type, they will be merged, otherwise an error will be generated.
	  - If they are booth optional, and the default value is the same, they will stay optional
	    on the `Initialize`. If default value are different, an error will be generated.
		If any is non optional, it will be required.
1. If you don't have any parameter-less constructor defined in your class, and the `Initialize`
   doesn't have any non optional parameter, then a parameter-less constructor will be 
   generated. It will invoke the `Initialize` for you, so you only have to invoked
   it in your own constructors:
   ```csharp
	public MyClass(string aParameter) 
	  *: this()*
	{
		../..
	}
   ```
1. The return type of the method marked with `[ConstructorMethod]` must be `void`

## Dispose Rules

If you have any method marked with the `[DisposeMethod]` attribute, the generation
will make your class implement `IDisposable`, and ensure to invoke your methods when the
`IDisposable.Dispose` method is invoked.

1. Your methods must not have any parameter.
1. The return type of the method marked with `[DisposeMethod]` must be `void`
1. Your class must not implement `IDisposable` itself. If you want to add stuff
   to the dispose process, add another method marked with `[DisposeMethod]`.
1. If your class inherits from a class which already implements IDisposable:
   1. If the base class implements the 
      [dispose pattern](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/dispose-pattern),
      the generated code will override the `Dispose(bool isDisposing)` method.
   1. If the base class is `Uno.Core.IExtensibleDisposable`, it will register itself
      as an extention.
   1. If the `Dispose()` method is virtual, it will override it.
   1. As the override in not possible, an error will be generated.
   
## Finalizer Rules

If you have any method which is marked with the `[FinalizerMethod]` attribute, the generation
will generate the class finalizer (`~MyClass`), and ensure to invoke your methods when invoked.

1. Your methods must not have any parameter.
1. The return type of the method marked with `[FinalizerMethod]` must be `void`
1. Your class must not define the finalizer itself. If you want to add stuff
   to the finalize process, add another method marked with `[FinalizerMethod]`.
