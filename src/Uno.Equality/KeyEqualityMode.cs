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
	/// Mode to use for <see cref="EqualityKeyAttribute"/>
	/// </summary>
	public enum KeyEqualityMode
	{
		/// <summary>
		/// Will use the KeyEquality if found on type, fallback to normal equality
		/// </summary>
		/// <remarks>
		/// This is the default mode.
		/// </remarks>
		Auto,

		/// <summary>
		/// Delegate the key equality to the _KeyEquality_ of the member type
		/// </summary>
		/// <remarks>
		/// Will fail if type is not implementing KeyEquality. If you're not sure
		/// which you want, use `Auto`.
		/// </remarks>
		UseKeyEquality,

		/// <summary>
		/// Delegate the key equality to the _Equality_ of the member type.
		/// </summary>
		UseEquality
	}
}