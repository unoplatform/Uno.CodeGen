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
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.CodeGen.ClassLifecycle
{
	internal class LifecycleMethods
	{
		public INamedTypeSymbol Owner { get; }
		public ICollection<IMethodSymbol> AllMethods { get; }
		public ICollection<IMethodSymbol> Constructors { get; }
		public ICollection<IMethodSymbol> Disposes { get; }
		public ICollection<IMethodSymbol> Finalizers { get; }
		public bool HasLifecycleMethods { get; }

		public LifecycleMethods(
			INamedTypeSymbol owner,
			ICollection<IMethodSymbol> allMethods,
			ICollection<IMethodSymbol> constructors,
			ICollection<IMethodSymbol> disposes,
			ICollection<IMethodSymbol> finalizers)
		{
			Owner = owner;
			AllMethods = allMethods;
			Constructors = constructors;
			Disposes = disposes;
			Finalizers = finalizers;

			HasLifecycleMethods = constructors.Count > 0 || disposes.Count > 0 || finalizers.Count > 0;
		}
	}
}