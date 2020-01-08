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
using WithoutJsonRefs = Uno.CodeGen.Tests.MinimalDeps.JsonTestObj;
using WithJsonDisabled = Uno.CodeGen.Tests.JsonDisabled.JsonTestObj;
using STJ = System.Text.Json;
using NJ = Newtonsoft.Json;

namespace Uno.CodeGen.Tests
{
	public partial class Given_ImmutableEntity
	{
		#region Serialization
		private void AssertJsonSerialized(string json)
		{
			json.Should().BeEquivalentTo("{\"T\":null,\"Entity\":{\"MyField1\":4,\"MyField2\":75},\"IsSomething\":true}");
		}

		[TestMethod]
		public void Immutable_When_Serializing_WithoutJsonRefs_Using_SystemTextJson()
		{
			var json = STJ.JsonSerializer.Serialize(WithoutJsonRefs.Default);
			AssertJsonSerialized(json);
		}

		[TestMethod]
		public void Immutable_When_Serializing_WithoutJsonRefs_Using_JsonNet()
		{
			var json = NJ.JsonConvert.SerializeObject(WithoutJsonRefs.Default);
			AssertJsonSerialized(json);
		}

		[TestMethod]
		public void Immutable_When_Serializing_WithJsonDisabled_Using_SystemTextJson()
		{
			var json = STJ.JsonSerializer.Serialize(WithJsonDisabled.Default);
			AssertJsonSerialized(json);
		}

		[TestMethod]
		public void Immutable_When_Serializing_WithJsonDisabled_Using_JsonNet()
		{
			var json = NJ.JsonConvert.SerializeObject(WithJsonDisabled.Default);
			AssertJsonSerialized(json);
		}
		#endregion Serialization

		#region DeserializeBuilderWithoutChild
		[TestMethod]
		public void Immutable_When_Deserializing_WithoutJsonRefsBuilder_Using_SystemTextJson()
		{
			const string json = "{\"IsSomething\":false, \"T\":null}";
			var a = STJ.JsonSerializer.Deserialize<WithoutJsonRefs.Builder>(json).ToImmutable();
			a.Should().NotBeNull();
			a.IsSomething.Should().BeFalse();
			a.T.Should().BeNull();
			a.Entity.Should().NotBeNull();
			a.Entity­.MyField1.Should().Be(4);
			a.Entity­.MyField2.Should().Be(75);
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithoutJsonRefsBuilder_Using_JsonNet()
		{
			const string json = "{\"IsSomething\":false, \"T\":null}";
			var a = NJ.JsonConvert.DeserializeObject<WithoutJsonRefs.Builder>(json).ToImmutable();
			a.Should().NotBeNull();
			a.IsSomething.Should().BeFalse();
			a.T.Should().BeNull();
			a.Entity.Should().NotBeNull();
			a.Entity­.MyField1.Should().Be(4);
			a.Entity­.MyField2.Should().Be(75);
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithJsonDisabledBuilder_Using_SystemTextJson()
		{
			const string json = "{\"IsSomething\":false, \"T\":null}";
			var a = STJ.JsonSerializer.Deserialize<WithJsonDisabled.Builder>(json).ToImmutable();
			a.Should().NotBeNull();
			a.IsSomething.Should().BeFalse();
			a.T.Should().BeNull();
			a.Entity.Should().NotBeNull();
			a.Entity­.MyField1.Should().Be(4);
			a.Entity­.MyField2.Should().Be(75);
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithJsonDisabledBuilder_Using_JsonNet()
		{
			const string json = "{\"IsSomething\":false, \"T\":null}";
			var a = NJ.JsonConvert.DeserializeObject<WithJsonDisabled.Builder>(json).ToImmutable();
			a.Should().NotBeNull();
			a.IsSomething.Should().BeFalse();
			a.T.Should().BeNull();
			a.Entity.Should().NotBeNull();
			a.Entity­.MyField1.Should().Be(4);
			a.Entity­.MyField2.Should().Be(75);
		}
		#endregion DeserializeBuilderWithoutChild

		#region DeserializeBuilderWithChild
		[TestMethod]
		public void Immutable_When_Deserializing_WithoutJsonRefsBuilderAndChild_Using_SystemTextJson()
		{
			//System.Text.Json fails with exception to deserialize because the child property Entity is immutable and there is no converter
			const string json = "{\"IsSomething\":false, \"T\":null, \"Entity\":{\"MyField1\":1, \"MyField2\":2}}";
			json.Invoking(j => STJ.JsonSerializer.Deserialize<WithoutJsonRefs.Builder>(j))
				.Should().Throw<System.MissingMemberException>();
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithoutJsonRefsBuilderAndChild_Using_JsonNet()
		{
			//WARNING!!!!
			//Newtonsoft.Json silently leave default because child property Entity is immutable and there is no converter
			const string json = "{\"IsSomething\":false, \"T\":null, \"Entity\":{\"MyField1\":1, \"MyField2\":2}}";
			var a = NJ.JsonConvert.DeserializeObject<WithoutJsonRefs.Builder>(json).ToImmutable();
			a.Should().NotBeNull();
			a.IsSomething.Should().BeFalse();
			a.T.Should().BeNull();
			a.Entity.Should().NotBeNull();
			a.Entity­.MyField1.Should().Be(4);
			a.Entity­.MyField2.Should().Be(75);
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithJsonDisabledBuilderAndChild_Using_SystemTextJson()
		{
			//System.Text.Json fails with exception to deserialize because the child property Entity is immutable and there is no converter
			const string json = "{\"IsSomething\":false, \"T\":null, \"Entity\":{\"MyField1\":1, \"MyField2\":2}}";
			json.Invoking(j => STJ.JsonSerializer.Deserialize<WithJsonDisabled.Builder>(j))
				.Should().Throw<System.MissingMemberException>();
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithJsonDisabledBuilderAndChild_Using_JsonNet()
		{
			//WARNING!!!!
			//Newtonsoft.Json silently leave default because child property Entity is immutable and there is no converter
			const string json = "{\"IsSomething\":false, \"T\":null, \"Entity\":{\"MyField1\":1, \"MyField2\":2}}";
			var a = NJ.JsonConvert.DeserializeObject<WithJsonDisabled.Builder>(json).ToImmutable();
			a.Should().NotBeNull();
			a.IsSomething.Should().BeFalse();
			a.T.Should().BeNull();
			a.Entity.Should().NotBeNull();
			a.Entity­.MyField1.Should().Be(4);
			a.Entity­.MyField2.Should().Be(75);
		}
		#endregion DeserializeBuilderWithChild

		#region DeserializeImmutable
		[TestMethod]
		public void Immutable_When_Deserializing_WithoutJsonRefs_Using_SystemTextJson()
		{
			const string json = "{\"IsSomething\":false, \"T\":null, \"Entity\":{\"MyField1\":1, \"MyField2\":2}}";
			json.Invoking(j => STJ.JsonSerializer.Deserialize<WithoutJsonRefs>(j))
				.Should().Throw<System.MissingMemberException>();
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithoutJsonRefs_Using_JsonNet()
		{
			const string json = "{\"IsSomething\":false, \"T\":null, \"Entity\":{\"MyField1\":1, \"MyField2\":2}}";
			json.Invoking(j => NJ.JsonConvert.DeserializeObject<WithoutJsonRefs>(j))
				.Should().Throw<System.ArgumentNullException>();
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithJsonDisabled_Using_SystemTextJson()
		{
			const string json = "{\"IsSomething\":false, \"T\":null, \"Entity\":{\"MyField1\":1, \"MyField2\":2}}";
			json.Invoking(j => STJ.JsonSerializer.Deserialize<WithJsonDisabled>(j))
				.Should().Throw<System.MissingMemberException>();
		}

		[TestMethod]
		public void Immutable_When_Deserializing_WithJsonDisabled_Using_JsonNet()
		{
			const string json = "{\"IsSomething\":false, \"T\":null, \"Entity\":{\"MyField1\":1, \"MyField2\":2}}";
			json.Invoking(j => NJ.JsonConvert.DeserializeObject<WithJsonDisabled>(j))
				.Should().Throw<System.ArgumentNullException>();
		}
		#endregion DeserializeImmutable
	}
}
