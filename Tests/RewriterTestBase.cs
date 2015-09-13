using System;
using System.Reflection;
using Aop.Definitions.ChangeTracking;
using Aop.Rewriter.MonoCecil;
using Mono.Cecil;
using Moq;
using NUnit.Framework;

namespace Aop.Tests
{
	[TestFixture]
	public abstract class RewriterTestBase
	{
		protected AssemblyDefinition assemblyDefinition;
		protected Mock<Action<object>> moqListener;

		[SetUp]
		public virtual void SetUp()
		{
			assemblyDefinition = MonoCecilHelpers.GetExecutingAssembly();
			moqListener = new Mock<Action<object>>();
		}

		protected object CreateInstance<T>(Assembly assembly, params object[] args)
		{
			return CreateInstance(assembly, typeof (T).FullName, args);
		}

		protected object CreateInstance(Assembly assembly, string fullname, params object[] args)
		{
			return assembly.CreateInstance(fullname, false, BindingFlags.Instance | BindingFlags.Public, null, args, null, null);
		}

		protected static T GetProperty<T>(object instance, string name)
		{
			return (T) instance.GetType().GetProperty(name).GetValue(instance, null);
		}

		protected TypeDefinition LoadType<T>()
		{
			return LoadType(typeof (T));
		}

		protected TypeDefinition LoadType(Type type)
		{
			return assemblyDefinition.MainModule.LoadType(type);
		}

		protected static void SetProperty(object instance, string name, object value)
		{
			instance.GetType()
				.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
				.SetValue(instance, value, null);
		}

		protected void AttachListenerTo(object instance)
		{
			((ITrackable) instance).Tracker.BeforeChange += moqListener.Object;
		}

		protected void DetachListenerFrom(object instance)
		{
			((ITrackable) instance).Tracker.BeforeChange -= moqListener.Object;
		}
	}
}