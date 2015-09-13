namespace Kontur.Elba.Aop.Definitions.ChangeTracking
{
	public interface ITrackable
	{
		ChangeTracker Tracker { get; }
	}
}