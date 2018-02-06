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
	/// Defines which attributes to ignore when copying to builder
	/// </summary>
	/// <remarks>
	/// Can be put on an assembly, a type or a property.
	/// </remarks>
	[System.AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
	public sealed class ImmutableAttributeCopyIgnoreAttribute : Attribute
	{
		/// <summary>
		/// Regex use to match the name (fullname) of the attribute type(s) to ignore.
		/// </summary>
		public string AttributeToIgnoreRegex { get; }

		public ImmutableAttributeCopyIgnoreAttribute(string attributeToIgnoreRegex)
		{
			AttributeToIgnoreRegex = attributeToIgnoreRegex;
		}
	}
}