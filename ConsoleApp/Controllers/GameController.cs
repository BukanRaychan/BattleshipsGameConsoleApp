namespace ConsoleApp.Controllers;

public class GameController
{
    public event Action<string>? OnMessage;
    public event Action? OnGameOver;

    public void Start()
    {
        
        OnMessage?.Invoke("Game started!");
    }
}