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
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

[assembly: Uno.ImmutableGenerationOptions(TreatArrayAsImmutable = true, GenerateEqualityByDefault = true)]

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
			var tBuilder = typeof(MySuperGenericImmutable<MyImmutableEntity, MyImmutableEntity, MyImmutableEntity[], string, string, string>.Builder);
			var idProperty = tBuilder.GetProperty("Id");
			var attributes = idProperty.GetCustomAttributes(false);
			attributes.Should().HaveCount(3);
		}

		[TestMethod]
		public void Immutable_When_Using_Options()
		{
			var list = ImmutableList<string>.Empty;

			var original = MyImmutableEntity.None;
			var modifiedBuilder = original.WithList(list);
			Option<MyImmutableEntity> modifiedOption = modifiedBuilder;
			MyImmutableEntity modified = modifiedOption;

			original.MatchNone().Should().BeTrue();
			modifiedOption.MatchSome().Should().BeTrue();
			modified.MyField1.Should().Be(MyImmutableEntity.Default.MyField1);
			modified.MyField2.Should().Be(MyImmutableEntity.Default.MyField2);
			modified.Date.Should().Be(MyImmutableEntity.Default.Date);
			modified.List.Should().BeSameAs(list);
		}

		[TestMethod]
		public void Immutable_When_Using_OptionNone()
		{
			MyImmutableEntity.Builder builder = MyImmutableEntity.None;
			Option<MyImmutableEntity> option = builder;
			option.MatchNone().Should().BeTrue();
		}

		[TestMethod]
		public void Immutable_When_Deserializing_A_Using_JsonNet()
		{
			const string json = "{IsSomething:false, T:null, Entity:{MyField1:1, MyField2:2}}";
			var a = JsonConvert.DeserializeObject<A>(json);
			a.Should().NotBeNull();
			a.IsSomething.Should().BeFalse();
			a.T.Should().BeNull();
			a.Entity.Should().NotBeNull();
			a.Entity­.MyField1.Should().Be(1);
			a.Entity­.MyField2.Should().Be(2);
		}

		[TestMethod]
		public void Immutable_When_Deserializing_ABuilder_Using_JsonNet()
		{
			const string json = "{IsSomething:false, T:null, Entity:{MyField1:1, MyField2:2}}";
			var a = JsonConvert.DeserializeObject<A.Builder>(json).ToImmutable();
			a.Should().NotBeNull();
			a.IsSomething.Should().BeFalse();
			a.T.Should().BeNull();
			a.Entity.Should().NotBeNull();
			a.Entity­.MyField1.Should().Be(1);
			a.Entity­.MyField2.Should().Be(2);
		}

		[TestMethod]
		public void Immutable_When_Serializing_A_Using_JsonNet()
		{
			var json = JsonConvert.SerializeObject(A.Default.WithEntity(x => null).ToImmutable());

			json.Should().BeEquivalentTo("{\"T\":null,\"Entity\":null,\"IsSomething\":true}");
		}

		[TestMethod]
		public void Immutable_When_Serializing_ABuilder_Using_JsonNet()
		{
			var json = JsonConvert.SerializeObject(A.Default.WithEntity(x => null));

			json.Should().BeEquivalentTo("{\"T\":null,\"Entity\":null,\"IsSomething\":true}");
		}
	}

	[GeneratedImmutable]
	public partial class A
	{
		public Type T { get; }

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

		public int? MyField3 { get; }

		public string[] MyField4 { get; }

		public IReadOnlyList<string[]> MyField5 { get; }

		public int Sum => MyField1 + MyField2; // won't generate any builder code

		public DateTimeOffset Date { get; } = DateTimeOffset.Now;

		public ImmutableList<string> List { get; } = ImmutableList.Create("a", "b");
	}

	[GeneratedImmutable]
	public abstract partial class MyGenericImmutable<T>
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

	[ImmutableAttributeCopyIgnore("RequiredAttribute")]
	[GeneratedImmutable()]
	public abstract partial class MySuperGenericImmutable<T1, T2, T3, T4, T5, T6>
		where T1: MyImmutableEntity
		where T2: T1
		where T3: IReadOnlyCollection<T1>
	{
		[Required, System.ComponentModel.DataAnnotations.DataType(DataType.Text)]
		[System.ComponentModel.DataAnnotations.Key]
		[System.ComponentModel.DataAnnotations.RegularExpression("regex-pattern", ErrorMessage = "error-msg")]
		public string Id { get; }

		[Required(AllowEmptyStrings = true, ErrorMessage = "Entity1 is required!")]
		public T1 Entity1 { get; }
		[EqualityIgnore]
		public T2 Entity2 { get; }
		[EqualityIgnore]
		public T3 Entity3 { get; }
		[EqualityIgnore]
		public T4 Entity4 { get; }
		[EqualityIgnore]
		public T5 Entity5 { get; }
		[EqualityHash]
		public T6 Entity6 { get; }
		public (T1, T2, T3, T4, T5, T6) Values { get; }

		private static int GetHash_Entity6(T6 value) => 50;
	}

	public partial class MySuperGenericConcrete<T> : MySuperGenericImmutable<MyImmutableEntity, MyImmutableEntity, MyImmutableEntity[], T, T, T>
		where T: IReadOnlyCollection<string>
	{
	}
}