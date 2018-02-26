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
		public readonly int I;
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
}