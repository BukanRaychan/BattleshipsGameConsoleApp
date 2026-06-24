using ConsoleApp.DTOs;
using ConsoleApp.Services;
using ConsoleApp.Types;


namespace ConsoleApp.Controllers;

public class GameController
{
    private IGameService _gameService; 
    public event MessageDelegate? OnMessage;

    public GameController()
    {
        _gameService = new GameService(MsgEvent);
    }
    public void LoadGame()
    {
        OnMessage?.Invoke("Welcome to battleship game!!! ", MessageType.Info);
    }

    public ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto dto)
    {
        return _gameService.StartShipPlacementPhase(dto);
    }

    public ShipPlacementResponseDto EditShipPlacement(EditShipPlacementDto dto)
    {
        return _gameService.EditShipPlacement(dto);
    }

    private void MsgEvent(string msg, MessageType msgType)
    {
        OnMessage?.Invoke(msg, msgType);
    }   

}