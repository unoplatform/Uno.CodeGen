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
}
