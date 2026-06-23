using ConsoleApp.Types;

namespace ConsoleApp.Models;

public class Board : IBoard
{
    public ICell[,] Cell { get; set; }
    public int Size { get; set; }

    public Board(int size)
    {
        Size = size;
        Cell = GenerateCells();
    }

    

    private ICell[,] GenerateCells()
    {
        ICell[,] cells = new ICell[Size, Size];

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                var coordinate = new Coordinate((HorizontalLabel)x, (VerticalLabel)y);
                cells[x, y] = new Cell(coordinate);
            }
        }

        return cells;
    }
}
