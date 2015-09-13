using System.Linq;
using Kontur.Elba.Aop.Definitions.ChangeTracking;
using Kontur.Elba.Aop.Rewriter.ChangeTracking;
using Kontur.Elba.Aop.Rewriter.MonoCecil;
using Moq;
using NUnit.Framework;

namespace Kontur.Elba.Aop.Tests
{
	public abstract class ChangeTrackingTypeWeaverTest : RewriterTestBase
	{
		private ChangeTrackingTypeWeaver weaver;

		public override void SetUp()
		{
			base.SetUp();
			weaver = new ChangeTrackingTypeWeaver(assemblyDefinition);
		}

		public class ImplementTrackableTest : ChangeTrackingTypeWeaverTest
		{
			public class Person
			{
				public string Name { get; set; }
			}

			[Test]
			public void Test()
			{
				weaver.ImplementTrackable(LoadType<Person>());
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<Person>(assembly);
				Assert.That(instance, Is.InstanceOf<ITrackable>());
				var patchedPersonTrackable = instance as ITrackable;
				Assert.That(patchedPersonTrackable.Tracker, Is.Not.Null);
			}
		}

		public class ImplementTrackableWhenManyConstructors : ChangeTrackingTypeWeaverTest
		{
			public class Person
			{
				public Person()
					: this("default")
				{
				}

				public Person(string name)
				{
					ConstructorTracker = (this as ITrackable).Tracker;
					Name = name;
				}

				public ChangeTracker ConstructorTracker { get; set; }
				public string Name { get; set; }
			}

			[Test]
			public void Test()
			{
				weaver.ImplementTrackable(LoadType<Person>());
				var assembly = assemblyDefinition.ToAssembly();
				var instance1 = CreateInstance<Person>(assembly);
				var instance2 = CreateInstance<Person>(assembly, "test");
				var tracker1 = GetProperty<ChangeTracker>(instance1, "ConstructorTracker");
				var tracker2 = GetProperty<ChangeTracker>(instance2, "ConstructorTracker");
				Assert.That(tracker2, Is.SameAs(((ITrackable)instance2).Tracker));
				Assert.That(tracker1, Is.SameAs(((ITrackable) instance1).Tracker));
				
			}
		}

		public class ObservePropertyTest : ChangeTrackingTypeWeaverTest
		{
			public class Person
			{
				public string Name { get; set; }
			}

			[Test]
			public void Test()
			{
				var personType = LoadType<Person>();
				var trackerField = weaver.ImplementTrackable(personType);
				weaver.TryObserveProperty(personType.Properties.Single(x => x.Name == "Name"), trackerField);
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<Person>(assembly);
				AttachListenerTo(instance);
				moqListener.Verify(x => x(instance), Times.Never());
				SetProperty(instance, "Name", "vasya");
				moqListener.Verify(x => x(instance), Times.Once());
			}
		}

		public class ObserveValueTypePropertyTest : ChangeTrackingTypeWeaverTest
		{
			public class Person
			{
				public int IntValue { get; set; }
			}

			[Test]
			public void Test()
			{
				var personType = LoadType<Person>();
				var trackerField = weaver.ImplementTrackable(personType);
				weaver.TryObserveProperty(personType.Properties.Single(x => x.Name == "IntValue"), trackerField);
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<Person>(assembly);
				AttachListenerTo(instance);
				moqListener.Verify(x => x(instance), Times.Never());
				SetProperty(instance, "IntValue", 42);
				moqListener.Verify(x => x(instance), Times.Once());
				SetProperty(instance, "IntValue", 42);
				moqListener.Verify(x => x(instance), Times.Once());
				SetProperty(instance, "IntValue", 43);
				moqListener.Verify(x => x(instance), Times.Exactly(2));
			}
		}

		public class ExcludeStaticConstructors: ChangeTrackingTypeWeaverTest
		{
			public class Person
			{
				private static readonly string[] cityRegions = new[] {"МОСКВА", "САНКТ-ПЕТЕРБУРГ", "БАЙКОНУР"};
				public string Name { get; set; }
			}

			[Test]
			public void Test()
			{
				weaver.ImplementTrackable(LoadType<Person>());
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<Person>(assembly);
				Assert.That(instance, Is.InstanceOf<ITrackable>());
				var patchedPersonTrackable = instance as ITrackable;
				Assert.That(patchedPersonTrackable.Tracker, Is.Not.Null);
			}
		}

		public class ObservePropertyFromChild : ChangeTrackingTypeWeaverTest
		{
			public class Person
			{
				public string Name { get; set; }
			}

			public class Worker : Person
			{
				public decimal Salary { get; set; }
			}

			[Test]
			public void Test()
			{
				var personType = LoadType<Person>();
				var trackerField = weaver.ImplementTrackable(personType);
				var workerType = LoadType<Worker>();
				weaver.TryObserveProperty(personType.Properties.Single(x => x.Name == "Name"), trackerField);
				weaver.TryObserveProperty(workerType.Properties.Single(x => x.Name == "Salary"), trackerField);
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<Worker>(assembly);

				AttachListenerTo(instance);
				moqListener.Verify(x => x(instance), Times.Never());
				SetProperty(instance, "Name", "vasya");
				moqListener.Verify(x => x(instance), Times.Once());
				SetProperty(instance, "Salary", 123m);
				moqListener.Verify(x => x(instance), Times.Exactly(2));
			}
		}
	}
}