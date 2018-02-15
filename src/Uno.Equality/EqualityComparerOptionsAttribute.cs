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
	/// Attribute to put on a collection field/property to specify the kind
	/// of collection comparer to use.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public class EqualityComparerOptionsAttribute : Attribute
	{
		/// <summary>
		/// Specify a special mode when it's a collection.
		/// </summary>
		/// <remarks>
		/// No effect on non-collection fields/properties
		/// </remarks>
		public CollectionComparerMode CollectionMode { get; set; } = CollectionComparerMode.Default;

		/// <summary>
		/// Specify a special mode when it's a string.
		/// </summary>
		/// <remarks>
		/// No effect on non-string fields/properties
		/// </remarks>
		public StringComparerMode StringMode { get; set; } = StringComparerMode.Default;
	}
}