namespace Aop.Definitions.ChangeTracking
{
	public interface ITrackable
	{
		ChangeTracker Tracker { get; }
	}
}