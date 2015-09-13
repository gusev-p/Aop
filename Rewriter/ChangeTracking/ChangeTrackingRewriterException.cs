using System;

namespace Aop.Rewriter.ChangeTracking
{
	public class ChangeTrackingRewriterException : Exception
	{
		public ChangeTrackingRewriterException(string message) : base(message)
		{
		}
	}
}