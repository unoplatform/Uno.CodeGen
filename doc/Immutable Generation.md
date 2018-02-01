# Immutable Generation

## Quick Start

1. Add a reference _Uno_ package `Uno.ImmutableGenerator` in your project.
1. Create a new _POCO_ class with the `Immutable` attribute
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
     (this won't work with _Newtownsoft's JSON.NET_ - that's intentional!)
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