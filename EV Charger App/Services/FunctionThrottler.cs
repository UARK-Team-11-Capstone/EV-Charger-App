using System;
using System.Diagnostics;

namespace EV_Charger_App.Services
{
    internal class FunctionThrottler
    {
        private Stopwatch stopwatch = new Stopwatch(); // Stopwatch to track time
        private TimeSpan throttleTime; // Throttle time interval
        private DateTime lastExecutionTime; // Last execution time

        public FunctionThrottler(TimeSpan throttleTime)
        {
            this.throttleTime = new TimeSpan(0, 0, 0, throttleTime.Seconds);
            this.lastExecutionTime = new DateTime();
            this.lastExecutionTime = DateTime.Now;
            this.stopwatch.Start();
        }

        // Check if the function can be executed based on the throttle time
        public bool CanExecute()
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSinceLastExecution = now - lastExecutionTime;

            if (stopwatch.IsRunning && timeSinceLastExecution < throttleTime)
            {
                return false; // Throttled, function cannot be executed
            }

            // Start or reset the stopwatch and update last execution time
            stopwatch.Restart();
            lastExecutionTime = now;

            return true; // Function can be executed
        }

        // Reset the throttler
        public void Reset()
        {
            stopwatch.Reset();
            lastExecutionTime = DateTime.MinValue;
        }
    }
}
