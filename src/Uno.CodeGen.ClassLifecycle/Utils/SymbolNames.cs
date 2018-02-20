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
using System.Linq;

namespace Uno.CodeGen.ClassLifecycle.Utils
{
	internal class SymbolNames
	{
		public SymbolNames(string name, string genericArguments, string nameWithGenerics, string forXml, string nameDefinition, string filename, string filePath)
		{
			Name = name;
			GenericArguments = genericArguments;
			NameWithGenerics = nameWithGenerics;
			ForXml = forXml;
			NameDefinition = nameDefinition;
			Filename = filename;
			FilePath = filePath;
		}

		public string Name { get; }

		/// <summary>
		/// MyType&lt;T1, T2&gt;
		/// </summary>
		public string NameWithGenerics { get; } // MyType<T1, T2>

		/// <summary>
		///  MyType&lt;,&gt;
		/// </summary>
		public string NameDefinition { get; } // MyType<,>

		/// <summary>
		/// &lt;T1, T2&gt;
		/// </summary>
		public string GenericArguments { get; } // <T1, T2>

		/// <summary>
		/// MyType&amp;lt;T1, T2&amp;gt;
		/// </summary>
		public string ForXml { get; } // MyType&lt;T1, T2&gt;

		/// <summary>
		/// MyType_T1_T2
		/// </summary>
		public string Filename { get; } // MyType_T1_T2

		public string FilePath { get; }
	}
}