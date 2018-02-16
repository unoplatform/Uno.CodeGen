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
using System.ComponentModel.DataAnnotations;

namespace Uno.CodeGen.Tests
{
	partial class Given_ImmutableEntity
	{
	}

	[GeneratedImmutable]
	public abstract partial class GenericFieldImmutable<T>
	{
		public T Entity { get; }
	}

	[GeneratedImmutable(GenerateEquality = false)]
	public partial class GenericToNonGenericDerivedImmutable : GenericFieldImmutable<MyImmutableEntity>
	{
	}

	[GeneratedImmutable]
	public partial class AnotherGenericToNonGenericDerivedImmutable : GenericFieldImmutable<GenericToNonGenericDerivedImmutable>
	{
	}

	[ImmutableAttributeCopyIgnore("RequiredAttribute")]
	[GeneratedImmutable()]
	public abstract partial class AbstractImmutableTypeWithManyGenerics<T1, T2, T3, T4, T5, T6>
		where T1 : MyImmutableEntity
		where T2 : T1
		where T3 : IReadOnlyCollection<T1>
	{
		[Required, System.ComponentModel.DataAnnotations.DataType(DataType.Text)]
		[System.ComponentModel.DataAnnotations.Key]
		[System.ComponentModel.DataAnnotations.RegularExpression("regex-pattern", ErrorMessage = "error-msg")]
		public string Id { get; }

		[Required(AllowEmptyStrings = true, ErrorMessage = "Entity1 is required!")]
		public T1 Entity1 { get; }
		[EqualityIgnore]
		public T2 Entity2 { get; }
		[EqualityIgnore]
		public T3 Entity3 { get; }
		[EqualityIgnore]
		public T4 Entity4 { get; }
		[EqualityIgnore]
		public T5 Entity5 { get; }
		[EqualityHash]
		public T6 Entity6 { get; }
		public (T1, T2, T3, T4, T5, T6) Values { get; }

		private static int GetHash_Entity6(T6 value) => 50;
	}

	public partial class ConcreteGenericImmutable<T> : AbstractImmutableTypeWithManyGenerics<MyImmutableEntity, MyImmutableEntity, MyImmutableEntity[], T, T, T>
		where T : IReadOnlyCollection<string>
	{
	}
}
