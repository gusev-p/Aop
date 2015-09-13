using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using Aop.Definitions.ChangeTracking;
using Aop.Rewriter.MonoCecil;
using Mono.Cecil;

namespace Aop.Rewriter.ChangeTracking
{
	public class ChangeTrackingScheme
	{
		private readonly ChangeTrackingParameters parameters;
		private readonly ISet<string> notTrackableTypeNames;

		private readonly Type[] notTrackableTypes = new[]
			{
				typeof (byte),
				typeof (short),
				typeof (ushort),
				typeof (int),
				typeof (uint),
				typeof (long),
				typeof (ulong),
				typeof (double),
				typeof (float),
				typeof (string),
				typeof (Guid),
				typeof (bool),
				typeof (DateTime),
				typeof (ChangeTracker),

				//что с этим делать, блять?
				typeof (NameValueCollection)
			};

		public ChangeTrackingScheme(ChangeTrackingParameters parameters)
		{
			this.parameters = parameters;
			notTrackableTypeNames = new HashSet<string>(notTrackableTypes.Select(x => x.FullName));
		}

		public bool NeedTrackPropertyType(TypeDefinition typeDefinition)
		{
			if (notTrackableTypeNames.Contains(typeDefinition.FullName))
				return false;
			if (typeDefinition.IsValueType)
				return false;
			if (typeDefinition.IsEnum)
				return false;
			if (typeDefinition.IsInterface)
				return false;
			if (parameters.ImmutableTypeNames != null && parameters.ImmutableTypeNames.Contains(typeDefinition.Name))
				return false;
			return true;
		}

		public bool NeedTrackType(TypeDefinition typeDefinition)
		{
			if (typeDefinition.Name == "<Module>")
				return false;
			if (typeDefinition.IsValueType)
				return false;
			if (typeDefinition.IsNested && !parameters.ProcessNestedTypes)
				return false;
			if (typeDefinition.IsInterface)
				return false;
			if (typeDefinition.HasAttribute<CompilerGeneratedAttribute>())
				return false;
			return true;
		}
	}
}