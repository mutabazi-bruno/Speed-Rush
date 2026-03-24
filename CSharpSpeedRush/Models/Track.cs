using System;

namespace CSharpSpeedRush.Models
{
    /// <summary>
    /// Represents the race track. Tracks distance driven and lap count.
    /// </summary>
    public class Track
    {
        // Constants

        /// <summary>
        /// The length of one lap in distance units.
        /// The player must accumulate this much distance to finish a lap.
        /// </summary>
        public const double LapDistance = 100.0;

        // Properties

        /// <summary>Total number of laps in this race (always 5 as per the spec).</summary>
        public int TotalLaps { get; private set; }

        /// <summary>Which lap the player is currently on (starts at 1).</summary>
        public int CurrentLap { get; private set; }

        /// <summary>
        /// How far the car has travelled within the current lap (0–100).
        /// Once this reaches 100, a new lap begins.
        /// </summary>
        public double DistanceInCurrentLap { get; private set; }

        /// <summary>True when the player has finished all laps.</summary>
        public bool IsRaceComplete => CurrentLap > TotalLaps;

        // Constructor

        /// <summary>
        /// Creates a track with the specified number of laps.
        /// </summary>
        /// <param name="totalLaps">How many laps the race has. Defaults to 5.</param>
        public Track(int totalLaps = 5)
        {
            TotalLaps = totalLaps;
            CurrentLap = 1;
            DistanceInCurrentLap = 0;
        }

        // Methods

        /// <summary>
        /// Moves the car forward by the given distance.
        /// Automatically completes laps when 100 units are reached.
        /// </summary>
        /// <param name="distance">Distance to add this turn.</param>
        /// <returns>How many laps were completed during this move (usually 0 or 1).</returns>
        public int AdvanceDistance(double distance)
        {
            int lapsCompleted = 0;

            DistanceInCurrentLap += distance;

            // Roll over to next lap when distance threshold is reached
            while (DistanceInCurrentLap >= LapDistance && !IsRaceComplete)
            {
                // Carry the leftover distance into the next lap
                DistanceInCurrentLap -= LapDistance;
                CurrentLap++;
                lapsCompleted++;
            }

            return lapsCompleted;
        }

        /// <summary>
        /// Returns the current lap progress as a 0–100 percentage.
        /// Used by the progress bar in the UI.
        /// </summary>
        public double LapProgressPercent => (DistanceInCurrentLap / LapDistance) * 100.0;

        /// <summary>
        /// Builds a visual text progress bar like [=====>    ] to show lap progress.
        /// This is the optional textual progress indicator from the spec.
        /// </summary>
        /// <param name="width">How many characters wide the bar should be.</param>
        public string GetProgressBar(int width = 20)
        {
            int filled = (int)((DistanceInCurrentLap / LapDistance) * width);
            filled = Math.Clamp(filled, 0, width);

            string bar = new string('=', filled);
            string empty = new string(' ', width - filled);

            return $"[{bar}>{empty}]";
        }

        /// <summary>Returns a short summary like "Lap 2/5".</summary>
        public override string ToString() => $"Lap {Math.Min(CurrentLap, TotalLaps)}/{TotalLaps}";
    }
}
