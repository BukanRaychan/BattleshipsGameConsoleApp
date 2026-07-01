using ConsoleApp.Controllers;
using ConsoleApp.DTOs;
using ConsoleApp.Models;
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

        if (_state.LastAttack.HasValue && _state.LastResult.HasValue)
        {
            (string icon, string color) = _state.LastResult.Value switch
            {
                AttackResult.Sunk => ("💥", "red1"),
                AttackResult.Hit  => ("🎯", "darkorange"),
                _                 => ("💨", "dodgerblue1"),
            };

            Coordinate coord = _state.LastAttack.Value;
            string col = ((int)coord.X + 1).ToString();
            string row = coord.Y.ToString();

            AnsiConsole.MarkupLine(
                $"  {icon} [bold]Last attack:[/] [{color}]" +
                $"{Markup.Escape(_state.LastResult.Value.ToString())}[/]" +
                $" at [bold]{row}{col}[/]"
            );

            AnsiConsole.WriteLine();

            AnsiConsole.Write(
            new Panel(BuildAttackedBoard())
                .Header("[bold] Attack Board [/]")
                .BorderColor(Color.Orange1)
            );

            AnsiConsole.WriteLine();
        }

        AnsiConsole.MarkupLine($"[dim]→[/]  [bold chartreuse1]{Markup.Escape(_state.CurrentPlayer.Name)}'s turn[/]");
        AnsiConsole.MarkupLine($"   [dim]Make sure {Markup.Escape(_state.CurrentOpponent.Name)} isn't looking, then press any key when ready...[/]");
        AnsiConsole.WriteLine();

        Console.ReadKey(intercept: true);
        IPage? nextPage = new GameBoardPage(_controller, _state);
        return nextPage;
    }

    private Table BuildAttackedBoard()
    {
        IBoard board = _state.PlayerBoard;
        Table table = new Table().NoBorder();
        table.AddColumn(new TableColumn("[dim]  [/]"){ Padding = new Padding(0, 0, 0, 1) });
        for (int x = 0; x < board.Size; x++)
        {
            table.AddColumn(new TableColumn($"[dim]{x + 1,2} [/]"){ Padding = new Padding(0, 0, 0, 1) }.Centered());
        }

        for (int y = 0; y < board.Size; y++)
        {
            List<string> row = new List<string> { $"[dim]{(VerticalLabel)y} [/]" };
            for (int x = 0; x < board.Size; x++)
            {
                row.Add(GetCellMarkup(board.Cell[x, y]));
            }
            table.AddRow(row.ToArray());
        }

        return table;
    }

    private string GetCellMarkup(ICell cell)
    {
        return cell.ReceivedAttackResult switch
        {
            AttackResult.Sunk => "[bold red1] ✕[/]",
            AttackResult.Hit  => "[darkorange] ✕[/]",
            AttackResult.Miss => "[grey46] ○[/]",
            _ => "[steelblue1] ·[/]"
        };
    }
}
