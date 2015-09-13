using System;

namespace Kontur.Elba.Aop.Definitions.ChangeTracking
{
	public class ChangeTracker
	{
		private readonly object target;
		private readonly Action<object> raiseBeforeChange;

		public ChangeTracker(object target)
		{
			this.target = target;
			raiseBeforeChange = RaiseBeforeChange;
		}

		public event Action<object> BeforeChange;

		//todo заинлайнить в ил - избавиться от боксинга
		public void BeforeTargetPropertyChange(object oldValue, object newValue)
		{
			if (oldValue is ChangeTracker || newValue is ChangeTracker || Equals(oldValue, newValue))
				return;
			RaiseBeforeChange(target);
			var oldTrackable = oldValue as ITrackable;
			if (oldTrackable != null)
				oldTrackable.Tracker.BeforeChange -= raiseBeforeChange;
			var newTrackable = newValue as ITrackable;
			if (newTrackable != null)
				newTrackable.Tracker.BeforeChange += raiseBeforeChange;
		}

		public void DetachAll()
		{
			BeforeChange = null;
		}

		private void RaiseBeforeChange(object sender)
		{
			var localChanged = BeforeChange;
			if (localChanged != null)
				localChanged(target);
		}
	}
}