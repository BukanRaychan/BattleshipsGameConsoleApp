# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build          # Build the solution
dotnet run            # Run the game
```

No test project exists yet. The single solution file is `ConsoleApp.sln`.

## Architecture

This is a .NET 8 console Battleships game using **Spectre.Console** for rich terminal UI. The structure follows an MVC-inspired layered pattern:

```
Program.Main → App.Run() → GameLayout (render loop) → Pages (navigation)
                                  ↕
                          GameController
                                  ↕
                           GameService
```

### Layers

**UI Layer** (`ConsoleApp/UI/`)
- `App.cs` — bootstraps the app, runs the page navigation loop
- `GameLayout.cs` — renders the banner, listens to `GameController` events, calls `IPage.Index()` to get the next page
- `IPage.cs` — every page implements this; `Index()` returns the next `IPage` (or null to quit)
- Pages in `UI/Pages/`: `MainMenuPage`, `GameInitPage`, `ShipPlacementPage`

**Controller** (`ConsoleApp/Controllers/GameController.cs`)
- Central coordinator between UI and service
- Exposes two events the UI subscribes to: `OnMessage` (string) and `OnGameOver` (void)
- Key methods: `StartPlacementPhase()`, `LoadGame()`

**Service** (`ConsoleApp/Services/`)
- `IGameService` / `GameService` — game logic (ship placement validation, attack mechanics)
- `StartPlacementPhase(StartPlacementPhaseDto)` returns `ShipPlacementResponseDto`
- Core logic is largely unimplemented (stubs throw `NotImplementedException`)

**Models** (`ConsoleApp/Models/`)
- All entities have interfaces: `IPlayer`, `IShip`, `IBoard`, `ICell`
- `Board` is a 10×10 grid of `Cell` objects
- `Cell` holds a `Coordinate`, an optional ship reference, and an optional `AttackResult`

**Types** (`ConsoleApp/Types/`)
- `ShipType` enum — values equal ship length (Destroyer=2 … Carrier=5)
- `Orientation` — Horizontal / Vertical
- `AttackResult` — Hit, Miss, Sunk
- `Coordinate` — struct combining `HorizontalLabel` (1–10) and `VerticalLabel` (A–J)

**DTOs** (`ConsoleApp/DTOs/`) — C# record types
- `StartPlacementPhaseDto` — player names + board size
- `ShipPlacementResponseDto` — board, ships, and current player

### Current State

The architectural skeleton is in place but gameplay is incomplete:
- `GameService.StartPlacementPhase()` throws `NotImplementedException`
- `ShipPlacementPage` is a stub
- `Event/` directory is empty (reserved for future game events)
- No attack/game-loop logic exists yet

### Key Design Decisions

- Page navigation is done by each `IPage.Index()` returning the next page instance (or `null` to exit)
- `GameController` uses C# events (`Action<string>` / `Action`) to decouple UI from game state changes
- DTOs are `record` types for immutability
- Spectre.Console `SelectionPrompt` is used for menu interactions
