using ConsoleApp.Models;
using ConsoleApp.Types;

namespace ConsoleApp.DTOs;

public record ShipPlacementResponseDto(
    IBoard Board,
    List<IShip> Ships,
    IPlayer CurrentPlayer,
    IShip? SelectedShip = null,
    Coordinate IndexPlayerCursor = new(),
    bool IsValidPlacement = false
);