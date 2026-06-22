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

    public void StartPlacementPhase(StartPlacementPhaseDto dto)
    {
        
    }

    public void LoadGame()
    {
        OnMessage?.Invoke("Game started!");
    }
}