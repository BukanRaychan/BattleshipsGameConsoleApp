using ConsoleApp.Types;

namespace ConsoleApp.Models;

public class Ship : IShip
{
    public ShipType ShipType {get; set;}
    public List<ICell>? Placement {get; set;}

    public Orientation Orientation {get; set;}

    public Ship(ShipType shipType, Orientation orientation)
    {
        Orientation = orientation;
        ShipType = shipType;
    }
}