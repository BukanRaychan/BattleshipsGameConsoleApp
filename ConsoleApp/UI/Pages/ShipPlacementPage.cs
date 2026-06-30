using ConsoleApp.Controllers;
using ConsoleApp.DTOs;
using ConsoleApp.Models;
using ConsoleApp.Types;
using Spectre.Console;

namespace ConsoleApp.UI.Pages;

public class ShipPlacementPage : IPage
{
    private readonly GameController _controller;
    private ShipPlacementResponseDto _state;
    private int _sectionRow;

    public ShipPlacementPage(GameController controller, ShipPlacementResponseDto state)
    {
        _controller = controller;
        _state = state;
    }

    public IPage? Index()
    {
        Render();

        while (true)
        {
            var key = Console.ReadKey(intercept: true).Key;

            if (key == ConsoleKey.Escape)
                return new MainMenuPage(_controller);

            _state = _controller.PlaceShip(
                new EditShipPlacementDto(_state.SelectedShip as Ship, key, _state.IndexPlayerCursor)
            );

            if (_state.IsPlacementPhaseFinished)
            {
                var attackState = _controller.StartAttackPhase();
                return new TransitionPage(_controller, attackState);
            }

            Console.SetCursorPosition(0, _sectionRow);
            RenderSection();
        }
    }

    private void Render()
    {
        AnsiConsole.Write(
            new Rule($"[bold yellow]BATTLESHIPS[/]  [dim]|[/]  [cyan]Ship Placement[/]")
                .LeftJustified()
        );
        Console.WriteLine();

        _sectionRow = Console.CursorTop;
        RenderSection();

        AnsiConsole.WriteLine();
        AnsiConsole.Write(BuildControls());
    }

    private void RenderSection()
    {
        AnsiConsole.MarkupLine(
            $"  [dim]Player:[/] [bold chartreuse1]{Markup.Escape(_state.CurrentPlayer.Name)}[/]  " +
            $"[dim]|[/]  " +
            (_state.IsValidPlacement
                ? "[chartreuse1]✓ Valid placement  [/]"
                : "[red1]✗ Invalid placement[/]")
        );
        AnsiConsole.WriteLine();

        var boardPanel = new Panel(BuildBoard())
            .Header("[bold] Board [/]")
            .BorderColor(Color.CornflowerBlue);

        var shipPanel = new Panel(BuildShipList())
            .Header("[bold] Ships [/]")
            .BorderColor(Color.CornflowerBlue);

        var layout = new Table().NoBorder().HideHeaders();
        layout.AddColumn(new TableColumn("").NoWrap());
        layout.AddColumn(new TableColumn("").NoWrap());
        layout.AddRow(boardPanel, shipPanel);
        AnsiConsole.Write(layout);
    }

    private Table BuildBoard()
    {
        IBoard board = _state.Board;

        HashSet<(int x, int y)> surroundingZone = [];
        HashSet<(int x, int y)> selectedSet = [.. _state.SelectedShip!.Placement!.Select(c => ((int)c.Coordinate.X, (int)c.Coordinate.Y))];

        
        if (selectedSet.Count > 0)
        {
            int minX = Math.Max(0, selectedSet.Min(c => c.x) - 1);
            int maxX = Math.Min(board.Size - 1, selectedSet.Max(c => c.x) + 1);
            int minY = Math.Max(0, selectedSet.Min(c => c.y) - 1);
            int maxY = Math.Min(board.Size - 1, selectedSet.Max(c => c.y) + 1);

            for (int sx = minX; sx <= maxX; sx++)
            for (int sy = minY; sy <= maxY; sy++)
            {
                if (!selectedSet.Contains((sx, sy))){
                    surroundingZone.Add((sx, sy));
                }
            }
        }

        var table = new Table().NoBorder();
        table.AddColumn(new TableColumn("[dim]  [/]"));
        for (int x = 0; x < board.Size; x++)
            table.AddColumn(new TableColumn($"[dim]{x + 1,2}[/]").Centered());

        for (int y = 0; y < board.Size; y++)
        {
            var row = new List<string> { $"[dim]{(VerticalLabel)y} [/]" };

            for (int x = 0; x < board.Size; x++)
                row.Add(GetCellMarkup(board.Cell[x, y], x, y, selectedSet, surroundingZone));

            table.AddRow(row.ToArray());
        }

        return table;
    }

    private string GetCellMarkup(
        ICell cell, int x, int y,
        HashSet<(int, int)> selectedSet,
        HashSet<(int, int)> surroundingZone)
    {
        bool isSelected = selectedSet.Contains((x, y));
        bool isSurrounding = surroundingZone.Contains((x, y));

        if (isSelected)
        {
            string color = _state.IsValidPlacement ? "chartreuse1" : "red1";
            return  $"[{color}] ■[/]";
        }

        if (isSurrounding)
            return cell.Ship != null ? "[bold red1] ■[/]" : "[dim green] ·[/]";

        if (cell.Ship != null)
            return "[cornflowerblue] ■[/]";

        return "[steelblue1] ·[/]";
    }

    private Table BuildShipList()
    {
        var table = new Table().NoBorder();
        table.AddColumn(new TableColumn(""));

        foreach (var ship in _state.Ships)
        {
            bool isSelected = ship == _state.SelectedShip;
            int length = (int)ship.ShipType;
            string name = ship.ShipType.ToString();
            string orient = ship.Orientation == Orientation.Vertical ? "↕ Vertical  " : "↔ Horizontal";
            string filled = new('■', length);
            string empty  = new('·', 5 - length);

            if (isSelected)
            {
                string color = _state.IsValidPlacement ? "chartreuse1" : "red1";
                table.AddRow(
                    $"[bold yellow] ▶ {name}[/] [dim]({length})[/]\n" +
                    $"   [{color}]{filled}[/][dim grey42]{empty}[/]  [dim]{orient}[/]"
                );
            }
            else
            {
                table.AddRow(
                    $"[dim]   {name} ({length})[/]\n" +
                    $"   [dim cornflowerblue]{filled}[/][dim grey42]{empty}[/]"
                );
            }
        }

        return table;
    }

    private static Panel BuildControls()
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(
            "[bold yellow]W/↑[/] Move Up      ",
            "[bold yellow]A/←[/] Move Left    ",
            "[bold yellow]S/↓[/] Move Down    ",
            "[bold yellow]D/→[/]  Move Right   "
        );
        grid.AddRow(
            "[bold yellow]Q  [/] Prev Ship    ",
            "[bold yellow]E  [/] Next Ship    ",
            "[bold yellow]R  [/] Rotate       ",
            "[bold yellow]C  [/]  Confirm     "
        );
        grid.AddRow(
            "[bold yellow]Z  [/] Undo",
            "[bold yellow]X  [/] Redo",
            "[red1]Esc[/]  Exit to Menu "
        );

        return new Panel(grid)
            .Header("[bold] Controls [/]")
            .BorderColor(Color.Grey);
    }
}
