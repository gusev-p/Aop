using System.Collections.Generic;
using System.Linq;
using Kontur.Elba.Aop.Definitions.ChangeTracking;
using Kontur.Elba.Aop.Rewriter.MonoCecil;
using Mono.Cecil;

namespace Kontur.Elba.Aop.Rewriter.ChangeTracking
{
	public class ChangeTrackingRewriter
	{
		private readonly ChangeTrackingParameters parameters;
		private readonly ChangeTrackingTypeWeaver weaver;

		private readonly IDictionary<TypeDefinition, TypeToRewrite> types =
			new Dictionary<TypeDefinition, TypeToRewrite>(TypeReferenceEqualityComparer.Instance);

		private readonly ChangeTrackingScheme scheme;

		public ChangeTrackingRewriter(ChangeTrackingParameters parameters)
		{
			this.parameters = parameters;
			weaver = new ChangeTrackingTypeWeaver(parameters.Assembly);
			scheme = new ChangeTrackingScheme(parameters);
		}

		public void Rewrite()
		{
			types.Clear();
			var typesToProcess = parameters.TypesToProcess
				.Where(scheme.NeedTrackType)
				.Select(GetTypeToRewrite)
				.Where(x => x.hierarchyMarkedWithTrackChangesAttribute);
			foreach (var type in typesToProcess)
				EnsureRewritten(type);
		}

		private void EnsureRewritten(TypeToRewrite type)
		{
			if (type.rewritten)
				return;
			if (type.parent != null)
				EnsureRewritten(type.parent);
			if (type.type.HasAttribute<RewrittenForTrackChangesAttribute>())
			{
				type.rewritten = true;
				if (type.parent != null)
					type.trackerFieldReference = type.parent.trackerFieldReference;
				else
				{
					var fieldDefinition = type.type.Fields.Single(x => x.FieldType.Is<ChangeTracker>());
					type.trackerFieldReference = parameters.Assembly.MainModule.Import(fieldDefinition);
				}
				return;
			}
			type.trackerFieldReference = type.parent != null ? type.parent.trackerFieldReference : weaver.ImplementTrackable(type.type);
			foreach (var property in type.type.Properties)
			{
				if (!weaver.TryObserveProperty(property, type.trackerFieldReference))
					continue;
				var typeReference = property.PropertyType;
				//if (typeReference.IsArray)
				//	throw new ChangeTrackingRewriterException(string.Format("can't track array property {0} of type {1}", property.Name, type.Type.Name));
				var propertyType = typeReference.Resolve();
				if (!scheme.NeedTrackPropertyType(propertyType))
					continue;
				var propertyAssembly = parameters.GetAssembly(propertyType);
				if (propertyAssembly.FullName != parameters.Assembly.FullName && !propertyType.HasAttribute<RewrittenForTrackChangesAttribute>())
					throw new ChangeTrackingRewriterException(
						string.Format("property {0} of type {1} references not rewritten type {2} from assembly {3}",
						              property.Name, type.type.FullName, propertyType.FullName, propertyAssembly.FullName));
				var typeToRewrite = GetTypeToRewrite(propertyType);
				EnsureRewritten(typeToRewrite);
			}
			weaver.MarkAsRewritten(type.type);
			type.rewritten = true;
		}

		private TypeToRewrite GetTypeToRewrite(TypeDefinition type)
		{
			TypeToRewrite result;
			if (!types.TryGetValue(type, out result))
				types.Add(type, result = CreateTypeToRewrite(type));
			return result;
		}

		private TypeToRewrite CreateTypeToRewrite(TypeDefinition type)
		{
			var markedWithTrackChanges = type.HasAttribute<TrackChangesAttribute>();
			var parent = type.BaseType.Name == "Object" || markedWithTrackChanges || parameters.IsForeign(type.BaseType)
				             ? null
				             : GetTypeToRewrite(type.BaseType.Resolve());
			return new TypeToRewrite
				{
					hierarchyMarkedWithTrackChangesAttribute = markedWithTrackChanges ||
					                                           parent != null && parent.hierarchyMarkedWithTrackChangesAttribute,
					parent = parent,
					type = type
				};
		}

		private class TypeToRewrite
		{
			public TypeDefinition type;
			public bool hierarchyMarkedWithTrackChangesAttribute;
			public FieldReference trackerFieldReference;
			public TypeToRewrite parent;
			public bool rewritten;
		}
	}
}