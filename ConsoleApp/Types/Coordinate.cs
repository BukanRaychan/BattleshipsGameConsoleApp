namespace ConsoleApp.Types;

public struct Coordinate {
    public HorizontalLabel X {get; set;} 
    public VerticalLabel Y {get; set;}

    public Coordinate(HorizontalLabel x, VerticalLabel y)
    {
        X = x;
        Y = y;
    } 
}