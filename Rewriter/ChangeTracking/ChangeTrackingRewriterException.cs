using System;

namespace Kontur.Elba.Aop.Rewriter.ChangeTracking
{
	public class ChangeTrackingRewriterException : Exception
	{
		public ChangeTrackingRewriterException(string message) : base(message)
		{
		}
	}
}