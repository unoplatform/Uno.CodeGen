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
	/// Indicates that the attributed property or field should be injected.
	/// </summary>
	/// <remarks>
	/// For partial class implementing <see cref="Uno.IInjectable" /> is generated for every class that defines properties or fields attributed with <see cref="Uno.InjectAttribute"/>.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
	public sealed class InjectAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Uno.InjectAttribute" /> class.
		/// </summary>
		/// <param name="name">The optional name used to resolve the injected instance.</param>
		public InjectAttribute(string name = null)
		{
			Name = name;
		}

		/// <summary>
		/// The optional name used to resolve the injected instance.
		/// </summary>
		public string Name { get; }
	}
}
