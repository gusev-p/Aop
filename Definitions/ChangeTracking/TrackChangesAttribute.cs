using System;

namespace Kontur.Elba.Aop.Definitions.ChangeTracking
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
	public class TrackChangesAttribute : Attribute
	{
	}
}