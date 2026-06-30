using ConsoleApp.DTOs;
using ConsoleApp.Services;
using ConsoleApp.Types;


namespace ConsoleApp.Controllers;

public class GameController
{
    private readonly IGameService _gameService;
    public event MessageDelegate? OnMessage;

    public GameController(IGameService gameService)
    {
        _gameService = gameService;
        _gameService.OnMessage += RaiseMessage;
    }

    public ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto startPlacementPhaseDto) =>
        _gameService.StartShipPlacementPhase(startPlacementPhaseDto);

    public ShipPlacementResponseDto PlaceShip(EditShipPlacementDto editShipPlacementDto) =>
        _gameService.PlaceShip(editShipPlacementDto);

    public AttackResponseDto StartAttackPhase() =>
        _gameService.StartAttackPhase();

    public AttackResponseDto Attack(AttackDto attackDto) =>
        _gameService.Attack(attackDto);

    private void RaiseMessage(string msg, MessageType msgType)
    {
        OnMessage?.Invoke(msg, msgType);
    }
}
