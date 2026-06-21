using System.ComponentModel;
using ConsoleApp.Controllers;

namespace ConsoleApp.UI;

public class GameLayout
{
    private string _eventMsg = "";
    private readonly GameController _controller;

    public GameLayout(GameController gameController)
    {
        _controller = gameController;
        _controller.OnMessage += (msg) => _eventMsg = msg;
    }

    public IPage? Render(IPage page)
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine(""" 
 ____              __    __    ___                   __                      ____                                   
/\  _`\           /\ \__/\ \__/\_ \                 /\ \      __            /\  _`\                                 
\ \ \L\ \     __  \ \ ,_\ \ ,_\//\ \      __    ____\ \ \___ /\_\  _____    \ \ \L\_\     __      ___ ___      __   
 \ \  _ <'  /'__`\ \ \ \/\ \ \/ \ \ \   /'__`\ /',__\\ \  _ `\/\ \/\ '__`\   \ \ \L_L   /'__`\  /' __` __`\  /'__`\ 
  \ \ \L\ \/\ \L\.\_\ \ \_\ \ \_ \_\ \_/\  __//\__, `\\ \ \ \ \ \ \ \ \L\ \   \ \ \/, \/\ \L\.\_/\ \/\ \/\ \/\  __/ 
   \ \____/\ \__/.\_\\ \__\\ \__\/\____\ \____\/\____/ \ \_\ \_\ \_\ \ ,__/    \ \____/\ \__/.\_\ \_\ \_\ \_\ \____\
    \/___/  \/__/\/_/ \/__/ \/__/\/____/\/____/\/___/   \/_/\/_/\/_/\ \ \/      \/___/  \/__/\/_/\/_/\/_/\/_/\/____/
                                                                     \ \_\                                          
                                                                      \/_/                                          
""");

        Console.WriteLine();
        Console.ResetColor();

        if (_eventMsg != "")
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine();
            Console.WriteLine(_eventMsg);
            ClearEventMsg();
        }
        

        IPage? next = page.Index();
        return next;
    }

    private void ClearEventMsg() => _eventMsg = "";
}
