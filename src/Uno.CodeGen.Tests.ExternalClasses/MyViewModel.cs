using System;

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

		// Fields
		[Inject] public string MyStringField;
		[Inject] public bool MyBooleanField;
		[Inject] public int MyIntegerField;
		[Inject] public DateTime MyDateTimeField;
		[Inject("name")] public object MyObjectField;
	}
}
