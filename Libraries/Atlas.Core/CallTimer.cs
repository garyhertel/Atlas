﻿using System;
using System.Diagnostics;

namespace Atlas.Core
{
	public class CallTimer : Call, IDisposable
	{
		private Stopwatch _stopwatch = new Stopwatch();
		private System.Timers.Timer _timer = new System.Timers.Timer();

		public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

		public CallTimer()
		{
			_stopwatch.Start();

			_timer.Interval = 1000.0;
			_timer.Elapsed += Timer_Elapsed;
			_timer.Start();
		}

		public void Stop()
		{
			_timer.Stop();
			_stopwatch.Stop();
			_timer.Elapsed -= Timer_Elapsed;
			UpdateDuration();
		}

		private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			UpdateDuration();
		}

		private void UpdateDuration()
		{
			if (Log != null)
				Log.Duration = ElapsedMilliseconds / 1000.0f;
		}

		public void Dispose()
		{
			Stop();
			_timer.Dispose();
			TaskInstance?.SetFinished();
			if (TaskInstance == null)
				Log.Add("Finished", new Tag("Time", _stopwatch.ElapsedMilliseconds / 1000.0));
		}
	}
}
