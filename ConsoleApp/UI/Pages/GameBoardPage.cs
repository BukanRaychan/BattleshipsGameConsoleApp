using ConsoleApp.Controllers;
using ConsoleApp.DTOs;
using ConsoleApp.Models;
using ConsoleApp.Types;
using Spectre.Console;

namespace ConsoleApp.UI.Pages;

public class GameBoardPage : IPage
{
    private readonly GameController _controller;
    private readonly AttackResponseDto _state;
    private Coordinate _cursor;

    public GameBoardPage(GameController controller, AttackResponseDto state, Coordinate cursor = new())
    {
        _controller = controller;
        _state = state;
        _cursor = cursor;
    }

    public IPage? Index()
    {
        Render();

        var key = Console.ReadKey(intercept: true).Key;

        int x = (int)_cursor.X;
        int y = (int)_cursor.Y;
        int size = _state.OpponentBoard.Size;

        switch (key)
        {
            case ConsoleKey.W or ConsoleKey.UpArrow:
                _cursor = y > 0 ? new Coordinate(_cursor.X, (VerticalLabel)(y - 1)) : _cursor;
                return new GameBoardPage(_controller, _state, _cursor);

            case ConsoleKey.S or ConsoleKey.DownArrow:
                _cursor = y < size - 1 ? new Coordinate(_cursor.X, (VerticalLabel)(y + 1)) : _cursor;
                return new GameBoardPage(_controller, _state, _cursor);

            case ConsoleKey.A or ConsoleKey.LeftArrow:
                _cursor = x > 0 ? new Coordinate((HorizontalLabel)(x - 1), _cursor.Y) : _cursor;
                return new GameBoardPage(_controller, _state, _cursor);

            case ConsoleKey.D or ConsoleKey.RightArrow:
                _cursor = x < size - 1 ? new Coordinate((HorizontalLabel)(x + 1), _cursor.Y) : _cursor;
                return new GameBoardPage(_controller, _state, _cursor);

            case ConsoleKey.F or ConsoleKey.Enter or ConsoleKey.Spacebar:
                var result = _controller.MakeAttack(new AttackDto(_cursor));
                return result.IsGameOver
                    ? new GameOverPage(_controller, result)
                    : new TransitionPage(_controller, result);

            case ConsoleKey.Escape:
                return new MainMenuPage(_controller);

            default:
                return new GameBoardPage(_controller, _state, _cursor);
        }
    }

    private void Render()
    {
        AnsiConsole.Write(
            new Rule($"[bold yellow]BATTLESHIPS[/]  [dim]|[/]  [cyan]Battle Phase[/]")
                .LeftJustified()
        );
        AnsiConsole.MarkupLine(
            $"  [dim]Player:[/] [bold green]{Markup.Escape(_state.CurrentPlayer.Name)}[/]"
        );
        AnsiConsole.WriteLine();

        var opponentPanel = new Panel(BuildOpponentBoard())
            .Header("[bold] Opponent [/]")
            .BorderColor(Color.Red);

        var ownPanel = new Panel(BuildOwnBoard())
            .Header("[bold] Your Board [/]")
            .BorderColor(Color.Blue);

        var layout = new Table().NoBorder().HideHeaders();
        layout.AddColumn(new TableColumn("").NoWrap());
        layout.AddColumn(new TableColumn("").NoWrap());
        layout.AddRow(opponentPanel, ownPanel);
        AnsiConsole.Write(layout);
        AnsiConsole.WriteLine();
        AnsiConsole.Write(BuildControls());
    }

    private Table BuildOwnBoard()
    {
        var board = _state.PlayerBoard;
        var table = new Table().NoBorder();
        table.AddColumn(new TableColumn("[dim]  [/]"));
        for (int x = 0; x < board.Size; x++)
            table.AddColumn(new TableColumn($"[dim]{x + 1,2}[/]").Centered());

        for (int y = 0; y < board.Size; y++)
        {
            var row = new List<string> { $"[dim]{(VerticalLabel)y} [/]" };
            for (int x = 0; x < board.Size; x++)
                row.Add(GetOwnCellMarkup(board.Cell[x, y]));
            table.AddRow(row.ToArray());
        }

        return table;
    }

    private Table BuildOpponentBoard()
    {
        var board = _state.OpponentBoard;
        var table = new Table().NoBorder();
        table.AddColumn(new TableColumn("[dim]  [/]"));
        for (int x = 0; x < board.Size; x++)
            table.AddColumn(new TableColumn($"[dim]{x + 1,2}[/]").Centered());

        for (int y = 0; y < board.Size; y++)
        {
            var row = new List<string> { $"[dim]{(VerticalLabel)y} [/]" };
            for (int x = 0; x < board.Size; x++)
                row.Add(GetOpponentCellMarkup(board.Cell[x, y], x, y));
            table.AddRow(row.ToArray());
        }

        return table;
    }

    private static string GetOwnCellMarkup(ICell cell)
    {
        return cell.ReceivedAttackResult switch
        {
            AttackResult.Sunk => "[bold red] ✕[/]",
            AttackResult.Hit  => "[red] ✕[/]",
            AttackResult.Miss => "[dim] ○[/]",
            _ => cell.Ship != null ? "[grey] ■[/]" : "[steelblue1] ·[/]"
        };
    }

    private string GetOpponentCellMarkup(ICell cell, int x, int y)
    {
        bool isCursor = x == (int)_cursor.X && y == (int)_cursor.Y;

        if (isCursor && cell.ReceivedAttackResult == null)
            return "[bold yellow] ▶[/]";

        return cell.ReceivedAttackResult switch
        {
            AttackResult.Sunk => "[bold red] ✕[/]",
            AttackResult.Hit  => "[red] ✕[/]",
            AttackResult.Miss => "[dim] ○[/]",
            _ => isCursor ? "[bold yellow] ▶[/]" : "[steelblue1] ·[/]"
        };
    }

    private static Panel BuildControls()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            "[bold yellow]W/↑[/] Move Up      ",
            "[bold yellow]A/←[/] Move Left    ",
            "[bold yellow]S/↓[/] Move Down    "
        );
        grid.AddRow(
            "[bold yellow]D/→[/] Move Right   ",
            "[bold yellow]F/Enter[/] Fire      ",
            "[red]Esc[/]   Exit to Menu"
        );

        return new Panel(grid)
            .Header("[bold] Controls [/]")
            .BorderColor(Color.Grey);
    }
}
