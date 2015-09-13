using System;

namespace Kontur.Elba.Aop.Definitions.ChangeTracking
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class RewrittenForTrackChangesAttribute : Attribute
	{
	}
}