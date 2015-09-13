using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kontur.Elba.Aop.Definitions.ChangeTracking;
using Kontur.Elba.Aop.Rewriter;
using Kontur.Elba.Aop.Rewriter.ChangeTracking;
using Kontur.Elba.Aop.Rewriter.MonoCecil;
using Mono.Cecil;
using Moq;
using NUnit.Framework;

namespace Kontur.Elba.Aop.Tests
{
	//todo не забивать на массивы
	public abstract class ChangeTrackingRewriterTest : RewriterTestBase
	{
		private ChangeTrackingRewriter rewriter;
		private ChangeTrackingParameters parameters;

		public override void SetUp()
		{
			base.SetUp();
			PurgeTempFiles();
			parameters = new ChangeTrackingParameters
				{
					Assembly = assemblyDefinition,
					TypesToProcess = LoadType(GetType()).NestedTypes.ToArray(),
					ProcessNestedTypes = true
				};
			rewriter = new ChangeTrackingRewriter(parameters);
		}

		[TearDown]
		public void TearDown()
		{
			PurgeTempFiles();
		}

		private static void PurgeTempFiles()
		{
			foreach (var tempFileName in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "tmp*"))
				File.Delete(tempFileName);
		}

		public class SingleClassMarkedWithTrackChanges : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Contractor
			{
				public string Name { get; set; }
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<Contractor>(assembly);
				AttachListenerTo(instance);
				SetProperty(instance, "Name", "ivan");
				moqListener.Verify(x => x(instance), Times.Once());
				SetProperty(instance, "Name", "vasyly");
				moqListener.Verify(x => x(instance), Times.Exactly(2));
			}
		}

		public class TwoClassesMarkedWithTrackChanges : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Contractor
			{
				public string Name { get; set; }
			}

			[TrackChanges]
			public class Document
			{
				public string Number { get; set; }
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();

				var contractorInstance = CreateInstance<Contractor>(assembly);
				AttachListenerTo(contractorInstance);

				var documentInstance = CreateInstance<Document>(assembly);
				AttachListenerTo(documentInstance);

				SetProperty(contractorInstance, "Name", "ivan");
				moqListener.Verify(x => x(contractorInstance), Times.Once());
				SetProperty(documentInstance, "Number", "123");
				moqListener.Verify(x => x(documentInstance), Times.Once());
			}
		}

		public class SkipClassesNotMarkedAsTrackChanges : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Contractor
			{
				public string Name { get; set; }
			}

			public class Document
			{
				public string Number { get; set; }
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();

				var contractorInstance = CreateInstance<Contractor>(assembly);
				AttachListenerTo(contractorInstance);

				var documentInstance = CreateInstance<Document>(assembly);
				Assert.That(documentInstance, Is.Not.AssignableFrom<ITrackable>());

				SetProperty(contractorInstance, "Name", "ivan");
				moqListener.Verify(x => x(contractorInstance), Times.Once());
				SetProperty(documentInstance, "Number", "123");
			}
		}

		public class TrackChangesAffectsChildClasses : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public abstract class Entity
			{
				public Guid Id { get; set; }
			}

			public class Document : Entity
			{
				public string Number { get; set; }
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();

				var documentInstance = CreateInstance<Document>(assembly);
				AttachListenerTo(documentInstance);

				SetProperty(documentInstance, "Number", "567");
				moqListener.Verify(x => x(documentInstance), Times.Once());

				SetProperty(documentInstance, "Id", Guid.NewGuid());
				moqListener.Verify(x => x(documentInstance), Times.Exactly(2));
			}
		}

		public class ManySimpleProperties : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Document
			{
				public string Number { get; set; }
				public decimal Sum { get; set; }
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();

				var documentInstance = CreateInstance<Document>(assembly);
				AttachListenerTo(documentInstance);

				SetProperty(documentInstance, "Number", "567");
				SetProperty(documentInstance, "Sum", 12m);
				moqListener.Verify(x => x(documentInstance), Times.Exactly(2));
			}
		}

		public class TrackOnlyAutoProperties : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Document
			{
				public string Number { get; set; }

				public string OnlyGetter
				{
					get { return "42"; }
				}

				private string onlySetter;

				public string OnlySetter
				{
					set { onlySetter = value; }
				}

				private string privateValue;

				private string Private
				{
					get { return "42"; }
					set { privateValue = value; }
				}

				public string WithoutBackingField
				{
					get { return "78"; }
					set { }
				}

				private string manyBackingField1;
				private string manyBackingField2;

				public string ManyBackingFields
				{
					get { return "78"; }
					set
					{
						manyBackingField1 = value;
						manyBackingField2 = value;
					}
				}

				private string manualProperty;
				private string manualPropertyTrash;

				public string ManualProperty
				{
					get { return manualProperty; }
					set
					{
						manualProperty = value;
						manualPropertyTrash = value;
					}
				}
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();

				var documentInstance = CreateInstance<Document>(assembly);
				AssertNotTrackable(documentInstance, "Private");
				AssertNotTrackable(documentInstance, "WithoutBackingField");
				AssertNotTrackable(documentInstance, "ManyBackingFields");
				AssertNotTrackable(documentInstance, "OnlySetter");
				AssertNotTrackable(documentInstance, "ManualProperty");

				AttachListenerTo(documentInstance);

				SetProperty(documentInstance, "Number", "567");
				moqListener.Verify(x => x(documentInstance), Times.Exactly(1));
			}
		}

		public class TrackReferencedClasses : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Contractor
			{
				public string Name { get; set; }
				public AccountInfo Account { get; set; }
			}

			public class AccountInfo
			{
				public string BankName { get; set; }
				public string AccountNumber { get; set; }
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();
				var contractor = CreateInstance<Contractor>(assembly);
				AttachListenerTo(contractor);
				moqListener.Verify(x => x(contractor), Times.Never());
				var accountInfo = CreateInstance<AccountInfo>(assembly);
				AttachListenerTo(accountInfo);
				SetProperty(contractor, "Account", accountInfo);
				moqListener.Verify(x => x(contractor), Times.Once());
				moqListener.Verify(x => x(accountInfo), Times.Never());
				SetProperty(accountInfo, "BankName", "testBank");
				moqListener.Verify(x => x(contractor), Times.Exactly(2));
				moqListener.Verify(x => x(accountInfo), Times.Once());
				SetProperty(accountInfo, "AccountNumber", "123");
				moqListener.Verify(x => x(contractor), Times.Exactly(3));
				moqListener.Verify(x => x(accountInfo), Times.Exactly(2));
				SetProperty(contractor, "Name", "testContractorName");
				moqListener.Verify(x => x(contractor), Times.Exactly(4));
				moqListener.Verify(x => x(accountInfo), Times.Exactly(2));
			}
		}

		public class DontExpandTrackingViaEnumProperties : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class User
			{
				public UserType UserType { get; set; }
			}

			public enum UserType
			{
				Main,
				Manager
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<User>(assembly);
				AttachListenerTo(instance);
				SetProperty(instance, "UserType", UserType.Manager);
				moqListener.Verify(x => x(instance), Times.Once());
			}
		}

		//todo забил пока
		//public class CannotTrackArrays : ChangeTrackingRewriterTest
		//{
		//	[TrackChanges]
		//	public class SomeClass
		//	{
		//		public string[] Array { get; set; }
		//	}

		//	[Test]
		//	public void Test()
		//	{
		//		var error = Assert.Throws<ChangeTrackingRewriterException>(() => rewriter.Rewrite());
		//		Assert.That(error.Message, Is.StringContaining("can't track array property Array of type "));
		//		Assert.That(error.Message, Is.StringContaining("SomeClass"));
		//	}
		//}

		public class DontProcessTypesTwice : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Contractor
			{
				public string Name { get; set; }
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				rewriter.Rewrite();
				Assert.That(LoadType<Contractor>().Fields.Count(x => x.FieldType.FullName == typeof (ChangeTracker).FullName), Is.EqualTo(1));
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<Contractor>(assembly);
				Assert.That(instance.GetType().GetCustomAttributes(typeof (RewrittenForTrackChangesAttribute), false).Length, Is.EqualTo(1));
				AttachListenerTo(instance);
				SetProperty(instance, "Name", "testName");
				moqListener.Verify(x => x(instance), Times.Exactly(1));
			}
		}

		public class CrashIfTypeFromOtherAssemblyIsNotRewritten : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Contractor
			{
				public AccountInfo Account { get; set; }
			}

			public class AccountInfo
			{
				public string AccountNumber { get; set; }
			}

			[Test]
			public void Test()
			{
				var stringAssembly = LoadType<string>().Module.Assembly;
				parameters.GetAssemblyDelegate = r => r.Is<AccountInfo>() ? stringAssembly : r.Resolve().Module.Assembly;
				var error = Assert.Throws<ChangeTrackingRewriterException>(rewriter.Rewrite);
				Assert.That(error.Message,
				            Is.EqualTo(string.Format("property Account of type {0} references not rewritten type {1} from assembly {2}",
				                                     LoadType<Contractor>().FullName, LoadType<AccountInfo>().FullName, stringAssembly.FullName)));
			}
		}

		public class SkipTypesFromForeignAssemblies : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class ForeignClass
			{
				public string AccountNumber { get; set; }
			}

			public class Contractor : ForeignClass
			{
				public string Name { get; set; }
			}

			[Test]
			public void Test()
			{
				parameters.IsForeignDelegate = r => r.Is<ForeignClass>();
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<Contractor>(assembly);
				AssertNotTrackable(instance, "Name");
			}
		}

		public class DontRewriteInterfaceProperties : ChangeTrackingRewriterTest
		{
			public interface IInterface
			{
			}
			
			[TrackChanges]
			public class ForeignClass
			{
				public IInterface Tricky { get; set; }
			}

			[Test]
			public void Test()
			{
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();
				var instance = CreateInstance<ForeignClass>(assembly);
				Assert.That(instance, Is.Not.AssignableFrom<ITrackable>());
			}
		}

		public class LookupForTrackerFromRewrittenParent : ChangeTrackingRewriterTest
		{
			[TrackChanges]
			public class Parent
			{
			}

			public class Child : Parent
			{
				public string Name { get; set; }
			}

			[Test]
			public void Test()
			{
				parameters.TypesToProcess = new[] {LoadType<Parent>()};
				rewriter.Rewrite();
				parameters.TypesToProcess = new[] {LoadType<Parent>(), LoadType<Child>()};
				rewriter.Rewrite();
				var assembly = assemblyDefinition.ToAssembly();
				AssertTrackable(CreateInstance<Child>(assembly), "Name");
			}
		}

		public class TrackerFieldsFromOtherAssembliesMustBeImported : ChangeTrackingRewriterTest
		{
			private const string firstCode = @"
					using System;
					using Kontur.Elba.Aop.Definitions.ChangeTracking;

					namespace Kontur.Elba.Aop.Tests.TrackerFieldsFromOtherAssembliesMustBeImported
					{
						[TrackChanges]
						public class Entity
						{
							public Guid Id { get; set; }
						}
					}
				";

			private const string secondCode = @"
					using System;

					namespace Kontur.Elba.Aop.Tests.TrackerFieldsFromOtherAssembliesMustBeImported
					{						
						public class Contractor: Entity
						{
							public string Name { get; set; }
						}
					}";

			[Test]
			public void Test()
			{
				var first = CompileAndRewrite(firstCode, typeof (TrackChangesAttribute).Assembly);
				var second = CompileAndRewrite(secondCode, typeof (TrackChangesAttribute).Assembly, first);
				var instance = CreateInstance(second, "Kontur.Elba.Aop.Tests.TrackerFieldsFromOtherAssembliesMustBeImported.Contractor");
				AssertTrackable(instance, "Name");
			}
		}

		public class SkipImmutableTypes : ChangeTrackingRewriterTest
		{
			private const string firstCode = @"
					namespace Kontur.Elba.Aop.Tests.SkipImmutableTypes
					{
						public class ImmutableName
						{
							public string Name { get; set; }
						}
					}
				";

			private const string secondCode = @"
					using Kontur.Elba.Aop.Definitions.ChangeTracking;

					namespace Kontur.Elba.Aop.Tests.SkipImmutableTypes
					{						
						[TrackChanges]
						public class Contractor
						{
							public ImmutableName Name { get; set; }
						}
					}";

			[Test]
			public void Test()
			{
				var first = CompileAndRewrite(firstCode, typeof (TrackChangesAttribute).Assembly);
				var second = CompileAndRewrite(secondCode, new[] {"ImmutableName"}, typeof (TrackChangesAttribute).Assembly, first);
				var instance = CreateInstance(second, "Kontur.Elba.Aop.Tests.SkipImmutableTypes.Contractor");
				var immutable = CreateInstance(first, "Kontur.Elba.Aop.Tests.SkipImmutableTypes.ImmutableName");
				AttachListenerTo(instance);
				SetProperty(instance, "Name", immutable);
				moqListener.Verify(x => x(instance), Times.Once());
			}
		}

		public class TrackerFieldFromGrandfather : ChangeTrackingRewriterTest
		{
			private const string grandFatherCode = @"
					using System;
					using Kontur.Elba.Aop.Definitions.ChangeTracking;

					namespace Kontur.Elba.Aop.Tests.TrackerFieldFromGrandfather
					{
						[TrackChanges]
						public class Entity
						{
							public Guid Id { get; set; }
						}
					}
				";

			private const string fatherCode = @"
					using System;

					namespace Kontur.Elba.Aop.Tests.TrackerFieldFromGrandfather
					{						
						public class OrganizationEntity: Entity
						{
							public Guid OrganizationId { get; set; }
						}
					}";

			private const string code = @"
					using System;

					namespace Kontur.Elba.Aop.Tests.TrackerFieldFromGrandfather
					{						
						public class Contractor: OrganizationEntity
						{
							public string Name { get; set; }
						}
					}";

			[Test]
			public void Test()
			{
				var grandFather = CompileAndRewrite(grandFatherCode, typeof (TrackChangesAttribute).Assembly);
				var father = CompileAndRewrite(fatherCode, typeof (TrackChangesAttribute).Assembly, grandFather);
				var assembly = CompileAndRewrite(code, typeof (TrackChangesAttribute).Assembly, grandFather, father);

				var instance = CreateInstance(assembly, "Kontur.Elba.Aop.Tests.TrackerFieldFromGrandfather.Contractor");
				AssertTrackable(instance, "Name");
			}
		}

		public class ReferencedPropertyFromAnotherAssembly : ChangeTrackingRewriterTest
		{
			private const string firstCode = @"
					using System;
					using Kontur.Elba.Aop.Definitions.ChangeTracking;

					namespace Kontur.Elba.Aop.Tests.ReferencedPropertyFromAnotherAssembly
					{
						[TrackChanges]
						public class AccountInfo
						{
							public string Number { get; set; }
						}
					}
				";

			private const string secondCode = @"
					using System;
					using Kontur.Elba.Aop.Definitions.ChangeTracking;

					namespace Kontur.Elba.Aop.Tests.ReferencedPropertyFromAnotherAssembly
					{
						[TrackChanges]
						public class Contractor
						{
							public AccountInfo Account { get; set; }
						}
					}";

			[Test]
			public void Test()
			{
				var first = CompileAndRewrite(firstCode, typeof (TrackChangesAttribute).Assembly);
				var second = CompileAndRewrite(secondCode, typeof (TrackChangesAttribute).Assembly, first);
				var instance = CreateInstance(second, "Kontur.Elba.Aop.Tests.ReferencedPropertyFromAnotherAssembly.Contractor");
				var account = CreateInstance(first, "Kontur.Elba.Aop.Tests.ReferencedPropertyFromAnotherAssembly.AccountInfo");
				SetProperty(instance, "Account", account);
				AttachListenerTo(instance);
				SetProperty(account, "Number", "123");
				moqListener.Verify(x => x(instance), Times.Once());
			}
		}

		protected Assembly CompileAndRewrite(string source, string[] immutableTypeNames, params Assembly[] references)
		{
			var testAssemblyName = "tmp_" + Guid.NewGuid().ToString("N");
			var tempAssemblyFileName = testAssemblyName + ".dll";
			var compilationParameters = new CompilerParameters
				{
					OutputAssembly = tempAssemblyFileName,
					GenerateExecutable = false
				};
			foreach (var reference in references.Select(x => x.GetName().Name + ".dll"))
				compilationParameters.ReferencedAssemblies.Add(reference);
			var compilationResult = CodeDomProvider.CreateProvider("C#").CompileAssemblyFromSource(compilationParameters, source);
			if (compilationResult.Errors.HasErrors || compilationResult.Errors.HasWarnings)
				Assert.Fail(compilationResult.Errors.Cast<CompilerError>().Select(x => x.ToString()).First());
			var generatedAssemblyDefinition = MonoCecilHelpers.LoadAssembly(tempAssemblyFileName, false);
			var rewriterParameters = new ChangeTrackingParameters
				{
					Assembly = generatedAssemblyDefinition,
					TypesToProcess = generatedAssemblyDefinition.MainModule.GetTypes().ToArray(),
					ProcessNestedTypes = false,
					ImmutableTypeNames = immutableTypeNames == null ? null : new HashSet<string>(immutableTypeNames),
				};
			var generatedAssemblyRewriter = new ChangeTrackingRewriter(rewriterParameters);
			generatedAssemblyRewriter.Rewrite();
			using (var stream = new FileStream(tempAssemblyFileName, FileMode.Create, FileAccess.Write))
				generatedAssemblyDefinition.Write(stream, new WriterParameters {WriteSymbols = false});
			return Assembly.Load(testAssemblyName);
		}

		protected Assembly CompileAndRewrite(string source, params Assembly[] references)
		{
			return CompileAndRewrite(source, null, references);
		}

		protected void AssertTrackable(object instance, string propertyName)
		{
			var listener = new Mock<Action<object>>();
			Assert.That(instance, Is.InstanceOf<ITrackable>());
			((ITrackable) instance).Tracker.BeforeChange += listener.Object;
			SetProperty(instance, propertyName, "42");
			((ITrackable) instance).Tracker.BeforeChange -= listener.Object;
			listener.Verify(x => x(instance), Times.Once());
		}

		protected void AssertNotTrackable(object instance, string propertyName)
		{
			var listener = new Mock<Action<object>>();
			if (instance is ITrackable == false)
				return;
			((ITrackable) instance).Tracker.BeforeChange += listener.Object;
			SetProperty(instance, propertyName, "42");
			((ITrackable) instance).Tracker.BeforeChange -= listener.Object;
			listener.Verify(x => x(instance), Times.Never());
		}
	}
}