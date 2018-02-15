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
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.CodeGen.Tests
{
	[TestClass]
	public partial class Given_GeneratedEquality
	{
		// Check other files
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

}