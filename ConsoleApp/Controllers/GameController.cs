using ConsoleApp.DTOs;
using ConsoleApp.Services;
using ConsoleApp.Types;


namespace ConsoleApp.Controllers;

public class GameController
{
    private IGameService? _gameService; 
    public event MessageDelegate? OnMessage;

    public ShipPlacementResponseDto PlaceShip(StartPlacementPhaseDto startPlacementPhaseDto)
    {
        _gameService = new GameService(MsgEvent);
        return _gameService.StartShipPlacementPhase(startPlacementPhaseDto);
    }

    public ShipPlacementResponseDto PlaceShip(EditShipPlacementDto editShipPlacementDto) => _gameService!.PlaceShip(editShipPlacementDto);

    public AttackResponseDto StartAttackPhase() => _gameService!.StartAttackPhase();

    public AttackResponseDto Attack(AttackDto attackDto) => _gameService!.Attack(attackDto);

    private void MsgEvent(string msg, MessageType msgType)
    {
        OnMessage?.Invoke(msg, msgType);
    }   

}