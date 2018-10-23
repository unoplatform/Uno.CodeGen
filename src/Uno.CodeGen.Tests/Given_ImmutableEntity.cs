// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Equality;

[assembly: Uno.ImmutableGenerationOptions(TreatArrayAsImmutable = true, GenerateEqualityByDefault = true)]

namespace Uno.CodeGen.Tests
{
	[TestClass]
	public partial class Given_ImmutableEntity
	{
		[TestMethod]
		public void Immutable_When_CreatingFromBuilder_WithDefaultValue()
		{
			var entity = new MyImmutableEntity.Builder().ToImmutable();

			entity.Should().NotBeNull();
			entity.MyField1.Should().Be(4);
		}

		[TestMethod]
		public void Immutable_When_CreatingFromBuilder_And_ImplicitlyCasted()
		{
			MyImmutableEntity entity = new MyImmutableEntity.Builder
			{
				MyField1 = 42
			};

			entity.Should().NotBeNull();
			entity.MyField1.Should().Be(42);
		}

		[TestMethod]
		public void Immutable_When_CreatingFromBuilder_TheResultIsCached()
		{
			var builder = new MyImmutableEntity.Builder
			{
				MyField1 = 42
			};

			var r1 = builder.ToImmutable();
			var r2 = builder.ToImmutable();

			r1.Should().NotBeNull();
			r1.MyField1.Should().Be(42);
			r1.Should().BeSameAs(r2);
		}

		[TestMethod]
		public void Immutable_When_CreatingABuilderFromExisting_And_NoChanges_Then_ReferenceIsSame()
		{
			MyImmutableEntity original = new MyImmutableEntity.Builder
			{
				MyField1 = 42
			};

			var builder = new MyImmutableEntity.Builder(original);
			var newInstance = builder.ToImmutable();

			newInstance.Should().BeSameAs(original);
		}

		[TestMethod]
		public void Immutable_When_CreatingABuilderFromExisting()
		{
			MyImmutableEntity original = new MyImmutableEntity.Builder
			{
				MyField1 = 42
			};

			MyImmutableEntity newInstance = original
				.WithMyField1(43);

			original.MyField1.Should().Be(42);
			newInstance.MyField1.Should().Be(43);
		}

		[TestMethod]
		public void Immutable_When_CreatingHierarchyOfBuilders()
		{
			A original = A.Default.WithEntity(x => x.WithMyField2(223));
			original.Entity.MyField2.Should().Be(223);
		}

		[TestMethod]
		public void Immutable_When_CreatingHierarchyOfBuilders_Then_NullRefException_Is_Prevented()
		{
			var list = ImmutableList<string>.Empty;
			A a = null;

			var newObj = a.WithEntity(b => b.WithList(list));

			newObj.Entity.List.Should().BeSameAs(list);
		}

		[TestMethod]
		public void Immutable_When_Using_Attributes_Then_They_Are_Copied_On_Builder()
		{
			var tBuilder = typeof(AbstractImmutableTypeWithManyGenerics<MyImmutableEntity, MyImmutableEntity, MyImmutableEntity[], string, string, string>.Builder);
			var idProperty = tBuilder.GetProperty("Id");
			var attributes = idProperty.GetCustomAttributes(false);
			attributes.Should().HaveCount(3);
		}

		[TestMethod]
		public void Immutable_When_Assigning_EquivalentParameters()
		{
			ImmutableForTypeEquality original = ImmutableForTypeEquality.Default
				.WithString("str")
				.WithSortedBytes(new byte[] { 1, 2, 3 })
				.WithUnsortedBytes(new byte[] { 1, 2, 3 });

			ImmutableForTypeEquality modified = original
				.WithString("str")
				.WithSortedBytes(new byte[] { 1, 2, 3 })
				.WithUnsortedBytes(new byte[] { 1, 2, 3 });

			original.Should().BeEquivalentTo(modified);
			original.Should().BeSameAs(modified);
		}

		[TestMethod]
		public void Immutable_When_AssigningEqualityIgnoreProperty()
		{
			Console.WriteLine(ImmutableWithEqualityIgnoreProperty.X1);

			ImmutableWithEqualityIgnoreProperty original = new ImmutableWithEqualityIgnoreProperty("key1", "value1");
			ImmutableWithEqualityIgnoreProperty modified1 = original.WithIgnoredValue("ignored1");
			ImmutableWithEqualityIgnoreProperty modified2 = modified1.WithIgnoredValue("ignored2");
			ImmutableWithEqualityIgnoreProperty modified3 = modified1.WithIgnoredValue("ignored1");

			modified1.IgnoredValue.Should().Be("ignored1");
			modified2.IgnoredValue.Should().Be("ignored2");
			modified3.IgnoredValue.Should().Be("ignored1");
			modified3.Should().BeSameAs(modified1);
		}
	}

	[GeneratedImmutable]
	public partial class A
	{
		public Type T { get; }

		public MyImmutableEntity Entity { get; } = MyImmutableEntity.Default;

		public bool IsSomething { get; } = true;

		public IImmutableDictionary<string, string> Metadata { get; }
	}

	public partial class B : A
	{
		public Uri FirstField { get; }

		public string SecondField { get; }

		public long ThirdField { get; }

		public TimeSpan TimeSpan { get; }

		public bool Boolean { get; }

		public DateTimeKind Enum { get; }

		public new bool IsSomething { get; }
	}

	[GeneratedImmutable]
	public partial class MyImmutableEntity
	{
		public int MyField1 { get; } = 4;

		public int MyField2 { get; } = 75;

		public int? MyField3 { get; }

		public string[] MyField4 { get; }

		public IReadOnlyList<string[]> MyField5 { get; }

		public ImplicitlyImmutableClass MyField6 { get; }

		public ImplicitlyImmutableStruct MyField7 { get; }

		public int Sum => MyField1 + MyField2; // won't generate any builder code

		public DateTimeOffset Date { get; } = DateTimeOffset.Now;

		public ImmutableList<string> List { get; } = ImmutableList.Create("a", "b");
	}

	public class ImplicitlyImmutableClass
	{
		public ImplicitlyImmutableClass(string a, string b, int i)
		{
			A = a;
			B = b;
			I = i;
		}

		public string A { get; }

		public string B { get; }

#pragma warning disable SA1401 // Fields must be private
		public readonly int I;
#pragma warning restore SA1401 // Fields must be private
	}

	public struct ImplicitlyImmutableStruct
	{
		public ImplicitlyImmutableStruct(string a, string b, int i)
		{
			A = a;
			B = b;
			I = i;
		}

		public string A { get; }

		public string B { get; }

		public readonly int I;
	}

	[GeneratedImmutable]
	public partial class ImmutableForTypeEquality
	{
		public string String { get; }

		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Sorted)]
		public byte[] SortedBytes { get; }

		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Unsorted)]
		public byte[] UnsortedBytes { get; }
	}

	[GeneratedImmutable]
	public partial class ImmutableWithStaticProperties
	{
		public static ImmutableWithStaticProperties Version1 { get; } = Default;

		public static ImmutableWithStaticProperties Version2 { get; } = Default.WithVersion(2);

		public static ImmutableWithStaticProperties Version3 { get; } = Default.WithVersion(3);

		[EqualityKey]
		public int Version { get; } = 1;
	}

	[GeneratedImmutable]
	public partial class ImmutableWithEqualityIgnoreProperty
	{
		[EqualityKey]
		public string Key { get; }

		public string Value { get; }

		[EqualityIgnore]
		public string IgnoredValue { get; }

		public ImmutableWithEqualityIgnoreProperty(string key, string value)
		{
			Key = key;
			Value = value;
		}

		public static string X1 { get; } = X2;

		public static string X2 => X;

		public static string X = "123";
	}
}
