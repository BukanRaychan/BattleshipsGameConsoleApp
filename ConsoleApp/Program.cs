using ConsoleApp.Controllers;
using ConsoleApp.Services;
using ConsoleApp.UI;

IGameService gameService = new GameService();
GameController controller = new GameController(gameService);
App app = new App(controller);
app.Run();
