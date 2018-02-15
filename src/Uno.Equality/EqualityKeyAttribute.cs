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
	/// Define a field/property to use for generating the <see cref="object.GetHashCode"/> method.
	/// </summary>
	/// <remarks>
	/// Use in conjonction with <see cref="GeneratedEqualityAttribute"/>.
	/// * If this attribute is not used, you must manually define a <see cref="object.GetHashCode"/> attribute
	/// * You can put this attribute to more than one member of your class. They will all be used for HashCode calculation.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public class EqualityKeyAttribute : Attribute
	{
		/// <summary>
		/// Override for equality mode
		/// </summary>
		public KeyEqualityMode Mode { get; }

		/// <summary />
		public EqualityKeyAttribute(KeyEqualityMode mode = KeyEqualityMode.Auto)
		{
			Mode = mode;
		}
	}
}