using ConsoleApp.DTOs;
using ConsoleApp.Models;
using ConsoleApp.Types;

namespace ConsoleApp.Services;

public class GameService : IGameService
{
    /// <summary>Ordered list of players in the game.</summary>
    private readonly List<IPlayer> _players;
    /// <summary>Maps each player to their own board.</summary>
    private readonly Dictionary<IPlayer, IBoard> _playerBoard;
    /// <summary>Maps each player to their fleet of ships.</summary>
    private readonly Dictionary<IPlayer, List<IShip>> _playerShips;
    /// <summary>Surfaces game messages to the UI layer.</summary>
    public event MessageDelegate? OnMessage;
    /// <summary>The player whose turn it currently is.</summary>
    public IPlayer? CurrentPlayer { get; set; }
    /// <summary>Index into <see cref="_players"/> for the current player.</summary>
    private int _indexCurrentPlayer = 0;
    /// <summary>The ship currently being moved or rotated during placement.</summary>
    private IShip? _selectedShip;
    /// <summary>Board coordinate of the anchor point for the selected ship.</summary>
    private Coordinate _indexPlayerCursor;
    /// <summary>Placement states available to undo, capped at <see cref="MaxUndoSteps"/>.</summary>
    private readonly List<PlacementState> _undoStack = [];
    /// <summary>Placement states available to redo after an undo.</summary>
    private readonly List<PlacementState> _redoStack = [];
    /// <summary>Maximum number of undo steps retained.</summary>
    private const int MaxUndoSteps = 5;
    /// <summary>Undo/Redo list of element state</summary>
    private record PlacementState(
        List<(IShip Ship, List<Coordinate> Coords, Orientation Orientation)> ShipStates,
        int SelectedShipIndex,
        Coordinate Cursor
    );

    /// <summary>Initializes empty player, board, and ship collections.</summary>
    public GameService()
    {
        _players = new List<IPlayer>();
        _playerBoard = new Dictionary<IPlayer, IBoard>();
        _playerShips = new Dictionary<IPlayer, List<IShip>>();
        CurrentPlayer = null;
    }

    /// <summary>Resets all state, sets up players, boards, and default ship positions; lifts the first ship for placement.</summary>
    public ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto startPlacementPhaseDto)
    {
        _players.Clear();
        _playerBoard.Clear();
        _playerShips.Clear();
        CurrentPlayer = null;
        _indexCurrentPlayer = 0;
        _selectedShip = null;
        _undoStack.Clear();
        _redoStack.Clear();

        _players.AddRange([new Player(startPlacementPhaseDto.PlayerOneName), new Player(startPlacementPhaseDto.PlayerTwoName)]);

        foreach (IPlayer player in _players)
        {
            Board board = new Board(startPlacementPhaseDto.BoardSize, GenerateCells(startPlacementPhaseDto.BoardSize));
            _playerBoard[player] = board;
            _playerShips[player] = PlaceShipsDefault(board);
        }

        CurrentPlayer = _players[_indexCurrentPlayer];
        IBoard currentBoard = _playerBoard[CurrentPlayer];
        List<IShip> currentShips = _playerShips[CurrentPlayer];
        _selectedShip = currentShips.First();
        _indexPlayerCursor = GetAnchorCoordinate(_selectedShip);

        // Lift the first ship off the board so cell.Ship only tracks non-selected ships
        LiftShip(_selectedShip);

        ShipPlacementResponseDto response = new ShipPlacementResponseDto(
            currentBoard,
            currentShips,
            CurrentPlayer,
            _selectedShip,
            _indexPlayerCursor,
            true
        );
        return response;
    }

    /// <summary>Dispatches a key event to the appropriate placement handler and returns the updated state.</summary>
    public ShipPlacementResponseDto PlaceShip(EditShipPlacementDto editShipPlacementDto)
    {
        List<IShip> ships = _playerShips[CurrentPlayer!];
        IBoard board = _playerBoard[CurrentPlayer!];
        _selectedShip = editShipPlacementDto.CurrentShip;
        _indexPlayerCursor = editShipPlacementDto.IndexPlayerCursor;

        OnMessage?.Invoke(); //Clear Message
        ShipPlacementResponseDto response = editShipPlacementDto.KeyEvent switch
        {
            ConsoleKey.Q                                  => HandlePrevShip(ships, board),
            ConsoleKey.W or ConsoleKey.UpArrow            => HandleShipPlacementMove(0, -1, board),
            ConsoleKey.E                                  => HandleNextShip(ships, board),
            ConsoleKey.R                                  => HandleRotate(board),
            ConsoleKey.A or ConsoleKey.LeftArrow          => HandleShipPlacementMove(-1, 0, board),
            ConsoleKey.S or ConsoleKey.DownArrow          => HandleShipPlacementMove(0, 1, board),
            ConsoleKey.D or ConsoleKey.RightArrow         => HandleShipPlacementMove(1, 0, board),
            ConsoleKey.Z                                  => HandleUndo(board),
            ConsoleKey.X                                  => HandleRedo(board),
            ConsoleKey.C                                  => HandleConfirm(ships, board),
            _                                             => BuildResponse(board, ships)
        };
        return response;
    }

    /// <summary>Resets to player 0 and returns the initial attack-phase state with no prior attack.</summary>
    public AttackResponseDto StartAttackPhase()
    {
        _indexCurrentPlayer = 0;
        CurrentPlayer = _players[0];
        IPlayer opponent = _players[1];

        AttackResponseDto response = new AttackResponseDto(
            _playerBoard[CurrentPlayer],
            _playerBoard[opponent],
            CurrentPlayer,
            opponent,
            null, null, false
        );
        return response;
    }

    /// <summary>Resolves an attack on the opponent's board, switches turns, and returns the updated state.</summary>
    public AttackResponseDto Attack(AttackDto attackDto)
    {
        IPlayer attacker = CurrentPlayer!;
        IPlayer opponent = GetOpponent(attacker);
        IBoard attackerBoard = _playerBoard[attacker];
        IBoard opponentBoard = _playerBoard[opponent];

        if (!ValidateAttack(opponentBoard, attackDto.Target))
        {
            AttackResult? existingResult = opponentBoard.Cell[(int)attackDto.Target.X, (int)attackDto.Target.Y].ReceivedAttackResult;
            AttackResponseDto invalidResponse = new AttackResponseDto(attackerBoard, opponentBoard, attacker, opponent, attackDto.Target, existingResult, false);
            return invalidResponse;
        }

        AttackResult result = ReceiveAttack(opponentBoard, attackDto.Target);

        if (IsAllShipsOnBoardSunk(_playerShips[opponent]))
        {
            AttackResponseDto gameOverResponse = new AttackResponseDto(attackerBoard, opponentBoard, attacker, opponent, attackDto.Target, result, true);
            return gameOverResponse;
        }

        SwitchTurn();
        IPlayer newOpponent = GetOpponent(CurrentPlayer!);
        AttackResponseDto response = new AttackResponseDto(_playerBoard[CurrentPlayer!], _playerBoard[newOpponent], CurrentPlayer!, newOpponent, attackDto.Target, result, false);
        return response;
    }


    /*
    ======================================== HELPER FUNCTION ========================================
    */

    /// <summary>Advances <see cref="CurrentPlayer"/> and <see cref="_indexCurrentPlayer"/> to the opponent.</summary>
    private void SwitchTurn()
    {
        CurrentPlayer = GetOpponent(CurrentPlayer!);
        _indexCurrentPlayer = _players.IndexOf(CurrentPlayer);
    }

    /// <summary>Applies an attack to the cell at the given coordinate and returns the result.</summary>
    private AttackResult ReceiveAttack(IBoard receiverBoard, Coordinate coordinate)
    {
        ICell cell = receiverBoard.Cell[(int)coordinate.X, (int)coordinate.Y];
        IShip? ship = GetShipAtCoordinate(receiverBoard, coordinate);
        if (ship == null)
        {
            cell.ReceivedAttackResult = AttackResult.Miss;
            return AttackResult.Miss;
        }
        cell.ReceivedAttackResult = AttackResult.Hit;
        RecordShipHit(ship);
        AttackResult attackResult = cell.ReceivedAttackResult == AttackResult.Sunk ? AttackResult.Sunk : AttackResult.Hit;
        return attackResult;
    }

    /// <summary>Returns true if all of the player's ships are sunk.</summary>
    private static bool IsAllShipsOnBoardSunk(List<IShip> ships) => ships.All(s => s.Placement!.All(c => c.ReceivedAttackResult != null));

    /// <summary>Returns the player in the game who is not the given player.</summary>
    private IPlayer GetOpponent(IPlayer player) => _players.First(p => p != player);

    /// <summary>Constructs a <see cref="Coordinate"/> from the given horizontal and vertical labels.</summary>
    private static Coordinate GetCoordinate(HorizontalLabel horizontalLabel, VerticalLabel verticalLabel) =>
        new(horizontalLabel, verticalLabel);

    /// <summary>Returns the ship occupying the given coordinate, or null if the cell is empty.</summary>
    private static IShip? GetShipAtCoordinate(IBoard board, Coordinate coordinate) =>
        board.Cell[(int)coordinate.X, (int)coordinate.Y].Ship;

    /// <summary>Returns true if the target cell has not already been attacked.</summary>
    private static bool ValidateAttack(IBoard board, Coordinate coordinate) =>
        board.Cell[(int)coordinate.X, (int)coordinate.Y].ReceivedAttackResult == null;

    /// <summary>Marks all cells of the ship as Sunk if every cell has been hit.</summary>
    private static void RecordShipHit(IShip ship)
    {
        List<ICell> placement = ship.Placement!;
        if (placement.All(c => c.ReceivedAttackResult != null))
        {
            foreach (ICell cell in placement)
            {
                cell.ReceivedAttackResult = AttackResult.Sunk;
            }
        }
    }

    /// <summary>Moves the selected ship by (destinationX, destinationY); rejects the move if it goes out of bounds.</summary>
    private ShipPlacementResponseDto HandleShipPlacementMove(int destinationX, int destinationY, IBoard board)
    {
        List<IShip> ships = _playerShips[CurrentPlayer!];
        int newX = (int)_indexPlayerCursor.X + destinationX;
        int newY = (int)_indexPlayerCursor.Y + destinationY;

        if (newX < 0 || newX >= board.Size || newY < 0 || newY >= board.Size)
        {
            ShipPlacementResponseDto outOfBoundsResponse = BuildResponse(board, ships);
            return outOfBoundsResponse;
        }

        Coordinate newCursor = GetCoordinate((HorizontalLabel)newX, (VerticalLabel)newY);
        List<Coordinate> newCoords = GetShipCoordinatesWithOrientation(_selectedShip!, newCursor, _selectedShip!.Orientation);

        if (!newCoords.All(c => IsInBounds(c, board.Size)))
        {
            ShipPlacementResponseDto invalidResponse = BuildResponse(board, ships);
            return invalidResponse;
        }

        PushUndo(ships);

        // Only update the ship's intended placement — never touch cell.Ship for the lifted ship
        UpdateShipPlacement(_selectedShip!, newCoords, board);
        _indexPlayerCursor = newCursor;
        _redoStack.Clear();

        ShipPlacementResponseDto response = BuildResponse(board, ships);
        return response;
    }

    /// <summary>Toggles orientation and shifts the cursor so the rotated ship always fits within board bounds.</summary>
    private ShipPlacementResponseDto HandleRotate(IBoard board)
    {
        List<IShip> ships = _playerShips[CurrentPlayer!];
        Orientation newOrientation = _selectedShip!.Orientation == Orientation.Vertical
            ? Orientation.Horizontal
            : Orientation.Vertical;

        List<Coordinate> newCoords = GetShipCoordinatesWithOrientation(_selectedShip, _indexPlayerCursor, newOrientation);

        int cursorX = (int)_indexPlayerCursor.X;
        int cursorY = (int)_indexPlayerCursor.Y;

        int minX = newCoords.Min(c => (int)c.X);
        int maxX = newCoords.Max(c => (int)c.X);
        int minY = newCoords.Min(c => (int)c.Y);
        int maxY = newCoords.Max(c => (int)c.Y);

        if (minX < 0) { cursorX -= minX; }
        if (maxX >= board.Size) { cursorX -= maxX - (board.Size - 1); }
        if (minY < 0) { cursorY -= minY; }
        if (maxY >= board.Size) { cursorY -= maxY - (board.Size - 1); }

        _indexPlayerCursor = GetCoordinate((HorizontalLabel)cursorX, (VerticalLabel)cursorY);
        newCoords = GetShipCoordinatesWithOrientation(_selectedShip, _indexPlayerCursor, newOrientation);

        PushUndo(ships);
        _selectedShip.Orientation = newOrientation;
        UpdateShipPlacement(_selectedShip, newCoords, board);
        _redoStack.Clear();

        ShipPlacementResponseDto response = BuildResponse(board, ships);
        return response;
    }

    /// <summary>Lands the current ship and selects the next one in the list, wrapping to the first.</summary>
    private ShipPlacementResponseDto HandleNextShip(List<IShip> ships, IBoard board)
    {
        if (!IsCurrentShipValid(board))
        {
            OnMessage?.Invoke("Invalid placement — the ship must not overlap with or be within 1 cell of another ship. Cannot proceed to select the next ship.", MessageType.Error);
            ShipPlacementResponseDto invalidResponse = BuildResponse(board, ships);
            return invalidResponse;
        } 

        LandShip(_selectedShip!);

        _selectedShip = ships.SkipWhile(s => s != _selectedShip).Skip(1).FirstOrDefault()
            ?? ships.First();
        _indexPlayerCursor = GetAnchorCoordinate(_selectedShip);

        LiftShip(_selectedShip);

        ShipPlacementResponseDto response = BuildResponse(board, ships);
        return response;
    }

    /// <summary>Lands the current ship and selects the previous one in the list, wrapping to the last.</summary>
    private ShipPlacementResponseDto HandlePrevShip(List<IShip> ships, IBoard board)
    {
        if (!IsCurrentShipValid(board))
        {
            OnMessage?.Invoke("Invalid placement — the ship must not overlap with or be within 1 cell of another ship. Cannot proceed to select the previous ship.", MessageType.Error);
            ShipPlacementResponseDto invalidResponse = BuildResponse(board, ships);
            return invalidResponse;
        }

        LandShip(_selectedShip!);

        _selectedShip = ships.AsEnumerable().Reverse().SkipWhile(s => s != _selectedShip).Skip(1).FirstOrDefault()
            ?? ships.Last();
        _indexPlayerCursor = GetAnchorCoordinate(_selectedShip);

        LiftShip(_selectedShip);

        ShipPlacementResponseDto response = BuildResponse(board, ships);
        return response;
    }

    /// <summary>Restores the last saved placement state from the undo stack.</summary>
    private ShipPlacementResponseDto HandleUndo(IBoard board)
    {
        List<IShip> ships = _playerShips[CurrentPlayer!];
        if (_undoStack.Count == 0)
        {
            ShipPlacementResponseDto emptyResponse = BuildResponse(board, ships);
            return emptyResponse;
        }

        _redoStack.Add(CreateSnapshot(ships));
        PlacementState state = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        ApplySnapshot(state, board, ships);

        ShipPlacementResponseDto response = BuildResponse(board, ships);
        return response;
    }

    /// <summary>Re-applies the last undone placement state from the redo stack.</summary>
    private ShipPlacementResponseDto HandleRedo(IBoard board)
    {
        List<IShip> ships = _playerShips[CurrentPlayer!];
        if (_redoStack.Count == 0)
        {
            ShipPlacementResponseDto emptyResponse = BuildResponse(board, ships);
            return emptyResponse;
        }

        PushUndo(ships);
        PlacementState state = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        ApplySnapshot(state, board, ships);

        ShipPlacementResponseDto response = BuildResponse(board, ships);
        return response;
    }

    /// <summary>Validates all ship placements; if valid, advances to the next player or ends the placement phase.</summary>
    private ShipPlacementResponseDto HandleConfirm(List<IShip> ships, IBoard board)
    {
        if (!IsCurrentShipValid(board))
        {
            OnMessage?.Invoke("Invalid placement — the ship must not overlap with or be within 1 cell of another ship. Cannot proceed to confirmation step.", MessageType.Error);
            ShipPlacementResponseDto invalidResponse = BuildResponse(board, ships);
            return invalidResponse;
        } 

        LandShip(_selectedShip!);

        _indexCurrentPlayer++;
        if (_indexCurrentPlayer < _players.Count)
        {
            CurrentPlayer = _players[_indexCurrentPlayer];
            IBoard nextBoard = _playerBoard[CurrentPlayer];
            List<IShip> nextShips = _playerShips[CurrentPlayer];
            _selectedShip = nextShips.First();
            _indexPlayerCursor = GetAnchorCoordinate(_selectedShip);
            _undoStack.Clear();
            _redoStack.Clear();
            LiftShip(_selectedShip);
            ShipPlacementResponseDto nextResponse = BuildResponse(nextBoard, nextShips);
            return nextResponse;
        }

        ShipPlacementResponseDto finishedResponse = BuildResponse(board, ships) with { IsPlacementPhaseFinished = true };
        return finishedResponse;
    }

    /// <summary>Constructs the response DTO from the current board, ships, and selection state.</summary>
    private ShipPlacementResponseDto BuildResponse(IBoard board, List<IShip> ships) =>
        new(board, ships, CurrentPlayer!, _selectedShip, _indexPlayerCursor, IsCurrentShipValid(board));

    /// <summary>Returns true if the selected ship's current placement is collision-free and in bounds.</summary>
    private bool IsCurrentShipValid(IBoard board)
    {
        if (_selectedShip?.Placement == null)
        {
            return false;
        }
        bool isValid = IsValidPlacement([.. _selectedShip.Placement.Select(c => c.Coordinate)], board);
        return isValid;
    }

    /// <summary>Returns true if the given coordinates are in bounds and have no adjacent ships on the board.</summary>
    private static bool IsValidPlacement(List<Coordinate> coords, IBoard board)
    {
        if (coords.Any(c => !IsInBounds(c, board.Size)))
        {
            return false;
        }

        int minX = Math.Max(0, coords.Min(c => (int)c.X) - 1);
        int maxX = Math.Min(board.Size - 1, coords.Max(c => (int)c.X) + 1);
        int minY = Math.Max(0, coords.Min(c => (int)c.Y) - 1);
        int maxY = Math.Min(board.Size - 1, coords.Max(c => (int)c.Y) + 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                // Since the selected ship is lifted, cell.Ship only contains other ships
                if (board.Cell[x, y].Ship != null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>Returns coordinates for a ship at a cursor position using a specific orientation.</summary>
    private static List<Coordinate> GetShipCoordinatesWithOrientation(IShip ship, Coordinate cursor, Orientation orientation)
    {
        int length = (int)ship.ShipType;
        int anchorIndex = length / 2;
        List<Coordinate> coords = new List<Coordinate>(length);

        for (int i = 0; i < length; i++)
        {
            coords.Add(orientation == Orientation.Vertical
                ? GetCoordinate(cursor.X, (VerticalLabel)((int)cursor.Y - anchorIndex + i))
                : GetCoordinate((HorizontalLabel)((int)cursor.X - anchorIndex + i), cursor.Y));
        }

        return coords;
    }

    /// <summary>Returns the middle cell coordinate of the ship, used as the cursor reference point.</summary>
    private static Coordinate GetAnchorCoordinate(IShip ship)
    {
        int anchorIndex = (int)ship.ShipType / 2;
        Coordinate anchor = ship.Placement![anchorIndex].Coordinate;
        return anchor;
    }

    /// <summary>Returns true if the coordinate falls within the board boundaries.</summary>
    private static bool IsInBounds(Coordinate coord, int boardSize) =>
        (int)coord.X >= 0 && (int)coord.X < boardSize &&
        (int)coord.Y >= 0 && (int)coord.Y < boardSize;

    /// <summary>Clears the ship reference from its occupied cells so it no longer blocks collision checks.</summary>
    private static void LiftShip(IShip ship)
    {
        if (ship.Placement == null)
        {
            return;
        }
        foreach (ICell cell in ship.Placement.Where(c => c.Ship == ship))
        {
            cell.Ship = null;
        }
    }

    /// <summary>Writes the ship reference back into its occupied cells to mark them as taken.</summary>
    private static void LandShip(IShip ship)
    {
        if (ship.Placement == null)
        {
            return;
        }
        foreach (ICell cell in ship.Placement)
        {
            cell.Ship = ship;
        }
    }

    /// <summary>Updates the ship's placement list to the new cell references without modifying cell.Ship.</summary>
    private static void UpdateShipPlacement(IShip ship, List<Coordinate> coords, IBoard board) =>
        ship.Placement = [.. coords.Select(c => board.Cell[(int)c.X, (int)c.Y])];

    /// <summary>Captures current ship positions, orientations, and cursor into a PlacementState.</summary>
    private PlacementState CreateSnapshot(List<IShip> ships)
    {
        List<(IShip Ship, List<Coordinate> Coords, Orientation Orientation)> shipStates = ships
            .Select(s => (s, s.Placement?.Select(c => c.Coordinate).ToList() ?? new List<Coordinate>(), s.Orientation))
            .ToList();
        PlacementState snapshot = new PlacementState(shipStates, ships.IndexOf(_selectedShip!), _indexPlayerCursor);
        return snapshot;
    }

    /// <summary>Clears the board and restores all ship placements from a PlacementState.</summary>
    private void ApplySnapshot(PlacementState state, IBoard board, List<IShip> ships)
    {
        // Clear all cells
        for (int x = 0; x < board.Size; x++)
        {
            for (int y = 0; y < board.Size; y++)
            {
                board.Cell[x, y].Ship = null;
            }
        }

        _selectedShip = state.SelectedShipIndex >= 0 ? ships[state.SelectedShipIndex] : null;
        _indexPlayerCursor = state.Cursor;

        // Restore ship placements from snapshot coordinates
        foreach ((IShip ship, List<Coordinate> coords, Orientation orientation) in state.ShipStates)
        {
            ship.Orientation = orientation;
            ship.Placement = [.. coords.Select(c => board.Cell[(int)c.X, (int)c.Y])];
        }

        // Land all ships except the selected one (which stays lifted)
        foreach (IShip ship in ships.Where(s => s != _selectedShip))
        {
            LandShip(ship);
        }
    }

    /// <summary>Pushes the current state onto the undo stack, evicting the oldest entry if at capacity.</summary>
    private void PushUndo(List<IShip> ships)
    {
        if (_undoStack.Count >= MaxUndoSteps)
        {
            _undoStack.RemoveAt(0);
        }
        _undoStack.Add(CreateSnapshot(ships));
    }

    /// <summary>Places all ships vertically in columns as the initial default arrangement.</summary>
    private static List<IShip> PlaceShipsDefault(IBoard board)
    {
        List<IShip> ships = new List<IShip>();
        ShipType[] shipTypes = Enum.GetValues<ShipType>();

        for (int col = 0; col < shipTypes.Length; col++)
        {
            ShipType shipType = shipTypes[col];
            Ship ship = new Ship(shipType, Orientation.Vertical);
            List<ICell> placement = new List<ICell>();

            for (int row = 0; row < (int)shipType; row++)
            {
                ICell cell = board.Cell[col * 2, row];
                cell.Ship = ship;
                placement.Add(cell);
            }

            ship.Placement = placement;
            ships.Add(ship);
        }

        return ships;
    }

    /// <summary>Generates cells within a 2-dimensional array.</summary>
    private ICell[,] GenerateCells(int boardSize)
    {
        ICell[,] cells = new ICell[boardSize, boardSize];

        for (int x = 0; x < boardSize; x++)
        {
            for (int y = 0; y < boardSize; y++)
            {
                Coordinate coordinate = GetCoordinate((HorizontalLabel)x, (VerticalLabel)y);
                cells[x, y] = new Cell(coordinate);
            }
        }

        return cells;
    }
}
