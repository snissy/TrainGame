// Create a new C# script, perhaps named "PerformanceUtils.cs"

using System.Diagnostics; // Required for Stopwatch
using System; // Required for Action

namespace AmbientOcclusion.Geometry.Scripts
{
    public static class PerformanceUtils
    {
        /// <summary>
        /// Measures the execution time of a given code block.
        /// </summary>
        /// <param name="actionToMeasure">The code block to measure, passed as an Action delegate.</param>
        /// <returns>A TimeSpan representing the elapsed time.</returns>
        public static TimeSpan MeasureExecutionTime(Action actionToMeasure)
        {
            if (actionToMeasure == null)
            {
                throw new ArgumentNullException(nameof(actionToMeasure));
            }

            Stopwatch stopwatch = Stopwatch.StartNew(); // Creates and starts a new stopwatch
            actionToMeasure(); // Execute the code block passed in
            stopwatch.Stop(); // Stop the stopwatch

            return stopwatch.Elapsed; // Return the measured time
        }

        // --- Optional Overloads ---

        /// <summary>
        /// Measures the execution time and logs it directly in milliseconds.
        /// </summary>
        public static void MeasureAndLogMs(string description, Action actionToMeasure)
        {
            TimeSpan elapsed = MeasureExecutionTime(actionToMeasure);
            UnityEngine.Debug.Log($"{description} took: {elapsed.TotalMilliseconds:F4} ms");
            // F4 formats to 4 decimal places for milliseconds
        }

        /// <summary>
        /// Measures the execution time and logs it directly in seconds.
        /// </summary>
        public static void MeasureAndLogSec(string description, Action actionToMeasure)
        {
            TimeSpan elapsed = MeasureExecutionTime(actionToMeasure);
            UnityEngine.Debug.Log($"{description} took: {elapsed.TotalSeconds:F6} s");
            // F6 formats to 6 decimal places for seconds
        }
    }
}