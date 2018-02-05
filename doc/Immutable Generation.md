# Immutable Generation

## Quick Start

1. Add a reference to the `Uno.CodeGen` _Nuget_ package in your project.
   [![NuGet](https://img.shields.io/nuget/v/Uno.CodeGen.svg)](https://www.nuget.org/packages/Uno.CodeGen/)
1. Create a new [POCO](https://en.wikipedia.org/wiki/Plain_old_CLR_object)
   class with the `[GeneratedImmutable]` attribute
   ``` csharp
   [GeneratedImmutable] // Uno.GeneratedImmutableAttribute
   public partial class MyEntity
   {
       // Please take note all properties are "get-only"

       public string A { get; } = "a";

       public string B { get; } = "b";
   }
   ```
1. Compile (this is important to generate the partial portion of the class).
1. Use it in your code
   ``` csharp
   MyEntity entity1 = MyEntity.Default; // A="a", B="b"
   MyEntity entity2 = entity1.WithA("c"); // A="c", B="b"
   MyEntity entity3 = new MyEntity.Builder(entity2) { B="123" }; // A="c", B="123"
   MyEntity entity4 = MyEntity.Default
       .WithA("value for A") // Intermediate fluent value is a builder,
       .WithB("value for B"); // so there's no memory impact doing this.
   ```

## How to use it

1. The class needs to be partial - because generated code will augment it.
1. Restrictions:
   * **No default constructor allowed**
     (this won't work with [Newtownsoft's JSON.NET](https://www.newtonsoft.com/json)
	 - that's intentional: You need to deserialize the builder instead)
   * **No property setters allowed** (even `private` ones):
     properties should be _read only_, even for the class itself.
   * **No fields allowed** (except static fields, but that would be weird).
   * **Static members are ok**, as they can only manipulate immutable stuff.
     Same apply for extensions.
   * **Nested classes not supported**, the class must be directly in its
     namespace for the generator to happend.
1. Property Initializers will become default values in the builder.
   ex:
   ``` csharp
   public partial class UserPreferences
   {
        // Each time a new version of the builder is created
        // the initializer ("DateTimeOffset.Now" here) will be
        // applied first.
        public DateTimeOffSet LastChange { get; } = DateTimeOffset.Now;
   }
   ```
   **Warning: don't use this for generating an id like a _guid_, because time you'll
   update the entity you'll get a new id.**

   * Properties with implementation will be ignored (also called _computed property_).

     Example:
     ``` csharp
     /// <summary>
     /// Gets the fullname of the contact.
     /// </summary>
     public string FullName => LastName + ", " + FirstName;
     ```
   * Generated builder are implicitely convertible to/from the entity
   * Collections must be IReadOnlyCollection, IReadonlyDictionary or an
     immutable type from System.Collection.Immutable namespace:
     ImmutableList, ImmutableDictionary, ImmutableArray, ImmutableSet...
     **The type of the collection must be immutable too**.
1. A static `.Default` readonly property will containt a default instance with
   properties to their default initial values.

   It can be used as starting point to create a new instance, example:
   ``` csharp
   Invoice invoice = Invoice.Default
       .WithId(invoiceId)
       .WithCustomer(customer)
       .WithItems(items);
   ```

## Usage

``` csharp
// No default constructor allowed on [Immutable] classes
var x = new MyEntity() ; // Won't compile

// Method 1: Creating a builder from its constructor
MyEntity.Builder b = new MyEntity.Builder();

// Method 2: Creating a builder using implicit cast
MyEntity myEntity = [...];
MyEntity.Builder b = myEntity;

// Method 3: Creating a builder using .WithXXX() method
MyEntity myEntity = [...];
MyEntity.Builder b = myEntity.WithName("new name");

// Method 4: You can also create a builder from a previous version
MyEntity.Builder b = new MyEntity.Builder(previousEntity);

// To get the Immutable entity...

// Method 1 : Use implicit conversion (MyEntity.Builder => MyEntity)
MyEntity e = b;

// Method 2 : Use the .ToImmutable() method
MyEntity e = b.ToImmutable();

// Method 3 : Use the generated constructor with builder as parameter
MyEntity e = new MyEntity(b);
```

## `.WithXXX()` helpers

All set+set properties will also generate a `With<propertyName>()` method.
The method will take a parameter of the type of the corresponding property.
This method is present on both the class and the builder and always
returns a builder.

Usage:

``` csharp
public partial class MyEntity
{
    public string A { get; } = string.Empty;
    public string B { get; } = null;
}

[...]

// Create a first immutable instance
var v1 = new MyEntity.Builder { A="a", B="b" };

// Create a new immutable instance
var v2 = v1
    .WithB("b2")
    .ToImmutable();

// Same as previous but with the usage of implicit conversion
MyEntity v2bis = v1.WithB("b2");
```

## Aggregates (graph of objects/classes)

Let's say we write this...

``` csharp
[Immutable]
public partial class MyRootEntity
{
    public string A { get; }
    public MySubEntity B { get; }
}

[Immutable]
public partial class MySubEntity
{
    public string C { get; }
    public string D { get; }
    public ImmutableList<string> E { get; }
}
```

This will generate something like this:

``` csharp
[Immutable]
public partial class MyRootEntity
{
    public partial class Builder
    {
        public string A { get; set; }
        public MySubEntity B { get; set; }
    }
}

[Immutable]
public partial class MySubEntity
{
    public partial class Builder
    {
        public string C { get; set; }
        public string D { get; set; }
    }
}
```

Important:

* Complex properties \*\***MUST**\*\* be immutable entities.
  A complex property is when it's a defined type, not a CLR primitive.
* Indexers are not supported.
* _Events Properties_ are not supported.

# FAQ

## What if I need to use it with [Newtownsoft's JSON.NET](https://www.newtonsoft.com/json)?
You simply need to deserialze the builder instead of the class itself.
The implicit casting will automatically convert it to the right type.

Example:
``` csharp
  MyEntity e = JsonConvert.DeserializeObject<MyEntity.Builder>(json);
```

## It's generating a lot of unused method. It's a waste.
For most application the compiled code won't be significant. Assets
in projects are usually a lot bigger than that.

If you are using a linker tool
([Mono Linker](http://www.mono-project.com/docs/tools+libraries/tools/linker/) /
[DotNetCore Linker](https://github.com/dotnet/core/blob/master/samples/linker-instructions.md)),
those unused methods will be removed from compiled result.

We think the cost of this unused code is cheaper than the potential
bugs when writing and maintaining this code manually.

## What is the usage of the `[Immutable]` and `[ImmutableBuilder]` attributes?
* **ImmutableAttribute**:
  The `[Immutable]` is used by other parts of _Uno_
  (some are not published as opened source) to identify an entity has been
  immutable.
* **ImmutableBuilderAttribute**:
  The `[ImmutableBuilder]` is used to indicate which class to use to build
  the target immutable type. The builder is expected to implement
  the `IImmutableBuilder<TImmutable>` interface.

If you want, you can manually create immutable classes and use those
attributes in your code: the code generators will use it as if it was
generated.

## Are immutable entities thread-safe?
Yes! That's the major aspect of immutable entities. Once an instance
of an immutable class is created, it's impossible to change it.
(ok, it's possible by using reflection, but why would you do that?)

But the builders are not thread safe. That means updating the same
property of the same instance concurrently (from many threads) will
produce unexpected result.

## Can we reuse builders?
Yes. You can continue to update the builder even after calling
`.ToImmutable()`. The created instance won't be updated.

## What is the usage of the [Pure] attribute on some methods?
This attribute is used to indicate a _pure method_. It means a method
having no side effect.

Since calling a _pure method_ without using the result is a waste
of resources, some IDE tools like [ReSharper(TM)](https://www.jetbrains.com/resharper/)
will give you a visual warning when you're not using the result.

## Can I create a nested immutable types?
Not supported yet. Open an issue if you need this.

## Can I use this for value types? (`struct`)
No. The type must be a reference type (`class`).

## What is happening with attributes on my properties?
All attributes are copied, except those defined in `Uno.Immutables` and
`Uno.Equality`. If you need to remove other attributes, you just need
to use the `[ImmutableAttributeCopyIgnore(<regex>)]` attribute.

For a better fine control, you can put it on assembly level, on a type or
even on a property itself.