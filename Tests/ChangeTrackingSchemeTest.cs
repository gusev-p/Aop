using System;
using System.Linq;
using Kontur.Elba.Aop.Definitions.ChangeTracking;
using Kontur.Elba.Aop.Rewriter;
using Kontur.Elba.Aop.Rewriter.ChangeTracking;
using NUnit.Framework;

namespace Kontur.Elba.Aop.Tests
{
	public class ChangeTrackingSchemeTest : RewriterTestBase
	{
		private ChangeTrackingScheme scheme;
		private ChangeTrackingParameters parameters;

		[SetUp]
		public override void SetUp()
		{
			base.SetUp();
			parameters = new ChangeTrackingParameters {ProcessNestedTypes = true};
			scheme = new ChangeTrackingScheme(parameters);
		}

		[Test]
		public void SimpleTypes_NoNeedToTrackPropertyType()
		{
			Assert.That(scheme.NeedTrackPropertyType(LoadType<string>()), Is.False);
			Assert.That(scheme.NeedTrackPropertyType(LoadType<int>()), Is.False);
			Assert.That(scheme.NeedTrackPropertyType(LoadType<DateTime>()), Is.False);
		}

		[Test]
		public void ChangeTracker_NoNeedToTrackPropertyType()
		{
			Assert.That(scheme.NeedTrackPropertyType(LoadType<ChangeTracker>()), Is.False);
		}

		public enum UserType
		{
			Main,
			Manager
		}

		[Test]
		public void Enum_NoNeedToTrackPropertyType()
		{
			Assert.That(scheme.NeedTrackPropertyType(LoadType<UserType>()), Is.False);
		}

		public struct SomeValueType
		{
			public string value;
		}

		[Test]
		public void ValueType_NoNeedToTrackPropertyType()
		{
			Assert.That(scheme.NeedTrackPropertyType(LoadType<SomeValueType>()), Is.False);
		}

		public class ClassWithNullableProperty
		{
			public DateTime? Date { get; set; }
		}

		[Test]
		public void NullableInt_NoNeedToTrackPropertyType()
		{
			Assert.That(scheme.NeedTrackPropertyType(LoadType<ClassWithNullableProperty>().Properties.Single().PropertyType.Resolve()), Is.False);
		}

		[Test]
		public void Interface_NoNeedToTrackPropertyType()
		{
			Assert.That(scheme.NeedTrackPropertyType(LoadType<IInterface>()), Is.False);
		}

		public class User
		{
			public UserType UserType { get; set; }
		}

		[Test]
		public void SimpleClass_NeedToTrackPropertyType()
		{
			Assert.That(scheme.NeedTrackPropertyType(LoadType<User>()), Is.True);
		}

		public class NestedClass
		{
		}

		[Test]
		public void Nested_NoNeedToTrackTypeReference()
		{
			Assert.That(scheme.NeedTrackType(LoadType<NestedClass>()), Is.True);
			parameters.ProcessNestedTypes = false;
			Assert.That(scheme.NeedTrackType(LoadType<NestedClass>()), Is.False);
		}

		[Test]
		public void ValueType_NoNeedToTrackTypeReference()
		{
			Assert.That(scheme.NeedTrackType(LoadType<SomeValueType>()), Is.False);
		}

		private int someField;

		[Test]
		public void CompilerGeneratedType_NoNeedToTrackTypeReference()
		{
			var value = someField + 42;
			Action someDelegate = delegate { someField = 42 + value; };
			Assert.That(scheme.NeedTrackType(LoadType(someDelegate.Target.GetType())), Is.False);
		}

		public interface IInterface
		{
		}

		[Test]
		public void Interface_NoNeedToTrackTypeReference()
		{
			Assert.That(scheme.NeedTrackType(LoadType<IInterface>()), Is.False);
		}
	}
}