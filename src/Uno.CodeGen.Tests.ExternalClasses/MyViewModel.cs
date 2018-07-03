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

namespace Uno.CodeGen.Tests.ExternalClasses
{
	public partial class MyViewModel
	{
		// Properties
		[Inject] public string MyStringProperty { get; private set; }
		[Inject] public bool MyBooleanProperty { get; private set; }
		[Inject] public int MyIntegerProperty { get; private set; }
		[Inject] public DateTime MyDateTimeProperty { get; private set; }
		[Inject("name")] public object MyObjectProperty { get; private set; }
		[Inject] public Func<string> MyFuncStringProperty { get; private set; }
		[Inject] public List<string> MyListStringProperty { get; private set; }

		// Fields
		[Inject] public string MyStringField;
		[Inject] public bool MyBooleanField;
		[Inject] public int MyIntegerField;
		[Inject] public DateTime MyDateTimeField;
		[Inject("name")] public object MyObjectField;
		[Inject] public Func<string> MyFuncStringField;
		[Inject] public List<string> MyListStringField;
	}
}
