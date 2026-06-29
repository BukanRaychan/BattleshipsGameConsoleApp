using ConsoleApp.DTOs;
using ConsoleApp.Models;

namespace ConsoleApp.Services;

public interface IGameService
{
    public IPlayer? CurrentPlayer {get; set;}
    public ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto dto);
    public ShipPlacementResponseDto EditShipPlacement(EditShipPlacementDto dto);
    public AttackResponseDto StartAttackPhase();
    public AttackResponseDto Attack(AttackDto dto);
}