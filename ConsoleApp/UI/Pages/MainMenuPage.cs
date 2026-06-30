using ConsoleApp.Controllers;
using Spectre.Console;

namespace ConsoleApp.UI.Pages;

public class MainMenuPage : IPage
{
    private readonly GameController _controller;
    private enum Action
    {
        Enter_Game,
        Quit
    }


    public MainMenuPage(GameController controller)
    {
        _controller = controller;
    }

    public IPage? Index()
    {
        AnsiConsole.Write(
            new Rule($"[bold yellow]BATTLESHIPS[/]  [dim]|[/]  [cyan]Main Menu[/]")
                .LeftJustified()
        );
        Console.WriteLine();
        Action action = AnsiConsole.Prompt(
            new SelectionPrompt<Action>()
                .HighlightStyle(new Style(Color.Black, Color.Chartreuse1, Decoration.Bold))
                .AddChoices(Action.Enter_Game, Action.Quit));

        IPage? nextPage = action switch
        {
            Action.Enter_Game => new GameInitPage(_controller),
            Action.Quit       => null,
            _                 => this
        };
        return nextPage;
    }
}
