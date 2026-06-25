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
            $"  [bold green]{Markup.Escape(_state.Winner!.Name)} wins![/]"
        );
        AnsiConsole.WriteLine();

        var winnerPanel = new Panel(BuildBoard(_state.PlayerBoard, revealShips: true))
            .Header($"[bold] {Markup.Escape(_state.Winner.Name)}'s Board [/]")
            .BorderColor(Color.Green);

        var loserPanel = new Panel(BuildBoard(_state.OpponentBoard, revealShips: true))
            .Header("[bold] Opponent's Board [/]")
            .BorderColor(Color.Red);

        AnsiConsole.Write(new Columns(winnerPanel, loserPanel));
        AnsiConsole.WriteLine();
    }

    private static Table BuildBoard(IBoard board, bool revealShips)
    {
        var table = new Table().NoBorder();
        table.AddColumn(new TableColumn("[dim]  [/]"));
        for (int x = 0; x < board.Size; x++)
            table.AddColumn(new TableColumn($"[dim]{x + 1,2}[/]").Centered());

        for (int y = 0; y < board.Size; y++)
        {
            var row = new List<string> { $"[dim]{(VerticalLabel)y} [/]" };
            for (int x = 0; x < board.Size; x++)
                row.Add(GetCellMarkup(board.Cell[x, y], revealShips));
            table.AddRow(row.ToArray());
        }

        return table;
    }

    private static string GetCellMarkup(ICell cell, bool revealShips)
    {
        return cell.ReceivedAttackResult switch
        {
            AttackResult.Sunk => "[bold red] ✕[/]",
            AttackResult.Hit  => "[red] ✕[/]",
            AttackResult.Miss => "[dim] ○[/]",
            _ => revealShips && cell.Ship != null ? "[grey] ■[/]" : "[steelblue1] ·[/]"
        };
    }
}
