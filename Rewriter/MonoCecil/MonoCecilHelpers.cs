using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Pdb;
using Mono.Collections.Generic;
using ICustomAttributeProvider = Mono.Cecil.ICustomAttributeProvider;

namespace Aop.Rewriter.MonoCecil
{
	public static class MonoCecilHelpers
	{
		public static TypeDefinition LoadType(this ModuleDefinition module, Type type)
		{
			var targetModule = type.Assembly.FullName == module.Assembly.FullName
				                   ? module
				                   : module.AssemblyResolver.Resolve(type.Assembly.FullName).MainModule;
			var result = targetModule.GetType(ToMonoFullName(type.FullName));
			if (result == null)
				throw new InvalidOperationException("can't resolve type " + type.FullName);
			return result;
		}

		public static string ToMonoFullName(string reflectionFullName)
		{
			return reflectionFullName.Replace("+", "/");
		}

		public static bool Is<T>(this TypeReference typeDefinition)
		{
			return typeDefinition.FullName == ToMonoFullName(typeof (T).FullName);
		}

		public static TypeDefinition LoadType<T>(this ModuleDefinition module)
		{
			return module.LoadType(typeof (T));
		}

		public static AssemblyDefinition GetExecutingAssembly()
		{
			var assemblyPath = Assembly.GetCallingAssembly().Location;
			return LoadAssembly(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(assemblyPath)));
		}

		public static AssemblyDefinition LoadAssembly(string path, bool readsymbols = true)
		{
			var resolver = new DefaultAssemblyResolver();
			resolver.AddSearchDirectory(Path.GetDirectoryName(path));
			var readerParameters = new ReaderParameters
				{
					SymbolReaderProvider = readsymbols ? new PdbReaderProvider() : null,
					ReadingMode = ReadingMode.Deferred,
					ReadSymbols = readsymbols,
					AssemblyResolver = resolver
				};
			return AssemblyDefinition.ReadAssembly(path, readerParameters);
		}

		public static void Prepend(this Collection<Instruction> instructions, params Instruction[] newInstructions)
		{
			instructions.Prepend(newInstructions.AsEnumerable());
		}

		public static void Prepend(this Collection<Instruction> instructions, IEnumerable<Instruction> newInstructions)
		{
			var index = 0;
			foreach (var instruction in newInstructions)
				instructions.Insert(index++, instruction);
		}

		//ебаные, блять, pdb, просираются иногда
		public static Assembly ToAssembly(this AssemblyDefinition assemblyDefinition)
		{
			var stream = new MemoryStream();
			var pdbFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Guid.NewGuid().ToString("N")) + ".pdb";
			byte[] symbolBytes;
			try
			{
				assemblyDefinition.Write(stream, new WriterParameters
					{
						WriteSymbols = true,
						SymbolWriterProvider = new SymbolWriterProvider(pdbFileName)
					});
				symbolBytes = File.ReadAllBytes(pdbFileName);
			}
			finally
			{
				if (File.Exists(pdbFileName))
					File.Delete(pdbFileName);
			}
			return Assembly.Load(stream.ToArray(), symbolBytes);
		}

		public class SymbolWriterProvider : ISymbolWriterProvider
		{
			private readonly string pdbFileName;
			private readonly PdbWriterProvider provider;

			public SymbolWriterProvider(string pdbFileName)
			{
				this.pdbFileName = pdbFileName;
				provider = new PdbWriterProvider();
			}

			public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
			{
				return provider.GetSymbolWriter(module, pdbFileName);
			}

			public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
			{
				return provider.GetSymbolWriter(module, symbolStream);
			}
		}

		public static bool HasAttribute<T>(this ICustomAttributeProvider type)
		{
			return type.CustomAttributes.Any(x => x.AttributeType.Is<T>());
		}
	}
}