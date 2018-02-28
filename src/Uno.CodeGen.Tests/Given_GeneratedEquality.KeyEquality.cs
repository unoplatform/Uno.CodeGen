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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Equality;

namespace Uno.CodeGen.Tests
{
	public partial class Given_GeneratedEquality
	{
		[TestMethod]
		public void KeyEquality_WhenUsingKeyEqualityMode()
		{
			var e1 = new KeyEqualityWrapper.Builder
			{
				IdField = new KeyEqualityId.Builder {Id = "a", Name = "n"},
				NonKeyField = "nkf1"
			}.ToImmutable();

			var e2 = new KeyEqualityWrapper.Builder
			{
				IdField = new KeyEqualityId.Builder { Id = "a", Name = "n-bis" },
				NonKeyField = "nkf2"
			}.ToImmutable();

			e1.Equals(e2).Should().BeFalse();
			e1.KeyEquals(e2).Should().BeTrue();
		}

		[TestMethod]
		public void KeyEquality_WhenUsingEqualityMode()
		{
			var e1 = new EqualityWrapper.Builder
			{
				IdField = new KeyEqualityId.Builder { Id = "a", Name = "n" },
				NonKeyField = "nkf1"
			}.ToImmutable();

			var e2 = new EqualityWrapper.Builder
			{
				IdField = new KeyEqualityId.Builder { Id = "a", Name = "n-bis" },
				NonKeyField = "nkf2"
			}.ToImmutable();

			e1.Equals(e2).Should().BeFalse();
			e1.KeyEquals(e2).Should().BeFalse();
			e1.IdField.KeyEquals(e2.IdField).Should().BeTrue();
		}

		//[TestMethod]
		//public void KeyEquality_WhenUsingKeyEqualityMode_OnAbstractClass()
		//{
		//	var e1 = new KeyEqualityConcreteWrapper.Builder
		//	{
		//		IdField = new KeyEqualityId.Builder { Id = "a", Name = "n" },
		//		NonKeyField = "nkf1"
		//	}.ToImmutable();

		//	var e2 = new KeyEqualityWrapper.Builder
		//	{
		//		IdField = new KeyEqualityConcreteWrapper.Builder { Id = "a", Name = "n-bis" },
		//		NonKeyField = "nkf2"
		//	}.ToImmutable();

		//	e1.Equals(e2).Should().BeFalse();
		//	e1.KeyEquals(e2).Should().BeTrue();
		//}
	}

	[GeneratedImmutable]
	public partial class KeyEqualityWrapper
	{
		[EqualityKey]
		public KeyEqualityId IdField { get; }

		public string NonKeyField { get; }
	}

	[GeneratedImmutable]
	public partial class EqualityWrapper
	{
		[EqualityKey(KeyEqualityMode.UseEquality)]
		public KeyEqualityId IdField { get; }

		public string NonKeyField { get; }
	}

	[GeneratedImmutable]
	public partial class KeyEqualityId
	{
		[EqualityKey]
		public string Id { get; }

		public string Name { get; }
	}

	[GeneratedImmutable]
	public abstract partial class KeyEqualityAbstractWrapper<T>
		where T:IKeyEquatable<T>
	{
		[EqualityKey]
		public T IdField { get; }

		public string NonKeyField { get; }
	}

	[GeneratedImmutable]
	public abstract partial class KeyEqualityConcreteAbstractWrapper : KeyEqualityAbstractWrapper<KeyEqualityId>
	{
		public string NonKeyField2 { get; }
	}

	[GeneratedImmutable]
	public partial class KeyEqualityConcreteWrapper : KeyEqualityConcreteAbstractWrapper
	{
		public string NonKeyField3 { get; }
	}
}
