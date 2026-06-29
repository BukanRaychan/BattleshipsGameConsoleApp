using ConsoleApp.Controllers;
using ConsoleApp.Types;
using Spectre.Console;

namespace ConsoleApp.UI;


public class GameLayout
{
    private string _eventMsg = "";
    private MessageType _eventMsgType = default;
    private readonly GameController _controller;

    public GameLayout(GameController gameController)
    {
        _controller = gameController;
        _controller.OnMessage += (msg, msgType) => {_eventMsg = msg; _eventMsgType = msgType;};
    }

    public IPage? Render(IPage page)
    {
        AnsiConsole.Clear();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("""
 [bold white]____              __    __    ___                   __                      ____
/\  _`\           /\ \__/\ \__/\_ \                 /\ \      __            /\  _`\
\ \ \L\ \     __  \ \ ,_\ \ ,_\//\ \      __    ____\ \ \___ /\_\  _____    \ \ \L\_\     __      ___ ___      __
 \ \  _ <'  /'__`\ \ \ \/\ \ \/ \ \ \   /'__`\ /',__\\ \  _ `\/\ \/\ '__`\   \ \ \L_L   /'__`\  /' __` __`\  /'__`\
  \ \ \L\ \/\ \L\.\_\ \ \_\ \ \_ \_\ \_/\  __//\__, `\\ \ \ \ \ \ \ \ \L\ \   \ \ \/, \/\ \L\.\_/\ \/\ \/\ \/\  __/
   \ \____/\ \__/.\_\\ \__\\ \__\/\____\ \____\/\____/ \ \_\ \_\ \_\ \ ,__/    \ \____/\ \__/.\_\ \_\ \_\ \_\ \____\
    \/___/  \/__/\/_/ \/__/ \/__/\/____/\/____/\/___/   \/_/\/_/\/_/\ \ \/      \/___/  \/__/\/_/\/_/\/_/\/_/\/____/
                                                                     \ \_\
                                                                      \/_/[/]
""");
        AnsiConsole.WriteLine();
        if (_eventMsg != "")
        {
            var (icon, color) = _eventMsgType switch
            {
                MessageType.Info  => ("ℹ", "dodgerblue2"),
                MessageType.Debug => ("⚙", "gold1"),
                MessageType.Error => ("✗", "red1"),
                _                 => ("·", "grey")
            };
            AnsiConsole.MarkupLine($"[bold {color}]{icon}  {Markup.Escape(_eventMsg)}[/]");
            ClearEventMsg();
        };

        AnsiConsole.WriteLine();

        Console.CursorVisible = false;
        IPage? next = page.Index();
        Console.CursorVisible = true;
        return next;
    }

    private void ClearEventMsg() {_eventMsg = ""; _eventMsgType = default;}
}
