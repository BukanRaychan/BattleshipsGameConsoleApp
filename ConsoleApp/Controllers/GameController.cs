using ConsoleApp.DTOs;
using ConsoleApp.Services;
using ConsoleApp.Types;


namespace ConsoleApp.Controllers;

public class GameController
{
    private IGameService? _gameService; 
    public event MessageDelegate? OnMessage;

    public ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto dto)
    {
        _gameService = new GameService(MsgEvent);
        return _gameService.StartShipPlacementPhase(dto);
    }

    public ShipPlacementResponseDto EditShipPlacement(EditShipPlacementDto dto) => _gameService!.EditShipPlacement(dto);

    public AttackResponseDto StartAttackPhase() => _gameService!.StartAttackPhase();

    public AttackResponseDto Attack(AttackDto dto) => _gameService!.Attack(dto);

    private void MsgEvent(string msg, MessageType msgType)
    {
        OnMessage?.Invoke(msg, msgType);
    }   

}