using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media;
using CSharpSpeedRush.Logic;
using CSharpSpeedRush.Models;

namespace CSharpSpeedRush.Views
{
    /// <summary>
    /// Main window code-behind. Connects UI events to RaceManager.
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// The game logic engine. All rules live in here, not in this file.
        /// </summary>
        private readonly RaceManager _raceManager;

        // Animated race progression (player + 2 opponent markers)
        private readonly DispatcherTimer _animationTimer;

        private const double PlayerLaneY = 22;
        private const double OpponentLane1Y = 29; // centered on the dashed line
        private const double OpponentLane2Y = 36;

        private double _playerProgress;
        private double _playerTargetProgress;
        private double _opponent1Progress;
        private double _opponent1TargetProgress;
        private double _opponent2Progress;
        private double _opponent2TargetProgress;

        private const int AnimationFrameMs = 30;
        private const int TurnAnimationDurationMs = 900;

        // Time-based interpolation so fuel/time/lap + markers animate together.
        private DateTime? _turnAnimationStartUtc;
        private double _playerProgressStart;
        private double _opponent1ProgressStart;
        private double _opponent2ProgressStart;

        // UI interpolation snapshot (RaceManager has already advanced state by the time
        // we start the animation; we interpolate from the previous snapshot -> target).
        private double _fuelStart;
        private double _fuelTarget;
        private double _fuelCapacityForAnimation;
        private int _timeStart;
        private int _timeTarget;

        private double _drivenDistanceStart;
        private double _drivenDistanceTarget;

        private bool _raceEndPending;

        private Track? _opponentTrack1;
        private Track? _opponentTrack2;
        private Car? _opponentCar1;
        private Car? _opponentCar2;


        /// <summary>
        /// Initializes window and car selection ComboBox.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            _raceManager = new RaceManager();
            foreach (var car in _raceManager.AvailableCars)
            {
                CarComboBox.Items.Add(car.ToString());
            }
            CarComboBox.SelectedIndex = 0;
            ApplyPlayerColorForSelectedCar(0);

            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(AnimationFrameMs)
            };
            _animationTimer.Tick += AnimationTimer_Tick;

            Loaded += (_, _) => ResetRaceCanvas();
        }

        // ──────────────────────────────────────────────
        // Button Event Handlers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Called when the player clicks "START RACE".
        /// Grabs the selected car and tells the RaceManager to begin.
        /// </summary>
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Figure out which car index the player picked in the dropdown
                int selectedIndex = CarComboBox.SelectedIndex;
                if (selectedIndex < 0)
                {
                    MessageBox.Show("Please select a car first!", "No Car Selected",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Car chosenCar = _raceManager.AvailableCars[selectedIndex];
                _raceManager.StartRace(chosenCar);
                TimeProgressBar.Maximum = RaceManager.StartingTime;

                // Disable car selection during race
                CarComboBox.IsEnabled  = false;
                StartButton.IsEnabled  = false;

                SetActionButtonsEnabled(true);
                RestartButton.IsEnabled = true;

                // Opponents are only used for the visual “race progression” animation.
                var opponents = _raceManager.AvailableCars
                    .Where(c => !ReferenceEquals(c, chosenCar))
                    .Take(2)
                    .ToList();

                _opponentCar1 = opponents.Count > 0 ? opponents[0] : _raceManager.AvailableCars[0];
                _opponentCar2 = opponents.Count > 1 ? opponents[1] : _raceManager.AvailableCars[1];

                _opponentTrack1 = new Track(5);
                _opponentTrack2 = new Track(5);
                ResetRaceCanvas();

                LogTextBlock.Text = $"🏁 Race started with {chosenCar.Name}!\n" +
                                    $"   Speed: {chosenCar.SpeedPerTurn}  |  Fuel Use: {chosenCar.FuelConsumptionPerTurn}/turn\n" +
                                    $"   5 laps to go. Good luck!\n" +
                                    new string('─', 50) + "\n";

                RefreshUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not start the race:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>Handles the "Speed Up" button click.</summary>
        private void SpeedUpButton_Click(object sender, RoutedEventArgs e)
            => HandleAction(PlayerAction.SpeedUp);

        /// <summary>Handles the "Maintain Speed" button click.</summary>
        private void MaintainButton_Click(object sender, RoutedEventArgs e)
            => HandleAction(PlayerAction.MaintainSpeed);

        /// <summary>Handles the "Pit Stop" button click.</summary>
        private void PitStopButton_Click(object sender, RoutedEventArgs e)
            => HandleAction(PlayerAction.PitStop);

        /// <summary>
        /// Resets the game so the player can try again without restarting the app.
        /// </summary>
        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            CarComboBox.IsEnabled  = true;
            StartButton.IsEnabled  = true;
            RestartButton.IsEnabled = false;

            SetActionButtonsEnabled(false);

            LapLabel.Text        = "–/5";
            FuelLabel.Text       = "–";
            TimeLabel.Text       = "–";
            StatusLabel.Text     = "Waiting";
            ProgressBarText.Text = "[                    >]";
            FuelProgressBar.Value = 100;
            TimeProgressBar.Maximum = RaceManager.StartingTime;
            TimeProgressBar.Value = RaceManager.StartingTime;

            LogTextBlock.Text = "Select a car and press START RACE to begin!";

            ResetRaceCanvas();
        }

        /// <summary>
        /// Handles Speed Up, Maintain Speed, and Pit Stop. Calls RaceManager and updates UI.
        /// </summary>
        /// <param name="action">The action chosen by the player.</param>
        private void HandleAction(PlayerAction action)
        {
            try
            {
                if (_turnAnimationStartUtc != null)
                    return;

                // Capture "before" snapshot for smooth UI interpolation.
                var carStart = _raceManager.SelectedCar;
                var trackStart = _raceManager.RaceTrack;

                _fuelStart = carStart.CurrentFuel;
                _fuelCapacityForAnimation = carStart.FuelCapacity;
                _timeStart = _raceManager.TimeRemaining;
                _drivenDistanceStart = ComputeDrivenDistance(trackStart);

                _playerProgressStart = _playerProgress;
                _opponent1ProgressStart = _opponent1Progress;
                _opponent2ProgressStart = _opponent2Progress;

                TurnResult result = _raceManager.ProcessTurn(action);

                // Capture "after" snapshot for smooth UI interpolation.
                _fuelTarget = _raceManager.SelectedCar.CurrentFuel;
                _timeTarget = _raceManager.TimeRemaining;
                _drivenDistanceTarget = ComputeDrivenDistance(_raceManager.RaceTrack);
                _raceEndPending = _raceManager.Status != RaceStatus.InProgress;

                SetActionButtonsEnabled(false);

                // Update animated race progression (player + 2 opponent markers).
                AdvanceOpponentTracks(action);
                SetRaceCanvasTargetsFromTracks();

                // Show starting values immediately (prevents instant jumps).
                ApplyUIFromInterpolatedState(ease01: 0);
                BeginRaceCanvasAnimation();

                LogTextBlock.Text += $"\nTurn {result.TurnNumber}: {result.Message}";
                LogTextBlock.Text += $"\n  → Lap {result.LapAfterTurn}/5  |  " +
                                     $"Fuel: {_raceManager.SelectedCar.CurrentFuel:F1}L  |  " +
                                     $"Time: {_raceManager.TimeRemaining}\n";

                LogScrollViewer.ScrollToBottom();
            }
            catch (InvalidOperationException ex)
            {
                // Expected errors (e.g. out of fuel): show as a message box
                MessageBox.Show(ex.Message, "Illegal Move",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                // Unexpected errors: show full detail
                MessageBox.Show($"Unexpected error:\n{ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Updates all labels and progress bars from RaceManager state.
        /// </summary>
        private void RefreshUI()
        {
            var car   = _raceManager.SelectedCar;
            var track = _raceManager.RaceTrack;
            int lap   = Math.Min(track.CurrentLap, track.TotalLaps);

            LapLabel.Text    = $"{lap}/{track.TotalLaps}";
            FuelLabel.Text   = $"{car.CurrentFuel:F1}L";
            TimeLabel.Text   = $"{Math.Max(0, _raceManager.TimeRemaining)}";
            StatusLabel.Text = _raceManager.Status.ToString();

            FuelProgressBar.Value = car.FuelPercentage;
            TimeProgressBar.Value = Math.Max(0, _raceManager.TimeRemaining);

            ProgressBarText.Text = track.GetProgressBar(20) + $"  {track.LapProgressPercent:F0}%";

            FuelProgressBar.Foreground = car.FuelPercentage < 25
                ? new SolidColorBrush(Colors.OrangeRed)
                : new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)); // neon green

            TimeProgressBar.Foreground = _raceManager.TimeRemaining < 15
                ? new SolidColorBrush(Colors.OrangeRed)
                : new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)); // blue
        }

        private static double ComputeDrivenDistance(Track track)
        {
            // Driven distance over the whole race (across all laps).
            return (track.CurrentLap - 1) * Track.LapDistance + track.DistanceInCurrentLap;
        }

        private static double EaseOutCubic(double t01)
        {
            // Smooths motion without overshooting.
            return 1 - Math.Pow(1 - t01, 3);
        }

        private static string BuildProgressBar(double distanceInLap, int width)
        {
            int filled = (int)((distanceInLap / Track.LapDistance) * width);
            filled = Math.Clamp(filled, 0, width);
            string bar = new string('=', filled);
            string empty = new string(' ', width - filled);
            return $"[{bar}>{empty}]";
        }

        private void ApplyUIFromInterpolatedState(double ease01)
        {
            var track = _raceManager.RaceTrack;
            int totalLaps = track.TotalLaps;
            double totalDistance = totalLaps * Track.LapDistance;

            double virtualDriven = _drivenDistanceStart +
                                    (_drivenDistanceTarget - _drivenDistanceStart) * ease01;
            virtualDriven = Math.Clamp(virtualDriven, 0, totalDistance);

            double virtualFuel = _fuelStart + (_fuelTarget - _fuelStart) * ease01;
            int virtualTime = (int)Math.Round(_timeStart + (_timeTarget - _timeStart) * ease01);

            int virtualLap;
            double virtualDistanceInLap;

            if (virtualDriven >= totalDistance - 1e-9)
            {
                virtualLap = totalLaps;
                virtualDistanceInLap = Track.LapDistance;
            }
            else
            {
                virtualLap = (int)(virtualDriven / Track.LapDistance) + 1;
                virtualLap = Math.Clamp(virtualLap, 1, totalLaps);
                virtualDistanceInLap = virtualDriven - (virtualLap - 1) * Track.LapDistance;
            }

            LapLabel.Text = $"{virtualLap}/{totalLaps}";
            FuelLabel.Text = $"{virtualFuel:F1}L";
            TimeLabel.Text = $"{Math.Max(0, virtualTime)}";
            StatusLabel.Text = _raceManager.Status.ToString();

            FuelProgressBar.Value = (virtualFuel / _fuelCapacityForAnimation) * 100.0;
            TimeProgressBar.Value = Math.Max(0, virtualTime);

            double lapPercent = (virtualDistanceInLap / Track.LapDistance) * 100.0;
            ProgressBarText.Text = BuildProgressBar(virtualDistanceInLap, 20) + $"  {lapPercent:F0}%";

            FuelProgressBar.Foreground = (virtualFuel / _fuelCapacityForAnimation) * 100.0 < 25
                ? new SolidColorBrush(Colors.OrangeRed)
                : new SolidColorBrush(Color.FromRgb(0x39, 0xFF, 0x14)); // neon green

            TimeProgressBar.Foreground = virtualTime < 15
                ? new SolidColorBrush(Colors.OrangeRed)
                : new SolidColorBrush(Color.FromRgb(0x4F, 0xC3, 0xF7)); // blue
        }

        /// <summary>
        /// Handles race end: shows message and disables action buttons.
        /// </summary>
        private void HandleRaceEnd()
        {
            if (_raceManager.Status == RaceStatus.InProgress) return;

            SetActionButtonsEnabled(false);

            string title, message;

            switch (_raceManager.Status)
            {
                case RaceStatus.Finished:
                    title   = "🏆 You Won!";
                    message = $"Congratulations! You completed all 5 laps in {_raceManager.CurrentTurn} turns!\n" +
                              $"Time remaining: {_raceManager.TimeRemaining}\n" +
                              $"Fuel remaining: {_raceManager.SelectedCar.CurrentFuel:F1}L";
                    LogTextBlock.Text += $"\n🏆 RACE COMPLETE! All 5 laps finished!\n";
                    break;

                case RaceStatus.OutOfFuel:
                    title   = "💀 Out of Fuel!";
                    message = "Your tank is empty and you couldn't finish the race.\nTry using more pit stops next time!";
                    LogTextBlock.Text += $"\n💀 RACE OVER: Out of fuel on lap {_raceManager.RaceTrack.CurrentLap}!\n";
                    break;

                case RaceStatus.TimeUp:
                    title   = "⏰ Time's Up!";
                    message = "You ran out of time before finishing all the laps.\nTry speeding up more often next time!";
                    LogTextBlock.Text += $"\n⏰ RACE OVER: Time expired on lap {_raceManager.RaceTrack.CurrentLap}!\n";
                    break;

                default:
                    return;
            }

            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Enables or disables all action buttons.
        /// </summary>
        private void SetActionButtonsEnabled(bool enabled)
        {
            SpeedUpButton.IsEnabled  = enabled;
            MaintainButton.IsEnabled = enabled;
            PitStopButton.IsEnabled  = enabled;
        }

        /// <summary>
        /// Updates status preview when car selection changes.
        /// </summary>
        private void CarComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int idx = CarComboBox.SelectedIndex;
            if (idx >= 0 && _raceManager.Status == RaceStatus.NotStarted)
            {
                var preview = _raceManager.AvailableCars[idx];
                StatusLabel.Text = $"Spd:{preview.SpeedPerTurn}";
            }

            if (idx >= 0)
                ApplyPlayerColorForSelectedCar(idx);
        }

        private void ApplyPlayerColorForSelectedCar(int idx)
        {
            if (idx < 0 || idx >= _raceManager.AvailableCars.Count)
                return;

            // Map car "personality" to a high-contrast marker color for visibility.
            // (We match by name because cars are created inside RaceManager.)
            string carName = _raceManager.AvailableCars[idx].Name;

            Color fill = carName switch
            {
                "Rocket Racer" => Color.FromRgb(0xE7, 0x4C, 0x3C),  // red
                "Eco Cruiser" => Color.FromRgb(0x2E, 0xC3, 0x71),    // green
                "Balanced Beast" => Color.FromRgb(0x34, 0x98, 0xDB), // blue
                _ => Color.FromRgb(0x39, 0xFF, 0x14)                 // neon green fallback
            };

            PlayerMarker.Fill = new SolidColorBrush(fill);
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (_turnAnimationStartUtc == null)
                return;

            double elapsedMs = (DateTime.UtcNow - _turnAnimationStartUtc.Value).TotalMilliseconds;
            double t01 = Math.Clamp(elapsedMs / TurnAnimationDurationMs, 0, 1);
            double ease01 = EaseOutCubic(t01);

            _playerProgress   = _playerProgressStart   + (_playerTargetProgress   - _playerProgressStart)   * ease01;
            _opponent1Progress = _opponent1ProgressStart + (_opponent1TargetProgress - _opponent1ProgressStart) * ease01;
            _opponent2Progress = _opponent2ProgressStart + (_opponent2TargetProgress - _opponent2ProgressStart) * ease01;

            UpdateRaceCanvasMarkers();
            ApplyUIFromInterpolatedState(ease01);

            if (t01 < 1) return;

            _turnAnimationStartUtc = null;
            _animationTimer.Stop();

            // Snap to final values to avoid rounding drift.
            _playerProgress = _playerTargetProgress;
            _opponent1Progress = _opponent1TargetProgress;
            _opponent2Progress = _opponent2TargetProgress;
            UpdateRaceCanvasMarkers();

            RefreshUI();

            bool raceEnd = _raceEndPending;
            _raceEndPending = false;

            if (raceEnd)
                HandleRaceEnd();
            else
                SetActionButtonsEnabled(true);
        }

        private void ResetRaceCanvas()
        {
            _animationTimer.Stop();
            _turnAnimationStartUtc = null;
            _raceEndPending = false;

            _playerProgress = 0;
            _playerTargetProgress = 0;
            _playerProgressStart = 0;

            _opponent1Progress = 0;
            _opponent1TargetProgress = 0;
            _opponent1ProgressStart = 0;

            _opponent2Progress = 0;
            _opponent2TargetProgress = 0;
            _opponent2ProgressStart = 0;

            UpdateRaceCanvasMarkers();
        }

        private static double ComputeOverallProgress(Track track)
        {
            double totalDistance = track.TotalLaps * Track.LapDistance;
            if (totalDistance <= 0) return 0;

            double drivenDistance =
                (track.CurrentLap - 1) * Track.LapDistance +
                track.DistanceInCurrentLap;

            return Math.Clamp(drivenDistance / totalDistance, 0, 1);
        }

        private void SetMarkerPosition(Ellipse marker, double progress01, double laneY)
        {
            double width = RaceCanvas.ActualWidth;
            if (width <= 1) width = 640; // fallback to XAML width

            const double leftBound = 10;
            double rightBound = width - 10;

            double markerHalf = marker.Width / 2.0;
            double x = leftBound + (rightBound - leftBound) * progress01 - markerHalf;

            Canvas.SetLeft(marker, x);
            Canvas.SetTop(marker, laneY);
        }

        private void UpdateRaceCanvasMarkers()
        {
            SetMarkerPosition(PlayerMarker, _playerProgress, PlayerLaneY);
            SetMarkerPosition(Opponent1Marker, _opponent1Progress, OpponentLane1Y);
            SetMarkerPosition(Opponent2Marker, _opponent2Progress, OpponentLane2Y);
        }

        private void BeginRaceCanvasAnimation()
        {
            _animationTimer.Stop();
            _turnAnimationStartUtc = DateTime.UtcNow;
            _animationTimer.Start();
        }

        private void AdvanceOpponentTracks(PlayerAction action)
        {
            if (_opponentTrack1 == null || _opponentTrack2 == null || _opponentCar1 == null || _opponentCar2 == null)
                return;

            // Pit stop slows the player to 0 movement, but opponents keep going.
            double opponentActionMult = action switch
            {
                PlayerAction.SpeedUp => 1.2,
                PlayerAction.MaintainSpeed => 1.0,
                PlayerAction.PitStop => 0.95,
                _ => 1.0
            };

            double opponent1TurnDistance = _opponentCar1.SpeedPerTurn * opponentActionMult;
            double opponent2TurnDistance = _opponentCar2.SpeedPerTurn * opponentActionMult;

            _opponentTrack1.AdvanceDistance(opponent1TurnDistance);
            _opponentTrack2.AdvanceDistance(opponent2TurnDistance);
        }

        private void SetRaceCanvasTargetsFromTracks()
        {
            if (_raceManager.RaceTrack == null) return;

            _playerTargetProgress = ComputeOverallProgress(_raceManager.RaceTrack);

            if (_opponentTrack1 != null)
                _opponent1TargetProgress = ComputeOverallProgress(_opponentTrack1);
            if (_opponentTrack2 != null)
                _opponent2TargetProgress = ComputeOverallProgress(_opponentTrack2);
        }
    }
}
