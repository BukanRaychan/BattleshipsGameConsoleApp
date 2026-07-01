using ConsoleApp.Controllers;
using ConsoleApp.Models;
using ConsoleApp.Services;
using ConsoleApp.Types;
using ConsoleApp.UI;

IGameService gameService = new GameService(
    name => new Player(name),
    (size, cells) => new Board(size, cells),
    (shipType, orientation) => new Ship(shipType, orientation),
    coord => new Cell(coord)
);
GameController controller = new GameController(gameService);
App app = new App(controller);
app.Run();
