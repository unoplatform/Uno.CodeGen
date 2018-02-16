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
