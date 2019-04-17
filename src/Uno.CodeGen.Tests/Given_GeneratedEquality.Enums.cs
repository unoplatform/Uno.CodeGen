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

namespace Uno.CodeGen.Tests
{
	partial class Given_GeneratedEquality
	{

		[TestMethod]
		public void Equality_WithEnums()
		{
			var x1 = new MyClassWithEnums() {A = ByteEnum.No, B = SignedByteEnum.Present};
			var x2 = new MyClassWithEnums() {A = ByteEnum.No, B = SignedByteEnum.Present};
			var x3 = new MyClassWithEnums() {A = ByteEnum.Maybe, B = SignedByteEnum.Present};
			var x4 = new MyClassWithEnums() {A = ByteEnum.Maybe, B = SignedByteEnum.Present};
			var x5 = new MyClassWithEnums() {A = ByteEnum.Maybe, B = SignedByteEnum.Past};

			var hash1 = x1.GetHashCode();
			var hash2 = x2.GetHashCode();
			var hash3 = x3.GetHashCode();
			var hash4 = x4.GetHashCode();
			var hash5 = x5.GetHashCode();

			hash1.Should().Be(hash2);
			hash1.Should().NotBe(hash3);
			hash1.Should().NotBe(0);
			hash1.Should().NotBe(104729);
			hash3.Should().Be(hash4);
			hash3.Should().NotBe(hash5);
		}
	}
	internal enum ByteEnum : byte
	{
		Yes,
		No,
		Maybe,
		Always,
		Never
	}

	internal enum SignedByteEnum : sbyte
	{
		Past = -1,
		Present = 0,
		Future = 1
	}

	[GeneratedEquality]
	internal partial class MyClassWithEnums
	{
		[EqualityHash]
		internal ByteEnum A { get; set; }

		[EqualityHash]
		internal SignedByteEnum B { get; set; }
	}
}
