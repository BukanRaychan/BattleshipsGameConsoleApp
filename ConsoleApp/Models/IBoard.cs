namespace ConsoleApp.Models;

public interface IBoard
{
    public ICell[,] Cell {get; set;}
    public int Size {get; set;}

}