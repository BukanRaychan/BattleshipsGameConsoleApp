using ConsoleApp.DTOs;

namespace ConsoleApp.Services;

public interface IGameService
{
    public ShipPlacementResponseDto StartPlacementPhase(StartPlacementPhaseDto dto);
}