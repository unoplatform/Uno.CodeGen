using System.Collections.Immutable;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.CodeGen.Tests
{
	partial class Given_ImmutableEntity
	{
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
	}
}
