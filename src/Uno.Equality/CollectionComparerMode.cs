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
namespace Uno.Equality
{
	/// <summary>
	/// Use to qualify the collection mode in <see cref="EqualityComparerOptionsAttribute"/> attribute.
	/// </summary>
	public enum CollectionComparerMode : int
	{
		/// <summary>
		/// No special mode for comparer
		/// </summary>
		Default = 0,
		
		/// <summary>
		/// Use a comparer which allows for a different ordering between collections.
		/// </summary>
		/// <remarks>
		/// This is not a flag, the flag is on `Sorted`
		/// </remarks>
		Unsorted = 0b0000,

		/// <summary>
		/// Use a comparer which checks the ordering in the collections.
		/// </summary>
		Sorted = 0b0001,

		/// <summary>
		/// Treat null collection and empty ones are equals.
		/// </summary>
		NullIsEmpty = 0b0010,
	}
}