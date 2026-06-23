using ConsoleApp.Types;

namespace ConsoleApp.Models;

public interface IShip
{
    public ShipType ShipType {get; set;}
    public List<ICell>? Placement {get; set;}
    public Orientation Orientation {get; set;}
}