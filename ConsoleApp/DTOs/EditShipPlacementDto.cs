using ConsoleApp.Models;
using ConsoleApp.Types;

namespace ConsoleApp.DTOs;

public record EditShipPlacementDto(Ship? CurrentShip, ConsoleKey KeyEvent, Coordinate IndexPlayerCursor);