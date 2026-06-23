using ConsoleApp.DTOs;
using ConsoleApp.Services;


namespace ConsoleApp.Controllers;

public class GameController
{
    private IGameService _gameService; 
    public event Action<string>? OnMessage;
    public event Action? OnGameOver;

    public GameController()
    {
        _gameService = new GameService();
    }
    public void LoadGame()
    {
        OnMessage?.Invoke("Welcome to battleship game!!! ", false);
    }

    public ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto dto)
    {
        return _gameService.StartShipPlacementPhase(dto);
    }

    public ShipPlacementResponseDto EditShipPlacement(EditShipPlacementDto dto)
    {
        return _gameService.EditShipPlacement(dto);
    }

    private void Debug()
    {
        
    }

}