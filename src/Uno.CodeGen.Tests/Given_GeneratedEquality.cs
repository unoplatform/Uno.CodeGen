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

namespace Uno.CodeGen.Tests
{
	[TestClass]
	public class Given_GeneratedEquality
	{
		[TestMethod]
		public void Equality_WhenUsingCustomComparer()
		{
			var e1 = GeneratedImmutableEntityForEquality.Default.WithId("a");
			var e2 = GeneratedImmutableEntityForEquality.Default.WithId("A");
			var e3 = e1.WithId("A");
			var e4 = e2.WithId("A");

			e1.Should().BeEquivalentTo(e2);
			e1.Should().BeEquivalentTo(e3);
			e1.Should().BeEquivalentTo(e4);
			e3.Should().BeSameAs(e1);
			e4.Should().BeSameAs(e2);
			e2.Should().NotBeSameAs(e1);

			typeof(GeneratedImmutableEntityForEquality).Should()
				.HaveImplictConversionOperator<GeneratedImmutableEntityForEquality.Builder, GeneratedImmutableEntityForEquality>();
			typeof(GeneratedImmutableEntityForEquality).Should()
				.HaveImplictConversionOperator<GeneratedImmutableEntityForEquality, GeneratedImmutableEntityForEquality.Builder>();
		}

		[TestMethod]
		public void Equality_WhenUsingArray()
		{
			var e1 = new MyEntityForArrayAndDictionaryEquals.Builder {Array = new[] {"a", "b", "c"}}.ToImmutable();
			var e2 = new MyEntityForArrayAndDictionaryEquals.Builder {Array = new[] {"a", "b", "c"}}.ToImmutable();
			var e3 = new MyEntityForArrayAndDictionaryEquals.Builder {Array = new[] {"a", "b", "c", "d"}}.ToImmutable();

			e1.Should().NotBeNull();
			e2.Should().NotBeNull();
			e3.Should().NotBeNull();

			(e1 == e2).Should().BeTrue();
			(e1 == e3).Should().BeFalse();
			e1.Equals(e2).Should().BeTrue();
			e1.Equals(e3).Should().BeFalse();
		}

		[TestMethod]
		public void Equality_WhenUsingCollectionsAndDictionaries()
		{
			new MyEntityForAllCollectionsAndDictionaryTypes()
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes()).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(arraySorted: new[] {"a", "b"})
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(arraySorted: new[] {"a", "b"})).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(arraySorted: new[] { "a", "b" })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(arraySorted: new[] { "b", "a" })).Should().BeFalse();

			new MyEntityForAllCollectionsAndDictionaryTypes(arrayUnsorted: new[] { "a", "b" })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(arrayUnsorted: new[] { "a", "b" })).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(arrayUnsorted: new[] { "a", "b" })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(arrayUnsorted: new[] { "b", "a" })).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(listSorted: new List<string>{"a", "b"})
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(listSorted: new List<string> { "a", "b" })).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(listSorted: new List<string> { "a", "b" })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(listSorted: new List<string> { "b", "a" })).Should().BeFalse();

			new MyEntityForAllCollectionsAndDictionaryTypes(listUnsorted: new List<string> { "a", "b" })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(listUnsorted: new List<string> { "a", "b" })).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(listUnsorted: new List<string> { "a", "b" })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(listUnsorted: new List<string> { "b", "a" })).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(readonlyCollectionSorted: ImmutableList.Create("a", "b"))
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(readonlyCollectionSorted: ImmutableList.Create("a", "b"))).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(readonlyCollectionSorted: ImmutableList.Create("a", "b"))
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(readonlyCollectionSorted: ImmutableList.Create("b", "a"))).Should().BeFalse();

			new MyEntityForAllCollectionsAndDictionaryTypes(readonlyCollectionUnsorted: ImmutableList.Create("a", "b"))
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(readonlyCollectionUnsorted: ImmutableList.Create("a", "b"))).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(readonlyCollectionUnsorted: ImmutableList.Create("a", "b"))
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(readonlyCollectionUnsorted: ImmutableList.Create("b", "a"))).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(dictionary: new Dictionary<string, string>{{"a", "a"}, {"b", "b"}})
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(dictionary: new Dictionary<string, string> { { "a", "a" }, { "b", "b" } })).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(dictionary: new Dictionary<string, string> { { "a", "a" }, { "b", "b" } })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(dictionary: new Dictionary<string, string> { { "b", "b" }, { "a", "a" } })).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(dictionary: new Dictionary<string, string> { { "a", "a" }, { "b", "b" } })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(dictionary: new Dictionary<string, string> { { "a", "a" }, { "b", "z" } })).Should().BeFalse();

			new MyEntityForAllCollectionsAndDictionaryTypes(dictionary: new Dictionary<string, string> { { "a", "a" }, { "b", "b" } })
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(dictionary: new Dictionary<string, string> { { "a", "a" }, { "c", "c" } })).Should().BeFalse();

			new MyEntityForAllCollectionsAndDictionaryTypes(readonlyDictionary: ImmutableDictionary<string, string>.Empty.Add("a", "a").Add("b", "b"))
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(readonlyDictionary: ImmutableDictionary<string, string>.Empty.Add("a", "a").Add("b", "b"))).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(readonlyDictionary: ImmutableDictionary<string, string>.Empty.Add("a", "a").Add("b", "b"))
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(readonlyDictionary: ImmutableDictionary<string, string>.Empty.Add("b", "b").Add("a", "a"))).Should().BeTrue();

			new MyEntityForAllCollectionsAndDictionaryTypes(readonlyDictionary: ImmutableDictionary<string, string>.Empty.Add("a", "a").Add("b", "b"))
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(readonlyDictionary: ImmutableDictionary<string, string>.Empty.Add("a", "a").Add("b", "z"))).Should().BeFalse();

			new MyEntityForAllCollectionsAndDictionaryTypes(readonlyDictionary: ImmutableDictionary<string, string>.Empty.Add("a", "a").Add("b", "b"))
				.Equals(new MyEntityForAllCollectionsAndDictionaryTypes(readonlyDictionary: ImmutableDictionary<string, string>.Empty.Add("a", "a").Add("c", "c"))).Should().BeFalse();
		}

		[TestMethod]
		public void Equality_WhenUsingMyEntityForStringComparison()
		{
			new MyEntityForStringComparison.Builder {DefaultMode = "a"}.ToImmutable()
				.Equals(new MyEntityForStringComparison.Builder {DefaultMode = "a"}.ToImmutable()).Should().BeTrue();

			new MyEntityForStringComparison.Builder { DefaultMode = "a" }.ToImmutable()
				.Equals(new MyEntityForStringComparison.Builder { DefaultMode = "A" }.ToImmutable()).Should().BeFalse();

			new MyEntityForStringComparison.Builder { DefaultMode = "" }.ToImmutable()
				.Equals(MyEntityForStringComparison.Default).Should().BeFalse();

			new MyEntityForStringComparison.Builder { IgnoreCase = "a" }.ToImmutable()
				.Equals(new MyEntityForStringComparison.Builder { IgnoreCase = "a" }.ToImmutable()).Should().BeTrue();

			new MyEntityForStringComparison.Builder { IgnoreCase = "a" }.ToImmutable()
				.Equals(new MyEntityForStringComparison.Builder { IgnoreCase = "A" }.ToImmutable()).Should().BeTrue();

			new MyEntityForStringComparison.Builder { IgnoreCase = "" }.ToImmutable()
				.Equals(MyEntityForStringComparison.Default).Should().BeFalse();

			new MyEntityForStringComparison.Builder { EmptyEqualsNull = "a" }.ToImmutable()
				.Equals(new MyEntityForStringComparison.Builder { EmptyEqualsNull = "a" }.ToImmutable()).Should().BeTrue();

			new MyEntityForStringComparison.Builder { EmptyEqualsNull = "a" }.ToImmutable()
				.Equals(new MyEntityForStringComparison.Builder { EmptyEqualsNull = "A" }.ToImmutable()).Should().BeFalse();

			new MyEntityForStringComparison.Builder { EmptyEqualsNull = "" }.ToImmutable()
				.Equals(MyEntityForStringComparison.Default).Should().BeTrue();

			new MyEntityForStringComparison.Builder {EmptyEqualsNull = ""}.ToImmutable()
				.Should().BeSameAs(MyEntityForStringComparison.Default);

			new MyEntityForStringComparison.Builder { EmptyEqualsNullIgnoreCase = "a" }.ToImmutable()
				.Equals(new MyEntityForStringComparison.Builder { EmptyEqualsNullIgnoreCase = "a" }.ToImmutable()).Should().BeTrue();

			new MyEntityForStringComparison.Builder { EmptyEqualsNullIgnoreCase = "a" }.ToImmutable()
				.Equals(new MyEntityForStringComparison.Builder { EmptyEqualsNullIgnoreCase = "A" }.ToImmutable()).Should().BeTrue();

			new MyEntityForStringComparison.Builder { EmptyEqualsNullIgnoreCase = "" }.ToImmutable()
				.Equals(MyEntityForStringComparison.Default).Should().BeTrue();

			new MyEntityForStringComparison.Builder { EmptyEqualsNullIgnoreCase = "" }.ToImmutable()
				.Should().BeSameAs(MyEntityForStringComparison.Default);

		}
	}

	[GeneratedEquality]
	internal partial class MyEqualityClass<TSomething>
	{
		[EqualityKey]
		internal string A { get; set; }

		private static int GetHash_A(string value) => -1;
		private static IEqualityComparer<string> A_CustomComparer => StringComparer.OrdinalIgnoreCase;

		[EqualityKey]
		internal int B { get; set; }

		[EqualityHash]
		internal bool C { get; set; }

		[EqualityHash]
		internal string D { get; set; }
		private static IEqualityComparer<string> D_CustomComparer => StringComparer.OrdinalIgnoreCase;

		[EqualityHash]
		internal TSomething E { get; set; }
		private static IEqualityComparer<TSomething> E_CustomComparer => EqualityComparer<TSomething>.Default;

		[EqualityHash]
		internal bool[] F { get; set; }

		[EqualityHash]
		internal IEnumerable<int> G { get; set; }

		[EqualityHash]
		internal ICollection<TSomething> H { get; set; }
	}

	[GeneratedEquality]
	internal partial class DerivedEqualityClass : MyEqualityClass<int>
	{

	}

	[GeneratedEquality]
	internal partial struct MyEqualityStruct
	{
		[EqualityKey]
		internal string A { get; }
		[Key]
		internal string B { get; }
	}


	[GeneratedImmutable(GenerateEquality = true)]
	public partial class GeneratedImmutableEntityForEquality
	{
		public string Id { get; }

		private static IEqualityComparer<string> Id_CustomComparer => StringComparer.OrdinalIgnoreCase;
	}

	[GeneratedImmutable(GenerateEquality = true)]
	public partial class MyEntityForArrayAndDictionaryEquals
	{
		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Unsorted)]
		public string[] Array { get; } = { };

		public ImmutableDictionary<string, string> Dictionary { get; } = ImmutableDictionary<string, string>.Empty;
	}

	[GeneratedImmutable(GenerateEquality = true)]
	public partial class MyEntityForStringComparison
	{
		[EqualityComparerOptions(StringMode = StringComparerMode.Default)]
		public string DefaultMode { get; }

		[EqualityComparerOptions(StringMode = StringComparerMode.IgnoreCase)]
		public string IgnoreCase { get; }

		[EqualityComparerOptions(StringMode = StringComparerMode.EmptyEqualsNull)]
		public string EmptyEqualsNull { get; }

		[EqualityComparerOptions(StringMode = StringComparerMode.EmptyEqualsNull | StringComparerMode.IgnoreCase)]
		public string EmptyEqualsNullIgnoreCase { get; }
	}

	[GeneratedEquality]
	public partial class MyEntityForAllCollectionsAndDictionaryTypes
	{
		public MyEntityForAllCollectionsAndDictionaryTypes(
			string[] arraySorted = null,
			string[] arrayUnsorted = null,
			List<string> listSorted = null,
			List<string> listUnsorted = null,
			IImmutableList<string> readonlyCollectionSorted = null,
			IImmutableList<string> readonlyCollectionUnsorted = null,
			IImmutableDictionary<string, string> readonlyDictionary = null,
			Dictionary<string, string> dictionary = null)
		{
			ArraySorted = arraySorted;
			ArrayUnsorted = arrayUnsorted;
			ListSorted = listSorted;
			ListUnsorted = listUnsorted;
			ReadonlyCollectionSorted = readonlyCollectionSorted;
			ReadonlyCollectionUnsorted = readonlyCollectionUnsorted;
			ReadonlyDictionary = readonlyDictionary;
			Dictionary = dictionary;
		}

		[EqualityHash]
		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Sorted)]
		public string[] ArraySorted { get; }

		[EqualityHash]
		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Unsorted)]
		public string[] ArrayUnsorted { get; }

		[EqualityHash]
		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Sorted)]
		public List<string> ListSorted { get; }

		[EqualityHash]
		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Unsorted)]
		public List<string> ListUnsorted { get; }

		[EqualityHash]
		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Sorted)]
		public IImmutableList<string> ReadonlyCollectionSorted { get; }

		[EqualityHash]
		[EqualityComparerOptions(CollectionMode = CollectionComparerMode.Unsorted)]
		public IImmutableList<string> ReadonlyCollectionUnsorted { get; }

		[EqualityHash]
		public IImmutableDictionary<string, string> ReadonlyDictionary { get; }

		[EqualityHash]
		public Dictionary<string, string> Dictionary { get; }
	}

}