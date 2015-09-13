using System.Collections.Generic;
using Mono.Cecil;

namespace Kontur.Elba.Aop.Rewriter.MonoCecil
{
	public class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference>
	{
		private TypeReferenceEqualityComparer()
		{
		}

		static TypeReferenceEqualityComparer()
		{
			Instance = new TypeReferenceEqualityComparer();
		}

		public static IEqualityComparer<TypeReference> Instance { get; private set; }

		public bool Equals(TypeReference x, TypeReference y)
		{
			return x.FullName.Equals(y.FullName);
		}

		public int GetHashCode(TypeReference obj)
		{
			return obj.FullName.GetHashCode();
		}
	}
}