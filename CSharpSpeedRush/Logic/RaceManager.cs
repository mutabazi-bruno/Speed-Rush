using System;
using System.Collections.Generic;
using CSharpSpeedRush.Models;

namespace CSharpSpeedRush.Logic
{
    /// <summary>
    /// Central game controller. Holds car, track, time, and turn state.
    /// Processes player actions and updates state. UI calls this layer only.
    /// </summary>
    public class RaceManager
    {
        // Configuration

        /// <summary>How many time units the player starts with.</summary>
        public const int StartingTime = 90;

        /// <summary>Time cost per turn when speeding up.</summary>
        private const int TimeForSpeedUp = 2;

        /// <summary>Time cost per turn when maintaining speed.</summary>
        private const int TimeForMaintain = 3;

        /// <summary>Time cost per turn for a pit stop (slower, but you get fuel).</summary>
        private const int TimeForPitStop = 5;

        /// <summary>How much fuel a pit stop adds to the tank.</summary>
        private const double PitStopFuelAmount = 50.0;

        /// <summary>Speed multiplier when the player chooses "Speed Up".</summary>
        private const double SpeedUpMultiplier = 1.5;

        /// <summary>Speed multiplier when the player chooses "Maintain Speed".</summary>
        private const double MaintainMultiplier = 1.0;

        // State

        /// <summary>The car the player selected before the race.</summary>
        public Car SelectedCar { get; private set; }

        /// <summary>The track the race is being run on.</summary>
        public Track RaceTrack { get; private set; }

        /// <summary>Time units remaining. Counts down each turn.</summary>
        public int TimeRemaining { get; private set; }

        /// <summary>Which turn we are on (1, 2, 3 ...).</summary>
        public int CurrentTurn { get; private set; }

        /// <summary>Current race status (InProgress, Finished, etc.).</summary>
        public RaceStatus Status { get; private set; }

        /// <summary>
        /// Queue of TurnResult structs. FIFO order for turn history.
        /// </summary>
        public Queue<TurnResult> TurnHistory { get; private set; }

        /// <summary>
        /// Available cars for selection. List allows ordered display and index lookup.
        /// </summary>
        public List<Car> AvailableCars { get; private set; }

        // Constructor

        /// <summary>
        /// Sets up the RaceManager and populates the list of available cars.
        /// Call StartRace() after the player picks a car to begin.
        /// </summary>
        public RaceManager()
        {
            TurnHistory = new Queue<TurnResult>();
            Status = RaceStatus.NotStarted;

            AvailableCars = new List<Car>
            {
                new Car("Rocket Racer",   speedPerTurn: 20, fuelConsumptionPerTurn: 15, fuelCapacity: 80),
                new Car("Eco Cruiser",    speedPerTurn: 12, fuelConsumptionPerTurn: 7,  fuelCapacity: 100),
                new Car("Balanced Beast", speedPerTurn: 16, fuelConsumptionPerTurn: 11, fuelCapacity: 90),
            };
        }

        // Public methods

        /// <summary>
        /// Starts the race with the car the player chose.
        /// Resets all state so the player can restart without creating a new manager.
        /// </summary>
        /// <param name="car">The car the player selected from AvailableCars.</param>
        /// <exception cref="ArgumentNullException">Thrown if no car is provided.</exception>
        public void StartRace(Car car)
        {
            SelectedCar = car ?? throw new ArgumentNullException(nameof(car), "You must select a car before starting!");

            // Reset everything to a fresh state
            RaceTrack    = new Track(5);
            TimeRemaining = StartingTime;
            CurrentTurn  = 0;
            Status       = RaceStatus.InProgress;
            TurnHistory.Clear();
        }

        /// <summary>
        /// Processes one turn of the game based on the player's chosen action.
        /// This is the core game loop step – called every time the player clicks a button.
        /// </summary>
        /// <param name="action">What the player wants to do this turn.</param>
        /// <returns>A TurnResult struct describing what happened.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the race is not in progress.</exception>
        public TurnResult ProcessTurn(PlayerAction action)
        {
            if (Status != RaceStatus.InProgress)
                throw new InvalidOperationException("Cannot process a turn – the race is not in progress.");

            CurrentTurn++;

            double distanceCovered = 0;
            double fuelBurned      = 0;
            string message         = "";

            if (action == PlayerAction.SpeedUp)
            {
                // Speed up: cover 1.5× normal distance, burn 1.5× normal fuel
                distanceCovered = SelectedCar.SpeedPerTurn * SpeedUpMultiplier;
                fuelBurned      = SelectedCar.FuelConsumptionPerTurn * SpeedUpMultiplier;
                SelectedCar.BurnFuel(SpeedUpMultiplier);
                RaceTrack.AdvanceDistance(distanceCovered);
                TimeRemaining -= TimeForSpeedUp;
                message = $"🚀 Floored it! Covered {distanceCovered:F1} units, burned {fuelBurned:F1}L of fuel.";
            }
            else if (action == PlayerAction.MaintainSpeed)
            {
                // Maintain: normal speed, normal fuel
                distanceCovered = SelectedCar.SpeedPerTurn * MaintainMultiplier;
                fuelBurned      = SelectedCar.FuelConsumptionPerTurn * MaintainMultiplier;
                SelectedCar.BurnFuel(MaintainMultiplier);
                RaceTrack.AdvanceDistance(distanceCovered);
                TimeRemaining -= TimeForMaintain;
                message = $"🏎️  Steady pace. Covered {distanceCovered:F1} units, burned {fuelBurned:F1}L of fuel.";
            }
            else if (action == PlayerAction.PitStop)
            {
                // Pit stop: no movement, costs extra time, but refuels the car
                SelectedCar.Refuel(PitStopFuelAmount);
                TimeRemaining -= TimeForPitStop;
                message = $"⛽ Pit stop! Added {PitStopFuelAmount}L of fuel. No distance covered this turn.";
            }

            var result = new TurnResult
            {
                TurnNumber      = CurrentTurn,
                Action          = action,
                DistanceCovered = distanceCovered,
                FuelBurned      = fuelBurned,
                LapAfterTurn    = Math.Min(RaceTrack.CurrentLap, RaceTrack.TotalLaps),
                Message         = message
            };

            TurnHistory.Enqueue(result);

            UpdateRaceStatus();

            return result;
        }

        // Private helpers

        /// <summary>
        /// Checks all end conditions after every turn.
        /// Updates Status so the UI knows whether to show game-over screens.
        /// </summary>
        private void UpdateRaceStatus()
        {
            if (RaceTrack.IsRaceComplete)
                Status = RaceStatus.Finished;
            else if (SelectedCar.CurrentFuel <= 0)
                Status = RaceStatus.OutOfFuel;
            else if (TimeRemaining <= 0)
                Status = RaceStatus.TimeUp;
        }
    }
}
