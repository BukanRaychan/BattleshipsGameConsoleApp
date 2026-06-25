using ConsoleApp.Controllers;
using ConsoleApp.DTOs;
using ConsoleApp.Types;
using Spectre.Console;

namespace ConsoleApp.UI.Pages;

public class TransitionPage : IPage
{
    private readonly GameController _controller;
    private readonly AttackResponseDto _state;

    public TransitionPage(GameController controller, AttackResponseDto state)
    {
        _controller = controller;
        _state = state;
    }

    public IPage? Index()
    {
        AnsiConsole.Write(
            new Rule($"[bold yellow]BATTLESHIPS[/]  [dim]|[/]  [cyan]Hand Off[/]")
                .LeftJustified()
        );
        AnsiConsole.WriteLine();

        if (_state.LastAttack != null && _state.LastResult != null)
        {
            var (icon, color) = _state.LastResult switch
            {
                AttackResult.Sunk => ("💥", "red"),
                AttackResult.Hit  => ("🎯", "darkorange"),
                _                 => ("💨", "steelblue1"),
            };

            var coord = _state.LastAttack.Value;
            string col = ((int)coord.X + 1).ToString();
            string row = coord.Y.ToString();

            // CurrentPlayer is already the NEXT player — the one who just attacked was the other one
            // We need the previous attacker's name. Since we switched players, we can infer it:
            // The previous attacker's board is now OpponentBoard.
            AnsiConsole.MarkupLine(
                $"  {icon} [bold]Last attack:[/] [{color}]{Markup.Escape(_state.LastResult.Value.ToString())}[/] at [bold]{row}{col}[/]"
            );
            AnsiConsole.WriteLine();
        }

        AnsiConsole.Write(
            new Rule($"[bold green]{Markup.Escape(_state.CurrentPlayer.Name)}'s turn[/]")
                .LeftJustified()
        );
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("  [dim]Make sure the other player isn't looking, then press any key when ready...[/]");
        AnsiConsole.WriteLine();

        Console.ReadKey(intercept: true);
        return new GameBoardPage(_controller, _state);
    }
}
