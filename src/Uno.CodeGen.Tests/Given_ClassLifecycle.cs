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
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Disposables;
using Uno.Extensions;

namespace Uno.CodeGen.Tests
{
	[TestClass]
	public partial class Given_ClassLifecycle
	{
		#region When_SimulateLifetimeOfObject_Then_AllMethodInvoked
		[TestMethod]
		public void When_SimulateLifetimeOfObject_Then_AllMethodInvoked()
		{
			var counter = Lifecycle(c => new When_SimulateLifetimeOfObject_Then_AllMethodInvoked_Subject(c));

			Assert.AreEqual(1, counter.Constructed);
			Assert.AreEqual(1, counter.Disposed);
			Assert.AreEqual(1, counter.Finalized);
		}

		private partial class When_SimulateLifetimeOfObject_Then_AllMethodInvoked_Subject : IDisposable
		{
			private readonly LifeTimeCounter _counter;

			public When_SimulateLifetimeOfObject_Then_AllMethodInvoked_Subject(LifeTimeCounter counter)
			{
				_counter = counter;
				Initialize();
			}

			[ConstructorMethod]
			private void MyConstructor() => _counter.Constructed++;

			[DisposeMethod]
			private void MyDispose() => _counter.Disposed++;

			[FinalizerMethod]
			private void MyFinalizer() => _counter.Finalized++;
		}
		#endregion

		#region When_InheritFromAnotherLifecycleObject_Then_AllMethodInvoked
		[TestMethod]
		public void When_InheritFromAnotherLifecycleObject_Then_AllMethodInvoked()
		{
			var counter = Lifecycle(c => new When_InheritFromAnotherLifecycleObject_Then_AllMethodInvoked_Subject(c));

			Assert.AreEqual(2, counter.Constructed);
			Assert.AreEqual(2, counter.Disposed);
			Assert.AreEqual(2, counter.Finalized);
		}

		private partial class When_InheritFromAnotherLifecycleObject_Then_AllMethodInvoked_Base : IDisposable
		{
			protected readonly LifeTimeCounter _counter;

			public When_InheritFromAnotherLifecycleObject_Then_AllMethodInvoked_Base(LifeTimeCounter counter)
			{
				_counter = counter;
				Initialize();
			}

			[ConstructorMethod]
			public void BaseConstructor() => _counter.Constructed++;

			[DisposeMethod]
			public void BaseDispose() => _counter.Disposed++;

			[FinalizerMethod]
			public void BaseFinalizer() => _counter.Finalized++;
		}

		private partial class When_InheritFromAnotherLifecycleObject_Then_AllMethodInvoked_Subject : When_InheritFromAnotherLifecycleObject_Then_AllMethodInvoked_Base
		{
			public When_InheritFromAnotherLifecycleObject_Then_AllMethodInvoked_Subject(LifeTimeCounter counter)
				: base(counter)
			{
				Initialize();
			}

			[ConstructorMethod]
			public void ChildConstructor() => _counter.Constructed++;

			[DisposeMethod]
			public void ChildDispose() => _counter.Disposed++;

			[FinalizerMethod]
			public void ChildFinalizer() => _counter.Finalized++;
		}
		#endregion

		#region When_RealConstructor_InvokesInitialize
		[TestMethod]
		public void When_RealConstructor_InvokesInitialize()
		{
			Assert.AreEqual(1, new When_RealConstructor_InvokesInitialize_Subject().Constructed);
		}

		private partial class When_RealConstructor_InvokesInitialize_Subject
		{
			public int Constructed { get; set; }

			public When_RealConstructor_InvokesInitialize_Subject() => Initialize();

			[ConstructorMethod]
			public void MyConstructor() => Constructed++;
		}
		#endregion

		#region When_RealConstructor_DoesNotInvokeInitialize_Then_Fails
		//// Compilation test
		//private partial class When_RealConstructor_DoesNotInvokeInitialize_Then_Fails
		//{
		//	public When_RealConstructor_DoesNotInvokeInitialize_Then_Fails()
		//	{
		//	}

		//	[ConstructorMethod]
		//	public void MyConstructor()
		//	{
		//	}
		//}
		#endregion

		#region When_RealConstructor_DoesNotInvokesInitialzie_With_InheritFromAnotherLifecycleObject_Then_Fails
		// Compilation test
		//private partial class When_RealConstructor_DoesNotInvokesInitialzie_With_InheritFromAnotherLifecycleObject_Then_Fails_Base
		//{
		//	[ConstructorMethod] private void MyConstructor() { }
		//}

		//private partial class When_RealConstructor_DoesNotInvokesInitialzie_With_InheritFromAnotherLifecycleObject_Then_Fails
		//	: When_RealConstructor_DoesNotInvokesInitialzie_With_InheritFromAnotherLifecycleObject_Then_Fails_Base
		//{
		//	public When_RealConstructor_DoesNotInvokesInitialzie_With_InheritFromAnotherLifecycleObject_Then_Fails()
		//		: base()
		//	{
		//	}
		//	[ConstructorMethod] private void MyConstructor() { }
		//}
		#endregion

		#region When_RealConstructor_InvokesParameterLessContructor
		[TestMethod]
		public void When_RealConstructor_InvokesParameterLessContructor()
		{
			Assert.AreEqual(1, new When_RealConstructor_InvokesParameterLessContructor_Subject("").Constructed);
			Assert.AreEqual(1, new When_RealConstructor_InvokesParameterLessContructor_Subject(0).Constructed);
		}

		private partial class When_RealConstructor_InvokesParameterLessContructor_Subject
		{
			public int Constructed { get; private set; }

			public When_RealConstructor_InvokesParameterLessContructor_Subject(string test)
				: this()
			{
			}

			public When_RealConstructor_InvokesParameterLessContructor_Subject(int integer)
				: this(integer.ToString())
			{
			}

			[ConstructorMethod]
			public void MyConstructor() => Constructed++;
		}
		#endregion

		#region When_Constructor_With_NullableParameter
		// Compilation test
		private partial class When_Constructor_With_NullableParameter
		{
			[ConstructorMethod]
			public void MyConstructor(int? value)
			{
			}
		}
		#endregion

		#region When_Constructor_With_NullableOptionalParameter
		// Compilation test
		private partial class When_Constructor_With_NullableOptionalParameter
		{
			[ConstructorMethod]
			public void MyConstructor(int? value = null)
			{
			}
		}
		#endregion

		#region When_Constructor_With_OptionalParameter
		// Compilation test
		private partial class When_Constructor_With_OptionalParameter
		{
			[ConstructorMethod]
			public void MyConstructor(When_Constructor_With_OptionalParameter value = null)
			{
			}
		}
		#endregion

		#region When_Constructor_With_OptionalParameter_And_DefaultValue_Then_DefaultValueCopied
		[TestMethod]
		public void When_Constructor_With_OptionalParameter_And_DefaultValue_Then_DefaultValueCopied()
		{
			Assert.AreEqual(5, new When_Constructor_With_OptionalParameter_And_DefaultValue_Then_DefaultValueCopied_Subject().Value.GetValueOrDefault());
		}

		private partial class When_Constructor_With_OptionalParameter_And_DefaultValue_Then_DefaultValueCopied_Subject
		{
			[ConstructorMethod]
			public void MyConstructor(int? value = 5)
			{
				Value = value;
			}

			public int? Value { get; set; }
		}
		#endregion

		#region When_Constructors_With_Parameters_Then_ParametersAggregatedOnInitialize
		// Compilation test
		private partial class When_Constructors_With_Parameters_Then_ParametersAggregatedOnInitialize
		{
			public When_Constructors_With_Parameters_Then_ParametersAggregatedOnInitialize()
			{
				Initialize(integer: 0, str: "");
			}

			[ConstructorMethod]
			public void MyConstructor1(string str)
			{
			}

			[ConstructorMethod]
			public void MyConstructor2(int integer)
			{
			}
		}
		#endregion

		#region When_Constructors_With_Parameters_And_TypeMismatch_Then_Fails
		//// Compilation test
		//private partial class When_Constructors_With_Parameters_And_TypeMismatch_Then_Fails
		//{
		//	[ConstructorMethod]
		//	public void MyConstructor1(string value)
		//	{
		//	}

		//	[ConstructorMethod]
		//	public void MyConstructor2(int value)
		//	{
		//	}
		//}
		#endregion

		#region When_Constructors_With_OptionalParameters_And_DefaultValueMismatch_Then_Fails
		//// Compilation test
		//private partial class When_Constructors_With_OptionalParameters_And_DefaultValueMismatch_Then_Fails
		//{
		//	[ConstructorMethod]
		//	public void MyConstructor1(int value = 5)
		//	{
		//	}

		//	[ConstructorMethod]
		//	public void MyConstructor2(int value = 6)
		//	{
		//	}
		//}
		#endregion

		#region When_Dispose_With_Parameter_Then_Fails
		//// Compilation test
		//private partial class When_Dispose_With_Parameter_Then_Fails
		//{
		//	[DisposeMethod]
		//	private void MyDispose(string text) { }
		//}
		#endregion

		#region When_Dispose_With_Result_Then_Fails
		//// Compilation test
		//private partial class When_Dispose_With_Result_Then_Fails
		//{
		//	[DisposeMethod]
		//	private string MyDispose(string text) => "";
		//}
		#endregion

		#region When_Dispose_And_InheritFromDisposablePattern_Then_ChildGetDispose
		[TestMethod]
		public void When_Dispose_And_InheritFromDisposablePattern_Then_ChildGetDispose()
		{
			var sut = new When_Dispose_And_InheritFromDisposablePattern_Then_ChildGetDispose_Subject();

			sut.Dispose();

			Assert.AreEqual(1, sut.Disposed);
		}

		private class DisposablePatternBase : IDisposable
		{
			protected virtual void Dispose(bool disposing)
			{
			}

			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			~DisposablePatternBase() => Dispose(false);
		}

		private partial class When_Dispose_And_InheritFromDisposablePattern_Then_ChildGetDispose_Subject : DisposablePatternBase
		{
			public int Disposed { get; private set; }

			[DisposeMethod]
			public void ChildDispose() => Disposed++;
		}
		#endregion

		#region When_Dispose_And_InheritFromSimpleDisposable_With_Override_Then_ChildGetDispose
		[TestMethod]
		public void When_Dispose_And_InheritFromSimpleDisposable_With_Override_Then_ChildGetDispose()
		{
			var sut = new When_Dispose_And_InheritFromSimpleDisposable_With_Override_Then_ChildGetDispose_Subject();

			sut.Dispose();

			Assert.AreEqual(1, sut.Disposed);
		}

		private class SimpleVirtualDisposableBaseClass : IDisposable
		{
			public virtual void Dispose()
			{
			}
		}

		private partial class When_Dispose_And_InheritFromSimpleDisposable_With_Override_Then_ChildGetDispose_Subject : SimpleVirtualDisposableBaseClass
		{
			public int Disposed { get; private set; }

			[DisposeMethod]
			public void ChildDispose() => Disposed++;
		}
		#endregion

		#region When_Dispose_And_InheritFromSimpleDisposable_Without_Override_Then_Fails
		//// Compilation test
		//private class SimpleNonOverridableDisposableBaseClass : IDisposable { public void Dispose() { } }
		//private partial class When_Dispose_And_InheritFromSimpleDisposable_Without_Override_Then_Fails : SimpleNonOverridableDisposableBaseClass
		//{
		//	public int Disposed { get; private set; }
		//	[DisposeMethod] public void ChildDispose() => Disposed++;
		//}
		#endregion

		#region When_Dispose_And_InheritFromExtensibleDisposable_Then_ChildGetDispose
		[TestMethod]
		public void When_Dispose_And_InheritFromExtensibleDisposable_Then_ChildGetDispose()
		{
			var sut = new When_Dispose_And_InheritFromExtensibleDisposable_Then_ChildGetDispose_Subject();

			sut.Dispose();

			Assert.AreEqual(1, sut.Disposed);
		}

		private class ExtensibleDisposable : IExtensibleDisposable
		{
			private readonly List<IDisposable> _extensions = new List<IDisposable>();

			public void Dispose() => _extensions.DisposeAll();

			public IReadOnlyCollection<object> Extensions => _extensions;

			public IDisposable RegisterExtension<T>(T extension) where T : class, IDisposable => _extensions.DisposableAdd(extension);
		}

		private partial class When_Dispose_And_InheritFromExtensibleDisposable_Then_ChildGetDispose_Subject : ExtensibleDisposable
		{
			public int Disposed { get; private set; }

			[DisposeMethod]
			public void ChildDispose() => Disposed++;
		}
		#endregion

		#region When_Dispose_And_InheritFromExtensibleDisposable_With_ExplicitImplementation_Then_ChildGetDispose
		[TestMethod]
		public void When_Dispose_And_InheritFromExtensibleDisposable_With_ExplicitImplementation_Then_ChildGetDispose()
		{
			var sut = new When_Dispose_And_InheritFromExtensibleDisposable_With_ExplicitImplementation_Then_ChildGetDispose_Subject();

			((IDisposable)sut).Dispose();

			Assert.AreEqual(1, sut.Disposed);
		}

		private class ExtensibleDisposableExplicit : IExtensibleDisposable
		{
			private readonly List<IDisposable> _extensions = new List<IDisposable>();

			void IDisposable.Dispose() => _extensions.DisposeAll();

			IReadOnlyCollection<object> IExtensibleDisposable.Extensions => _extensions;

			IDisposable IExtensibleDisposable.RegisterExtension<T>(T extension) => _extensions.DisposableAdd(extension);
		}

		private partial class When_Dispose_And_InheritFromExtensibleDisposable_With_ExplicitImplementation_Then_ChildGetDispose_Subject : ExtensibleDisposableExplicit
		{
			public int Disposed { get; private set; }

			[DisposeMethod]
			public void ChildDispose() => Disposed++;
		}
		#endregion

		#region When_Finalizer_With_Parameter_Then_Fails
		//// Compilation test
		//private partial class When_Finalizer_With_Parameter_Then_Fails
		//{
		//	[FinalizerMethod]
		//	private void MyFinalizer(string text) { }
		//}
		#endregion

		#region When_Finalizer_With_Result_Then_Fails
		//// Compilation test
		//private partial class When_Finalizer_With_Result_Then_Fails
		//{
		//	[FinalizerMethod]
		//	private string MyFinalizer(string text) => "";
		//}
		#endregion

		#region -- Helpers --
		private System.WeakReference<When_SimulateLifetimeOfObject_Then_AllMethodInvoked_Subject> Create(LifeTimeCounter counter)
		{
			using (var sut = new When_SimulateLifetimeOfObject_Then_AllMethodInvoked_Subject(counter))
			{
				return new System.WeakReference<When_SimulateLifetimeOfObject_Then_AllMethodInvoked_Subject>(sut);
			}
		}

		private System.WeakReference<T> Create<T>(LifeTimeCounter counter, Func<LifeTimeCounter, T> factory)
			where T : class, IDisposable
		{
			using (var sut = factory(counter))
			{
				return new System.WeakReference<T>(sut);
			}
		}

		private LifeTimeCounter Lifecycle<T>(Func<LifeTimeCounter, T> factory)
			where T : class, IDisposable
		{
			var counter = new LifeTimeCounter();
			var sut = Create(counter, factory);

			GC.Collect(3, GCCollectionMode.Forced);
			GC.WaitForPendingFinalizers();
			GC.Collect(3, GCCollectionMode.Forced);
			GC.WaitForPendingFinalizers();

			Assert.IsFalse(sut.TryGetTarget(out var _));

			return counter;
		}

		private class LifeTimeCounter
		{
			public int Constructed { get; set; }

			public int Disposed { get; set; }

			public int Finalized { get; set; }
		}
		#endregion
	}
}
