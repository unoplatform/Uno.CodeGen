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
using Newtonsoft.Json;

namespace Uno.CodeGen.Tests
{
	public partial class Given_ImmutableEntity
	{
		[TestMethod]
		public void Immutable_When_Serializing_A_Using_JsonNet()
		{
			var json = JsonConvert.SerializeObject(A.Default.WithEntity(x => null).ToImmutable());

			json.Should().BeEquivalentTo("{\"T\":null,\"Entity\":null,\"IsSomething\":true,\"Metadata\":null}");
		}

		[TestMethod]
		public void Immutable_When_Serializing_ABuilder_Using_JsonNet()
		{
			var json = JsonConvert.SerializeObject(A.Default.WithEntity(x => null));

			json.Should().BeEquivalentTo("{\"T\":null,\"Entity\":null,\"IsSomething\":true,\"Metadata\":null}");
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
	}
}
