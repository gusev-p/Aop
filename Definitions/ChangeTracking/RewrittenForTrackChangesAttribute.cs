using System;

namespace Aop.Definitions.ChangeTracking
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class RewrittenForTrackChangesAttribute : Attribute
	{
	}
}