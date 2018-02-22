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
using Uno.CodeGen.Tests.ExternalClass;
using Uno.Equality;

namespace Uno.CodeGen.Tests
{
	partial class Given_ImmutableEntity
	{
		[TestMethod]
		public void Immutable_When_Abstracted_Base_Class()
		{
			// var sut1 = InheritanceDerivedClass.Default.WithKeyValue(null);
		}
	}

	//public interface IImmutable<out TBuilder>
	//{
	//	TBuilder GetBuilder();
	//}

	[GeneratedImmutable]
	public abstract partial class InheritanceAbstractBaseClass<T> // : IImmutable<InheritanceAbstractBaseClass<T>.Builder>
		where T : IKeyEquatable<T>
	{
		[EqualityKey]
		public T KeyValue { get; }

		//public abstract TBuilder GetBuilder<TBuilder>() where TBuilder : Builder;

		//Builder IImmutable<Builder>.GetBuilder()
		//{
		//	throw new System.NotImplementedException();
		//}
	}

	//[GeneratedImmutable]
	//public abstract partial class InheritanceAbstractBaseClass2<T> : InheritanceAbstractBaseClass<T>
	//	where T : IKeyEquatable<T>
	//{
	//	//Builder IImmutable<InheritanceAbstractBaseClass2<T>.Builder>.GetBuilder()
	//	//{
	//	//	throw new System.NotImplementedException();
	//	//}

	//}

	[GeneratedImmutable]
	public partial class InheritanceHashedClass
	{
		[EqualityKey]
		public string Id { get; }
	}

	[GeneratedImmutable]
	public partial class InheritanceDerivedClass : InheritanceAbstractBaseClass<InheritanceHashedClass>
	{
		//public override TBuilder GetBuilder<TBuilder>() where TBuilder : Builder
		//{
		//	throw new System.NotImplementedException();
		//}
	}

	public partial class InheritanceDerivedClassFromExternal : AbstractExternalClass
	{

	}

	public partial class InheritanceDerivedClassFromExternalGeneric : AbstractExternalGenericClass<string>
	{

	}
}
