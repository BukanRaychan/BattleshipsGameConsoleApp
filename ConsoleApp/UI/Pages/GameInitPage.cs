using ConsoleApp.Controllers;
using ConsoleApp.DTOs;
using Spectre.Console;

namespace ConsoleApp.UI.Pages;

public class GameInitPage : IPage
{
    private readonly GameController _controller;

    public GameInitPage(GameController controller)
    {
        _controller = controller;
    }

    public IPage? Index()
    {
        AnsiConsole.Write(
            new Rule($"[bold yellow]BATTLESHIPS[/]  [dim]|[/]  [cyan]Insert Players Name[/]")
                .LeftJustified()
        );
        Console.WriteLine();
        string playerOneName = AnsiConsole.Ask<string>("Insert first player's name?\t");
        string playerTwoName = AnsiConsole.Ask<string>("Insert second player's name?\t");

        ShipPlacementResponseDto state = _controller.PlaceShip(new StartPlacementPhaseDto(playerOneName, playerTwoName));

        return new ShipPlacementPage(_controller, state);
    }
}
