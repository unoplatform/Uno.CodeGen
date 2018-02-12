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
	/// Global settings for [GenerateImmutable] generator.
	/// </summary>
	[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public sealed class ImmutableGenerationOptionsAttribute : Attribute
	{
		/// <summary>
		/// If you want arrays to be treat as immutable in the validation.
		/// </summary>
		/// <remarks>
		/// Recommendation is to set this to false if you want actually immutable entities.
		/// </remarks>
		public bool TreatArrayAsImmutable { get; set; } = false;

		/// <summary>
		/// If you want to generate `Option`-specific Code
		/// </summary>
		/// <remarks>
		/// No effect if not using `Uno.Core`.
		/// Default to true.
		/// </remarks>
		public bool GenerateOptionCode { get; set; } = true;

		/// <summary>
		/// If you want to generate equality by default.
		/// </summary>
		/// <remarks>
		/// Default is true. Can be overridden by type on the attribute declaration
		/// `[GenerateImmutable(GenerateEquality=XXX)]`
		/// </remarks>
		public bool GenerateEqualityByDefault { get; set; } = true;
	}
}