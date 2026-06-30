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
    private (int x, int y) _opponentsBoardsStartCoordinate;

    public GameBoardPage(GameController controller, AttackResponseDto state)
    {
        _controller = controller;
        _state = state;
        _cursor = InitCursor();
    }

    public IPage? Index()
    {
        Render();

        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;

            switch (key)
            {
                case ConsoleKey.W or ConsoleKey.UpArrow:
                case ConsoleKey.S or ConsoleKey.DownArrow:
                case ConsoleKey.A or ConsoleKey.LeftArrow:
                case ConsoleKey.D or ConsoleKey.RightArrow:
                    Console.SetCursorPosition(_opponentsBoardsStartCoordinate.x + (int)_cursor.X * 3, _opponentsBoardsStartCoordinate.y + (int)_cursor.Y);
                    AnsiConsole.Markup("[steelblue1]·[/]");
                    _cursor = MoveCursor(key);
                    Console.SetCursorPosition(_opponentsBoardsStartCoordinate.x + (int)_cursor.X * 3, _opponentsBoardsStartCoordinate.y + (int)_cursor.Y);
                    AnsiConsole.Markup("[bold yellow]▶[/]");
                    break;

                case ConsoleKey.F or ConsoleKey.Enter or ConsoleKey.Spacebar:
                    var result = _controller.Attack(new AttackDto(_cursor));
                    return result.IsGameOver
                        ? new GameOverPage(_controller, result)
                        : new TransitionPage(_controller, result);

                case ConsoleKey.Escape:
                    return new MainMenuPage(_controller);
            }
        }
    }

    private Coordinate InitCursor()
    {
        var board = _state.OpponentBoard;
        int diagonals = board.Size * 2 - 1;

        for (int d = 0; d < diagonals; d++)
        {
            int xStart = Math.Min(d, board.Size - 1);
            int xEnd   = Math.Max(0, d - board.Size + 1);

            for (int x = xStart; x >= xEnd; x--)
            {
                int y = d - x;
                if (board.Cell[x, y].ReceivedAttackResult == null)
                    return new Coordinate((HorizontalLabel)x, (VerticalLabel)y);
            }
        }

        return new Coordinate();
    }

    private Coordinate MoveCursor(ConsoleKey key)
    {
        int destinationX = key is ConsoleKey.A or ConsoleKey.LeftArrow  ? -1 : key is ConsoleKey.D or ConsoleKey.RightArrow ? 1 : 0;
        int destinationY = key is ConsoleKey.W or ConsoleKey.UpArrow    ? -1 : key is ConsoleKey.S or ConsoleKey.DownArrow  ? 1 : 0;

        var board = _state.OpponentBoard;
        int size  = board.Size;
        int x = (int)_cursor.X + destinationX;
        int y = (int)_cursor.Y + destinationY;

        for (int step = 0; step < size; step++, x += destinationX, y += destinationY)
        {
            int wx = ((x % size) + size) % size;
            int wy = ((y % size) + size) % size;

            if (board.Cell[wx, wy].ReceivedAttackResult == null)
                return new Coordinate((HorizontalLabel)wx, (VerticalLabel)wy);
        }

        return _cursor;
    }

    private void Render()
    {
        AnsiConsole.Write(
            new Rule($"[bold yellow]BATTLESHIPS[/]  [dim]|[/]  [cyan]Battle Phase[/]")
                .LeftJustified()
        );
        Console.WriteLine();
        AnsiConsole.MarkupLine(
            $"  [dim]Player:[/] [bold chartreuse1]{Markup.Escape(_state.CurrentPlayer.Name)}[/]"
        );
        AnsiConsole.WriteLine();

        _opponentsBoardsStartCoordinate = (6, Console.CursorTop + 2);
        RenderBoards();

        AnsiConsole.WriteLine();
        AnsiConsole.Write(BuildControls());
    }

    private void RenderBoards()
    {
        var opponentPanel = new Panel(BuildOpponentBoard())
            .Header("[bold] Opponent's Board [/]")
            .BorderColor(Color.Red1);

        var ownPanel = new Panel(BuildOwnBoard())
            .Header("[bold] Your Board [/]")
            .BorderColor(Color.CornflowerBlue);

        var layout = new Table().NoBorder().HideHeaders();
        layout.AddColumn(new TableColumn("").NoWrap());
        layout.AddColumn(new TableColumn("").NoWrap());
        layout.AddRow(opponentPanel, ownPanel);
        AnsiConsole.Write(layout);
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
            AttackResult.Sunk => "[bold red1] ✕[/]",
            AttackResult.Hit  => "[red1] ✕[/]",
            AttackResult.Miss => "[grey46] ○[/]",
            _ => cell.Ship != null ? "[cornflowerblue] ■[/]" : "[steelblue1] ·[/]"
        };
    }

    private string GetOpponentCellMarkup(ICell cell, int x, int y)
    {
        bool isCursor = x == (int)_cursor.X && y == (int)_cursor.Y;

        if (isCursor && cell.ReceivedAttackResult == null)
            return "[bold yellow] ▶[/]";

        return cell.ReceivedAttackResult switch
        {
            AttackResult.Sunk => "[bold red1] ✕[/]",
            AttackResult.Hit  => "[darkorange] ✕[/]",
            AttackResult.Miss => "[grey46] ○[/]",
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
            "[red1]Esc[/]   Exit to Menu"
        );

        return new Panel(grid)
            .Header("[bold] Controls [/]")
            .BorderColor(Color.Grey);
    }
}
