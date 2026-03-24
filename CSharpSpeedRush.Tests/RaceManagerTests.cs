using Microsoft.VisualStudio.TestTools.UnitTesting;
using CSharpSpeedRush.Models;
using CSharpSpeedRush.Logic;

namespace CSharpSpeedRush.Tests
{
    /// <summary>
    /// Unit tests for fuel consumption, refuelling, lap progression,
    /// race end conditions, and exception handling.
    /// </summary>
    [TestClass]
    public class RaceManagerTests
    {
        /// <summary>
        /// When a car drives at full speed, its fuel should decrease
        /// by exactly FuelConsumptionPerTurn units.
        /// </summary>
        [TestMethod]
        public void BurnFuel_FullSpeed_ReducesFuelByCorrectAmount()
        {
            // ARRANGE – set up a car we can predict
            var car = new Car("Test Car", speedPerTurn: 10, fuelConsumptionPerTurn: 10, fuelCapacity: 100);
            double fuelBefore = car.CurrentFuel; // Should be 100 (full tank)

            // ACT – burn fuel at full speed (multiplier = 1.0)
            car.BurnFuel(1.0);

            // ASSERT – fuel should now be 90
            double expectedFuel = fuelBefore - 10.0;
            Assert.AreEqual(expectedFuel, car.CurrentFuel, 0.001,
                "Fuel should decrease by exactly FuelConsumptionPerTurn when multiplier is 1.0");
        }

        /// <summary>
        /// Refuelling a nearly full car should cap at FuelCapacity.
        /// </summary>
        [TestMethod]
        public void Refuel_WhenNearlyFull_DoesNotExceedCapacity()
        {
            // ARRANGE – car with 90/100 fuel
            var car = new Car("Test Car", speedPerTurn: 10, fuelConsumptionPerTurn: 10, fuelCapacity: 100);
            car.BurnFuel(1.0); // Burn 10, now at 90

            // ACT – add 50 units (which would normally be 140, but cap at 100)
            car.Refuel(50);

            // ASSERT – should be exactly 100, not 140
            Assert.AreEqual(100.0, car.CurrentFuel, 0.001,
                "Fuel must never exceed the car's FuelCapacity after refuelling");
        }

        /// <summary>
        /// Advancing exactly 100 distance units should move from lap 1 to lap 2.
        /// </summary>
        [TestMethod]
        public void Track_AdvanceDistance_CompletesLapAtCorrectDistance()
        {
            // ARRANGE – fresh track, starting at lap 1
            var track = new Track(5);
            Assert.AreEqual(1, track.CurrentLap, "Should start on lap 1");

            // ACT – advance exactly one lap's worth of distance
            int lapsCompleted = track.AdvanceDistance(Track.LapDistance);

            // ASSERT – should now be on lap 2, with 1 lap completed
            Assert.AreEqual(1, lapsCompleted,   "Should have completed exactly 1 lap");
            Assert.AreEqual(2, track.CurrentLap, "Should now be on lap 2");
        }

        /// <summary>
        /// RaceStatus becomes OutOfFuel when tank is empty.
        /// </summary>
        [TestMethod]
        public void RaceManager_ProcessTurn_EndsRaceWhenFuelRunsOut()
        {
            // ARRANGE – a car with a tiny 10-unit tank that burns 7 per turn
            var manager = new RaceManager();
            var tinyTankCar = new Car("Tiny Tank", speedPerTurn: 10, fuelConsumptionPerTurn: 7, fuelCapacity: 10);
            manager.StartRace(tinyTankCar);

            // ACT – speed up twice; first turn burns 10.5 (but capped at 10), so second turn will have 0 fuel
            // Turn 1: 10L * 1.5 = burns up to 10, leaving 0
            manager.ProcessTurn(PlayerAction.SpeedUp);

            // ASSERT – race should now be over due to no fuel
            Assert.AreEqual(RaceStatus.OutOfFuel, manager.Status,
                "Race should end with OutOfFuel status when fuel reaches zero");
        }

        /// <summary>
        /// BurnFuel throws InvalidOperationException when tank is empty.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException),
            "Expected an exception when trying to drive with an empty tank")]
        public void Car_BurnFuel_ThrowsException_WhenTankEmpty()
        {
            // ARRANGE – burn all the fuel first
            var car = new Car("Empty Car", speedPerTurn: 10, fuelConsumptionPerTurn: 100, fuelCapacity: 50);
            car.BurnFuel(1.0); // Burns 100 units but tank only has 50 → CurrentFuel = 0

            // ACT – try to drive again with empty tank → should throw
            car.BurnFuel(1.0);
        }
    }
}
