using ConsoleApp.Controllers;
using ConsoleApp.DTOs;
using ConsoleApp.Models;
using ConsoleApp.Types;
using Spectre.Console;

namespace ConsoleApp.UI.Pages;

public class GameOverPage : IPage
{
    private readonly GameController _controller;
    private readonly AttackResponseDto _state;

    public GameOverPage(GameController controller, AttackResponseDto state)
    {
        _controller = controller;
        _state = state;
    }

    public IPage? Index()
    {
        Render();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .AddChoices("Play Again", "Quit")
        );

        return choice == "Play Again" ? new MainMenuPage(_controller) : null;
    }

    private void Render()
    {
        AnsiConsole.Write(
            new Rule($"[bold yellow]BATTLESHIPS[/]  [dim]|[/]  [bold red]Game Over[/]")
                .LeftJustified()
        );
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"  [bold chartreuse1]{Markup.Escape(_state.CurrentPlayer.Name)} wins![/]"
        );
        AnsiConsole.WriteLine();

        var winnerPanel = new Panel(BuildBoard(_state.PlayerBoard))
            .Header($"[bold] {Markup.Escape(_state.CurrentPlayer.Name)}'s Board [/]")
            .BorderColor(Color.Chartreuse1);

        var loserPanel = new Panel(BuildBoard(_state.OpponentBoard))
            .Header($"[bold] {Markup.Escape(_state.CurrentOpponent.Name)}'s Board [/]")
            .BorderColor(Color.Red1);

        var layout = new Table().NoBorder().HideHeaders();
        layout.AddColumn(new TableColumn("").NoWrap());
        layout.AddColumn(new TableColumn("").NoWrap());
        layout.AddRow(winnerPanel, loserPanel);
        AnsiConsole.Write(layout);

        AnsiConsole.WriteLine();
    }

    private static Table BuildBoard(IBoard board)
    {
        var table = new Table().NoBorder();
        table.AddColumn(new TableColumn("[dim]  [/]"));
        for (int x = 0; x < board.Size; x++)
            table.AddColumn(new TableColumn($"[dim]{x + 1,2}[/]").Centered());

        for (int y = 0; y < board.Size; y++)
        {
            var row = new List<string> { $"[dim]{(VerticalLabel)y} [/]" };
            for (int x = 0; x < board.Size; x++)
                row.Add(GetCellMarkup(board.Cell[x, y]));
            table.AddRow(row.ToArray());
        }

        return table;
    }

    private static string GetCellMarkup(ICell cell)
    {
        return cell.ReceivedAttackResult switch
        {
            AttackResult.Sunk => "[bold red] ✕[/]",
            AttackResult.Hit  => "[red] ✕[/]",
            AttackResult.Miss => "[dim] ○[/]",
            _ => cell.Ship != null ? "[grey] ■[/]" : "[steelblue1] ·[/]"
        };
    }
}
