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
using System.Collections.Generic;
using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Equality;

namespace Uno.CodeGen.Tests
{
	public partial class Given_GeneratedEquality
	{
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
