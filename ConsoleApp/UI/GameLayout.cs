namespace ConsoleApp.UI;

public class GameLayout()
{
    public void Index(IPage page)
    {
        Console.WriteLine("==== BattleShip Game ====");

        page.Index();

        Console.WriteLine("========================");
    }
}
