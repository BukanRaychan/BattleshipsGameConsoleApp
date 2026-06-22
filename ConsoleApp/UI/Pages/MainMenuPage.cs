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
        var action = AnsiConsole.Prompt(
            new SelectionPrompt<Action>()
                .Title("Main Menu")
                .HighlightStyle(new Style(Color.White, Color.DarkGreen, Decoration.Bold))
                .AddChoices(Action.Enter_Game, Action.Quit));

        return action switch
        {
            Action.Enter_Game => new GameInitPage(_controller),
            Action.Quit => null,
            _ => this
        };
    }
}
