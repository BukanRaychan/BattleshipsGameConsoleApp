using ConsoleApp.DTOs;
using ConsoleApp.Models;
using ConsoleApp.Types;

namespace ConsoleApp.Services;

public interface IGameService
{
    event MessageDelegate? OnMessage;
    IPlayer? CurrentPlayer { get; set; }
    ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto startPlacementPhaseDto);
    ShipPlacementResponseDto PlaceShip(EditShipPlacementDto editShipPlacementDto);
    AttackResponseDto StartAttackPhase();
    AttackResponseDto Attack(AttackDto attackDto);
}
