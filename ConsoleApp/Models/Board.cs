using ConsoleApp.Types;

namespace ConsoleApp.Models;

public class Board : IBoard
{
    public ICell[,] Cell { get; set; }
    public int Size { get; set; }

    public Board(int size, ICell[,] cells)
    {
        Size = size;
        Cell = cells;
    }
}
