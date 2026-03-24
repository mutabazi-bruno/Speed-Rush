using System;

namespace CSharpSpeedRush.Models
{
    /// <summary>
    /// Represents a racing car with speed, fuel consumption, and tank capacity.
    /// </summary>
    public class Car
    {
        // Properties

        /// <summary>The display name of the car (e.g. "Rocket Racer").</summary>
        public string Name { get; private set; }

        /// <summary>
        /// How many distance units this car covers each turn when going at full speed.
        /// Higher = faster, but burns more fuel.
        /// </summary>
        public double SpeedPerTurn { get; private set; }

        /// <summary>
        /// How many fuel units this car burns each turn at full speed.
        /// Lower = more efficient.
        /// </summary>
        public double FuelConsumptionPerTurn { get; private set; }

        /// <summary>
        /// The maximum amount of fuel this car can hold.
        /// Larger tank = fewer pit stops needed.
        /// </summary>
        public double FuelCapacity { get; private set; }

        /// <summary>Current fuel level. Starts full.</summary>
        public double CurrentFuel { get; private set; }

        // Constructor

        /// <summary>
        /// Creates a new car with the given stats and starts it with a full tank.
        /// </summary>
        /// <param name="name">Car's display name.</param>
        /// <param name="speedPerTurn">Distance covered per turn at full speed.</param>
        /// <param name="fuelConsumptionPerTurn">Fuel burned per turn at full speed.</param>
        /// <param name="fuelCapacity">Maximum fuel the tank can hold.</param>
        public Car(string name, double speedPerTurn, double fuelConsumptionPerTurn, double fuelCapacity)
        {
            Name = name;
            SpeedPerTurn = speedPerTurn;
            FuelConsumptionPerTurn = fuelConsumptionPerTurn;
            FuelCapacity = fuelCapacity;

            CurrentFuel = fuelCapacity;
        }

        // Methods

        /// <summary>
        /// Burns fuel based on the speed multiplier the player chose.
        /// For example, a multiplier of 0.5 = half speed = half fuel used.
        /// </summary>
        /// <param name="speedMultiplier">A value between 0 and 1 representing speed level.</param>
        /// <exception cref="InvalidOperationException">Thrown when there is not enough fuel to drive.</exception>
        public void BurnFuel(double speedMultiplier)
        {
            double fuelNeeded = FuelConsumptionPerTurn * speedMultiplier;

            if (CurrentFuel <= 0)
                throw new InvalidOperationException($"{Name} is out of fuel! You must pit stop.");

            CurrentFuel = Math.Max(0, CurrentFuel - fuelNeeded);
        }

        /// <summary>
        /// Refuels the car during a pit stop. Adds a fixed amount of fuel.
        /// Cannot exceed the tank's maximum capacity.
        /// </summary>
        /// <param name="amount">How many fuel units to add.</param>
        /// <exception cref="ArgumentException">Thrown if amount is negative.</exception>
        public void Refuel(double amount)
        {
            if (amount < 0)
                throw new ArgumentException("You cannot remove fuel during a pit stop!");

            // Cap at tank capacity
            CurrentFuel = Math.Min(FuelCapacity, CurrentFuel + amount);
        }

        /// <summary>
        /// Returns the fuel level as a percentage (0–100) so we can show it on a progress bar.
        /// </summary>
        public double FuelPercentage => (CurrentFuel / FuelCapacity) * 100.0;

        /// <summary>
        /// Returns a friendly string showing the car's key stats.
        /// Used in the car selection screen.
        /// </summary>
        public override string ToString() =>
            $"{Name}  |  Speed: {SpeedPerTurn}  |  Fuel Use: {FuelConsumptionPerTurn}/turn  |  Tank: {FuelCapacity}L";
    }
}
