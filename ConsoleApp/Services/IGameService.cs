using ConsoleApp.DTOs;
using ConsoleApp.Models;

namespace ConsoleApp.Services;

public interface IGameService
{
    public IPlayer? CurrentPlayer {get; set;}
    public ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto startPlacementPhaseDto);
    public ShipPlacementResponseDto PlaceShip(EditShipPlacementDto editShipPlacementDto);
    public AttackResponseDto StartAttackPhase();
    public AttackResponseDto Attack(AttackDto attackDto);
}