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

namespace Uno
{
	/// <summary>
	/// Indicates the type of the builder for this class.
	/// The type must implement <see cref="IImmutableBuilder{TImmutable}"/>
	/// </summary>
	/// <remarks>
	/// This attribute is added on generated code and is also used by some code generators
	/// to find the builder to use for creating the entity.
	/// </remarks>
	[System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public sealed class ImmutableBuilderAttribute : Attribute
	{
		/// <summary>
		/// Type to use to build this entity.
		/// </summary>
		/// <remarks>
		/// The type must implement <see cref="IImmutableBuilder{TImmutable}"/>.
		/// </remarks>
		public Type BuilderType { get; }

		/// <summary />
		public ImmutableBuilderAttribute(Type builderType)
		{
			BuilderType = builderType;
		}
	}
}