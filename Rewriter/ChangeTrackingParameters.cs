using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Kontur.Elba.Aop.Rewriter
{
	public class ChangeTrackingParameters
	{
		public AssemblyDefinition Assembly { get; set; }
		public TypeDefinition[] TypesToProcess { get; set; }
		public bool ProcessNestedTypes { get; set; }
		public Func<TypeDefinition, AssemblyDefinition> GetAssemblyDelegate { get; set; }
		public Func<TypeReference, bool> IsForeignDelegate { get; set; }
		public ISet<String> ImmutableTypeNames { get; set; }

		public AssemblyDefinition GetAssembly(TypeDefinition definition)
		{
			return GetAssemblyDelegate != null ? GetAssemblyDelegate(definition) : definition.Module.Assembly;
		}

		public bool IsForeign(TypeReference typeReference)
		{
			return IsForeignDelegate != null && IsForeignDelegate(typeReference);
		}
	}
}