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
	/// Exposes a way to inject a dependency resolver.
	/// </summary>
	/// <remarks>
	/// A partial class implementing <see cref="Uno.IInjectable"/> is generated for every class defining properties or fields attributed with <see cref="Uno.InjectAttribute"/>.
	/// </remarks>
	public interface IInjectable
	{
		/// <summary>
		/// Inject the dependency resolver used to populate properties and fields attributed with <see cref="Uno.InjectAttribute"/>.
		/// </summary>
		/// <remarks>
		/// You are responsible for calling this method and provide the appropriate dependency resolver.
		/// </remarks>
		/// <param name="resolver">The dependency resolver.</param>
		void Inject(DependencyResolver resolver);
	}
}