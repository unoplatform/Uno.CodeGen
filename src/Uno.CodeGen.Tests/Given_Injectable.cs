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
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.CodeGen.Tests.ExternalClasses;

namespace Uno.CodeGen.Tests
{
	[TestClass]
	public partial class Given_Injectable
	{
		[TestMethod]
		public void When_Injecting_Then_Injected()
		{
			var boolean = true;
			var integer = 42;
			var @string = "Hello world!";
			var dateTime = DateTime.Now;
			var providedName = default(string);

			var vm = new MyViewModel();

			(vm as IInjectable).Inject((type, name) =>
			{
				if (type == typeof(bool))
				{
					return boolean;
				}
				if (type == typeof(int))
				{
					return integer;
				}
				if (type == typeof(string))
				{
					return @string;
				}
				if (type == typeof(DateTime))
				{
					return dateTime;
				}
				if (type == typeof(object))
				{
					providedName = name;
					return name;
				}

				throw new NotSupportedException();
			});

			// Properties
			Assert.AreEqual(boolean, vm.MyBooleanProperty);
			Assert.AreEqual(integer, vm.MyIntegerProperty);
			Assert.AreEqual(@string, vm.MyStringProperty);
			Assert.AreEqual(dateTime, vm.MyDateTimeProperty);
			Assert.AreEqual(providedName, vm.MyObjectProperty);
			Assert.AreEqual(@string, vm.MyFuncStringProperty());

			// Fields
			Assert.AreEqual(boolean, vm.MyBooleanField);
			Assert.AreEqual(integer, vm.MyIntegerField);
			Assert.AreEqual(@string, vm.MyStringField);
			Assert.AreEqual(dateTime, vm.MyDateTimeField);
			Assert.AreEqual(providedName, vm.MyObjectField);
			Assert.AreEqual(@string, vm.MyFuncStringField());
		}
	}
}