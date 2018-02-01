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
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.CodeGen.Tests
{
	[TestClass]
	public class Given_ImmutableEntity
	{
		[TestMethod]
		public void Immutable_When_CreatingFromBuilder_WithDefaultValue()
		{
			var entity = new MyImmutableEntity.Builder().ToImmutable();

			entity.Should().NotBeNull();
			entity.MyField1.ShouldBeEquivalentTo(4);
		}

		[TestMethod]
		public void Immutable_When_CreatingFromBuilder_And_ImplicitlyCasted()
		{
			MyImmutableEntity entity = new MyImmutableEntity.Builder
			{
				MyField1 = 42
			};

			entity.Should().NotBeNull();
			entity.MyField1.ShouldBeEquivalentTo(42);
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
			r1.MyField1.ShouldBeEquivalentTo(42);
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

			original.MyField1.ShouldBeEquivalentTo(42);
			newInstance.MyField1.ShouldBeEquivalentTo(43);
		}

		[TestMethod]
		public void Immutable_When_CreatingHierarchyOfBuilders()
		{
			A original = A.Default.WithEntity(x => x.WithMyField2(223));
			original.Entity.MyField2.ShouldBeEquivalentTo(223);
		}
	}

	[GeneratedImmutable]
	public partial class A
	{
		public MyImmutableEntity Entity { get; } = MyImmutableEntity.Default;

		public bool IsSomething { get; } = true;
	}

	public partial class B : A
	{
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

		public int Sum => MyField1 + MyField2; // won't generate any builder code

		public DateTimeOffset Date { get; } = DateTimeOffset.Now;

		public ImmutableList<string> List { get; } = ImmutableList.Create("a", "b");
	}

	[GeneratedImmutable]
	public partial class MyGenericImmutable<T>
	{
		public T Entity { get; }
	}

	[GeneratedImmutable(GenerateEquality = false)]
	public partial class MyOtherImmutable : MyGenericImmutable<MyImmutableEntity>
	{
	}

	[GeneratedImmutable]
	public partial class MyVeryOtherImmutable : MyGenericImmutable<MyOtherImmutable>
	{
	}

	[GeneratedImmutable(GenerateEquality = true)]
	public partial class MySuperGenericImmutable<T1, T2, T3, T4, T5, T6>
	{
		public T1 Entity1 { get; }
		public T2 Entity2 { get; }
		public T3 Entity3 { get; }
		public T4 Entity4 { get; }
		public T5 Entity5 { get; }
		[EqualityHash]
		public T6 Entity6 { get; }
		public (T1, T2, T3, T4, T5, T6) Values { get; }

		private static int GetHash_Entity6(T6 value) => 50;
	}
}