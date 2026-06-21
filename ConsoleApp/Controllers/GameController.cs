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

    public void StartPlacementPhase()
    {
        
    }

    public void StartGame()
    {
        OnMessage?.Invoke("Game started!");
    }
}