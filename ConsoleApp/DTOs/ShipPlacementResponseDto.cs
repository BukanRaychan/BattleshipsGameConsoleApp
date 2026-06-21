using ConsoleApp.Models;

namespace ConsoleApp.DTOs;

public record ShipPlacementResponseDto(IBoard Board, List<IShip> Ship, IPlayer CurrentPlayer);