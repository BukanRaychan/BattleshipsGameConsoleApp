using ConsoleApp.Controllers;
using ConsoleApp.DTOs;

namespace ConsoleApp.UI.Pages;

public class ShipPlacementPage : IPage
{
    private readonly GameController _controller;

    public ShipPlacementPage(GameController controller)
    {
        _controller = controller;
    }

    public IPage? Index()
    {
        Console.WriteLine("INSERT NAME");
        Console.Write("Player one name: "); string playerOneName = Console.ReadLine() ?? string.Empty;
        Console.Write("Player two name: "); string playerTwoName = Console.ReadLine() ?? string.Empty;

        _controller.StartPlacementPhase(new StartPlacementPhaseDto(playerOneName, playerTwoName));

        return new ShipPlacementPage(_controller);
    }
}
