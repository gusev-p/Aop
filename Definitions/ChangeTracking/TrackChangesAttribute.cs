using System;

namespace Aop.Definitions.ChangeTracking
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class TrackChangesAttribute : Attribute
	{
	}
}