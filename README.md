# C# Speed Rush

C# Speed Rush is a turn-based, time-focused racing simulation built with WPF (.NET 6).
The player must complete 5 laps by managing speed, fuel, and remaining time.

## Assignment Alignment (Quick View)

- WPF form-based UI implemented in `Views/MainWindow.xaml`
- Car selection with 3 cars and different attributes
- 5-lap track with lap-distance simulation (`Track.LapDistance = 100`)
- Turn-based actions: `SpeedUp`, `MaintainSpeed`, `PitStop`
- Race ends when laps are completed or fuel/time reaches zero
- UI includes lap, fuel, time, status, action buttons, car ComboBox, and text progress bar
- Uses classes (`Car`, `Track`, `RaceManager`), `List`, `Queue`, `Enum`, and `struct`
- Includes exception handling and XML documentation comments
- Includes unit tests (5 tests, exceeding minimum requirement)

## How to Play

1. Select a car in the ComboBox.
2. Click **START RACE**.
3. Choose one action each turn:
   - **Speed Up**: more distance, more fuel usage, lower time cost
   - **Maintain Speed**: balanced distance and fuel usage
   - **Pit Stop**: add fuel, no distance gain, higher time cost
4. Win by finishing all 5 laps before fuel or time runs out.

## Car Options

| Car | Speed/Turn | Fuel/Turn | Fuel Capacity | Strategy |
|-----|------------|-----------|---------------|----------|
| Rocket Racer | 20 | 15 | 80 | Fast but fuel-hungry |
| Eco Cruiser | 12 | 7 | 100 | Slow but efficient |
| Balanced Beast | 16 | 11 | 90 | Mid-range balance |

## Run the Project

### Requirements

- Windows OS (WPF requirement)
- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- VS Code + C# extension, or Visual Studio 2022

### VS Code / Terminal

From repository root (`CSharpSpeedRush`):

```powershell
dotnet build
dotnet run --project .\CSharpSpeedRush\CSharpSpeedRush.csproj
```

### Run Unit Tests

```powershell
dotnet test .\CSharpSpeedRush.Tests\CSharpSpeedRush.Tests.csproj
```

## Project Structure

```text
CSharpSpeedRush/
├── CSharpSpeedRush/                 # Main WPF app
│   ├── Logic/
│   │   └── RaceManager.cs
│   ├── Models/
│   │   ├── Car.cs
│   │   ├── Track.cs
│   │   └── GameEnums.cs
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   ├── App.xaml
│   └── CSharpSpeedRush.csproj
├── CSharpSpeedRush.Tests/
│   ├── RaceManagerTests.cs
│   └── CSharpSpeedRush.Tests.csproj
└── docs/
    ├── Architecture.md
    ├── TestingStrategy.md
    ├── RequirementChecklist.md
    ├── CodeGuide.md
    ├── VideoDemoScript.md
    ├── PresentationScript.md
    └── SubmissionSummary.md
```

## Documentation

- Architecture and design: `docs/Architecture.md`
- Testing strategy: `docs/TestingStrategy.md`
- Requirement-by-requirement evidence: `docs/RequirementChecklist.md`
- Plain-language code explanation: `docs/CodeGuide.md`
- Video narration script: `docs/VideoDemoScript.md`
- Presentation script and Q&A prep: `docs/PresentationScript.md`
- Final submission links/template: `docs/SubmissionSummary.md`

## Unit Tests Included

- `BurnFuel_FullSpeed_ReducesFuelByCorrectAmount`
- `Refuel_WhenNearlyFull_DoesNotExceedCapacity`
- `Track_AdvanceDistance_CompletesLapAtCorrectDistance`
- `RaceManager_ProcessTurn_EndsRaceWhenFuelRunsOut`
- `Car_BurnFuel_ThrowsException_WhenTankEmpty`

## Notes

- XML documentation comments are embedded in the source code.
- The game logic is separated from the UI to support testability.
