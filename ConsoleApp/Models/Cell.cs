using ConsoleApp.Types;

namespace ConsoleApp.Models;


public class Cell : ICell
{
    public IShip? Ship {get; set;}

    public Coordinate Coordinate{get; set;}

    public AttackResult? ReceivedAttackResult {get; set;}

    public Cell(Coordinate coordinate)
    {
        Coordinate = coordinate;
    }
}
