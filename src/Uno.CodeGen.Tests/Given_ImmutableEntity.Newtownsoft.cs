using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Uno.CodeGen.Tests
{
	partial class Given_ImmutableEntity
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
