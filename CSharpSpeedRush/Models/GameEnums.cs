namespace CSharpSpeedRush.Models
{
    /// <summary>The three actions a player can take on their turn.</summary>
    public enum PlayerAction
    {
        /// <summary>Go faster – uses more fuel but covers more distance.</summary>
        SpeedUp,

        /// <summary>Keep the current pace – balanced fuel and distance.</summary>
        MaintainSpeed,

        /// <summary>Pull into the pit lane to refuel – no distance gained this turn.</summary>
        PitStop
    }

    /// <summary>
    /// Tracks whether the race is still going or has finished (and why).
    /// The UI uses this to decide what message to show the player.
    /// </summary>
    public enum RaceStatus
    {
        /// <summary>Race has not started yet.</summary>
        NotStarted,

        /// <summary>Race is currently in progress.</summary>
        InProgress,

        /// <summary>Player finished all laps successfully.</summary>
        Finished,

        /// <summary>Car ran out of fuel before finishing – game over.</summary>
        OutOfFuel,

        /// <summary>Clock ran out before all laps were completed – game over.</summary>
        TimeUp
    }

    /// <summary>
    /// A snapshot of everything that happened during one game turn.
    /// Stored in the turn history queue so the player can review their race.
    /// </summary>
    public struct TurnResult
    {
        /// <summary>Which turn number this was (1, 2, 3 ...).</summary>
        public int TurnNumber;

        /// <summary>What action the player chose on this turn.</summary>
        public PlayerAction Action;

        /// <summary>How far the car moved this turn (in distance units).</summary>
        public double DistanceCovered;

        /// <summary>How much fuel was burned this turn.</summary>
        public double FuelBurned;

        /// <summary>What lap the car was on after this turn.</summary>
        public int LapAfterTurn;

        /// <summary>A human-friendly summary of this turn's events.</summary>
        public string Message;
    }
}
