using ConsoleApp.DTOs;
using ConsoleApp.Models;
using ConsoleApp.Types;

namespace ConsoleApp.Services;

public class GameService : IGameService
{
    private List<IPlayer> _players;
    private readonly Dictionary<IPlayer, IBoard> _playerBoard;
    private readonly Dictionary<IPlayer, List<IShip>> _playerShips;
    public IPlayer? CurrentPlayer { get; set; }
    private int _indexCurrentPlayer = 0;
    private IShip? _selectedShip;
    private Coordinate _indexPlayerCursor;
    private readonly List<PlacementState> _undoStack = [];
    private readonly List<PlacementState> _redoStack = [];
    private const int MaxUndoSteps = 5;
    private MessageDelegate _messageProvider;

    private record PlacementState(
        List<(IShip Ship, List<Coordinate> Coords, Orientation Orientation)> ShipStates,
        int SelectedShipIndex,
        Coordinate Cursor
    );

    /// <summary>Initializes empty player, board, and ship collections.</summary>
    public GameService(MessageDelegate messageEventSubscriber)
    {
        _players = new();
        _playerBoard = new();
        _playerShips = new();
        CurrentPlayer = null;
        _messageProvider += messageEventSubscriber;
    }

    /// <summary>Sets up players, boards, and default ship positions; lifts the first ship for placement.</summary>
    public ShipPlacementResponseDto StartShipPlacementPhase(StartPlacementPhaseDto dto)
    {
        _players.AddRange([new Player(dto.PlayerOneName), new Player(dto.PlayerTwoName)]);

        foreach (var player in _players)
        {
            var board = new Board(dto.BoardSize);
            _playerBoard[player] = board;
            _playerShips[player] = PlaceShipsDefault(board);
        }

        CurrentPlayer = _players[_indexCurrentPlayer];
        var currentBoard = _playerBoard[CurrentPlayer];
        var currentShips = _playerShips[CurrentPlayer];
        _selectedShip = currentShips.First();
        _indexPlayerCursor = GetAnchorCoordinate(_selectedShip);

        // Lift the first ship off the board so cell.Ship only tracks non-selected ships
        LiftShip(_selectedShip);

        return new ShipPlacementResponseDto(
            currentBoard,
            currentShips,
            CurrentPlayer,
            _selectedShip,
            _indexPlayerCursor,
            true
        );
    }

    /// <summary>Dispatches a key event to the appropriate placement handler and returns the updated state.</summary>
    public ShipPlacementResponseDto EditShipPlacement(EditShipPlacementDto dto)
    {
        var ships = _playerShips[CurrentPlayer!];
        var board = _playerBoard[CurrentPlayer!];
        _selectedShip = dto.CurrentShip;
        _indexPlayerCursor = dto.IndexPlayerCursor;

        return dto.KeyEvent switch
        {
            ConsoleKey.Q  => HandlePrevShip(ships, board),
            ConsoleKey.W or ConsoleKey.UpArrow => HandleShipPlacementMove(0, -1, board),
            ConsoleKey.E => HandleNextShip(ships, board),
            ConsoleKey.R => HandleRotate(board),
            ConsoleKey.A or ConsoleKey.LeftArrow => HandleShipPlacementMove(-1, 0, board),
            ConsoleKey.S or ConsoleKey.DownArrow => HandleShipPlacementMove(0, 1, board),
            ConsoleKey.D or ConsoleKey.RightArrow => HandleShipPlacementMove(1, 0, board),
            ConsoleKey.Z => HandleUndo(board),
            ConsoleKey.X => HandleRedo(board),
            ConsoleKey.C => HandleConfirm(ships, board),
            _ => BuildResponse(board, ships)
        };
    }

    /// <summary>Moves the selected ship by (dx, dy); rejects the move if it goes out of bounds.</summary>
    private ShipPlacementResponseDto HandleShipPlacementMove(int dx, int dy, IBoard board)
    {
        var ships = _playerShips[CurrentPlayer!];
        int newX = (int)_indexPlayerCursor.X + dx;
        int newY = (int)_indexPlayerCursor.Y + dy;

        if (newX < 0 || newX >= board.Size || newY < 0 || newY >= board.Size)
            return BuildResponse(board, ships);

        var newCursor = new Coordinate((HorizontalLabel)newX, (VerticalLabel)newY);
        var newCoords = GetShipCoordinates(_selectedShip!, newCursor);

        if (!newCoords.All(c => IsInBounds(c, board.Size)))
            return BuildResponse(board, ships);

        PushUndo(ships);

        // Only update the ship's intended placement — never touch cell.Ship for the lifted ship
        UpdateShipPlacement(_selectedShip!, newCoords, board);
        _indexPlayerCursor = newCursor;
        _redoStack.Clear();

        return BuildResponse(board, ships);
    }

    /// <summary>Toggles orientation and shifts the cursor so the rotated ship always fits within board bounds.</summary>
    private ShipPlacementResponseDto HandleRotate(IBoard board)
    {
        var ships = _playerShips[CurrentPlayer!];
        var newOrientation = _selectedShip!.Orientation == Orientation.Vertical
            ? Orientation.Horizontal
            : Orientation.Vertical;

        var newCoords = GetShipCoordinatesWithOrientation(_selectedShip, _indexPlayerCursor, newOrientation);

        int cursorX = (int)_indexPlayerCursor.X;
        int cursorY = (int)_indexPlayerCursor.Y;

        int minX = newCoords.Min(c => (int)c.X);
        int maxX = newCoords.Max(c => (int)c.X);
        int minY = newCoords.Min(c => (int)c.Y);
        int maxY = newCoords.Max(c => (int)c.Y);

        if (minX < 0) cursorX -= minX;
        if (maxX >= board.Size) cursorX -= maxX - (board.Size - 1);
        if (minY < 0) cursorY -= minY;
        if (maxY >= board.Size) cursorY -= maxY - (board.Size - 1);

        _indexPlayerCursor = new Coordinate((HorizontalLabel)cursorX, (VerticalLabel)cursorY);
        newCoords = GetShipCoordinatesWithOrientation(_selectedShip, _indexPlayerCursor, newOrientation);

        PushUndo(ships);
        _selectedShip.Orientation = newOrientation;
        UpdateShipPlacement(_selectedShip, newCoords, board);
        _redoStack.Clear();

        return BuildResponse(board, ships);
    }

    /// <summary>Lands the current ship and selects the next one in the list, wrapping to the first.</summary>
    private ShipPlacementResponseDto HandleNextShip(List<IShip> ships, IBoard board)
    {
        if (!IsCurrentShipValid(board)){
            _messageProvider?.Invoke("Invalid placement — the ship must not overlap with or be within 1 cell of another ship. Cannot proceed to select the next ship.", MessageType.Error);
            return BuildResponse(board, ships);
        }

        LandShip(_selectedShip!);

        _selectedShip = ships.SkipWhile(s => s != _selectedShip).Skip(1).FirstOrDefault()
            ?? ships.First();
        _indexPlayerCursor = GetAnchorCoordinate(_selectedShip);

        LiftShip(_selectedShip);

        return BuildResponse(board, ships);
    }

    /// <summary>Lands the current ship and selects the previous one in the list, wrapping to the last.</summary>
    private ShipPlacementResponseDto HandlePrevShip(List<IShip> ships, IBoard board)
    {
        if (!IsCurrentShipValid(board)) {
            _messageProvider?.Invoke("Invalid placement — the ship must not overlap with or be within 1 cell of another ship. Cannot proceed to select the previous ship.", MessageType.Error);
            return BuildResponse(board, ships);
        }

        LandShip(_selectedShip!);

        _selectedShip = ships.AsEnumerable().Reverse().SkipWhile(s => s != _selectedShip).Skip(1).FirstOrDefault()
            ?? ships.Last();
        _indexPlayerCursor = GetAnchorCoordinate(_selectedShip);

        LiftShip(_selectedShip);

        return BuildResponse(board, ships);
    }

    /// <summary>Restores the last saved placement state from the undo stack.</summary>
    private ShipPlacementResponseDto HandleUndo(IBoard board)
    {
        var ships = _playerShips[CurrentPlayer!];
        if (_undoStack.Count == 0)
            return BuildResponse(board, ships);

        _redoStack.Add(CreateSnapshot(ships));
        var state = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        ApplySnapshot(state, board, ships);

        return BuildResponse(board, ships);
    }

    /// <summary>Re-applies the last undone placement state from the redo stack.</summary>
    private ShipPlacementResponseDto HandleRedo(IBoard board)
    {
        var ships = _playerShips[CurrentPlayer!];
        if (_redoStack.Count == 0)
            return BuildResponse(board, ships);

        PushUndo(ships);
        var state = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        ApplySnapshot(state, board, ships);

        return BuildResponse(board, ships);
    }

    /// <summary>Validates all ship placements; if valid, advances to the next player or ends the placement phase.</summary>
    private ShipPlacementResponseDto HandleConfirm(List<IShip> ships, IBoard board)
    {
        if (!IsCurrentShipValid(board)){
            _messageProvider?.Invoke("Invalid placement — the ship must not overlap with or be within 1 cell of another ship. Cannot proceed to confirmation step.", MessageType.Error);
            return BuildResponse(board, ships);
        }

        LandShip(_selectedShip!);

        _indexCurrentPlayer++;
        if (_indexCurrentPlayer < _players.Count)
        {
            CurrentPlayer = _players[_indexCurrentPlayer];
            var nextBoard = _playerBoard[CurrentPlayer];
            var nextShips = _playerShips[CurrentPlayer];
            _selectedShip = nextShips.First();
            _indexPlayerCursor = GetAnchorCoordinate(_selectedShip);
            _undoStack.Clear();
            _redoStack.Clear();
            LiftShip(_selectedShip);
            return BuildResponse(nextBoard, nextShips);
        }

        return BuildResponse(board, ships) with { IsPlacementPhaseFinished = true };
    }

    /// <summary>Constructs the response DTO from the current board, ships, and selection state.</summary>
    private ShipPlacementResponseDto BuildResponse(IBoard board, List<IShip> ships) =>
        new(board, ships, CurrentPlayer!, _selectedShip, _indexPlayerCursor, IsCurrentShipValid(board));

    /// <summary>Returns true if the selected ship's current placement is collision-free and in bounds.</summary>
    private bool IsCurrentShipValid(IBoard board)
    {
        if (_selectedShip?.Placement == null) return false;
        return IsValidPlacement([.. _selectedShip.Placement.Select(c => c.Coordinate)], board);
    }

    /// <summary>Returns true if the given coordinates are in bounds and have no adjacent ships on the board.</summary>
    private static bool IsValidPlacement(List<Coordinate> coords, IBoard board)
    {
        if (coords.Any(c => !IsInBounds(c, board.Size))) return false;

        int minX = Math.Max(0, coords.Min(c => (int)c.X) - 1);
        int maxX = Math.Min(board.Size - 1, coords.Max(c => (int)c.X) + 1);
        int minY = Math.Max(0, coords.Min(c => (int)c.Y) - 1);
        int maxY = Math.Min(board.Size - 1, coords.Max(c => (int)c.Y) + 1);

        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        {
            // Since the selected ship is lifted, cell.Ship only contains other ships
            if (board.Cell[x, y].Ship != null)
                return false;
        }

        return true;
    }

    /// <summary>Returns the board coordinates the ship occupies at the given cursor position.</summary>
    private static List<Coordinate> GetShipCoordinates(IShip ship, Coordinate cursor) =>
        GetShipCoordinatesWithOrientation(ship, cursor, ship.Orientation);

    /// <summary>Returns coordinates for a ship at a cursor position using a specific orientation.</summary>
    private static List<Coordinate> GetShipCoordinatesWithOrientation(IShip ship, Coordinate cursor, Orientation orientation)
    {
        int length = (int)ship.ShipType;
        int anchorIdx = length / 2;
        var coords = new List<Coordinate>(length);

        for (int i = 0; i < length; i++)
            coords.Add(orientation == Orientation.Vertical
                ? new Coordinate(cursor.X, (VerticalLabel)((int)cursor.Y - anchorIdx + i))
                : new Coordinate((HorizontalLabel)((int)cursor.X - anchorIdx + i), cursor.Y));

        return coords;
    }

    /// <summary>Returns the middle cell coordinate of the ship, used as the cursor reference point.</summary>
    private static Coordinate GetAnchorCoordinate(IShip ship)
    {
        int anchorIdx = (int)ship.ShipType / 2;
        return ship.Placement![anchorIdx].Coordinate;
    }

    /// <summary>Returns true if the coordinate falls within the board boundaries.</summary>
    private static bool IsInBounds(Coordinate coord, int boardSize) =>
        (int)coord.X >= 0 && (int)coord.X < boardSize &&
        (int)coord.Y >= 0 && (int)coord.Y < boardSize;

    /// <summary>Clears the ship reference from its occupied cells so it no longer blocks collision checks.</summary>
    private static void LiftShip(IShip ship)
    {
        if (ship.Placement == null) return;
        foreach (var cell in ship.Placement.Where(c => c.Ship == ship))
            cell.Ship = null;
    }

    /// <summary>Writes the ship reference back into its occupied cells to mark them as taken.</summary>
    private static void LandShip(IShip ship)
    {
        if (ship.Placement == null) return;
        foreach (var cell in ship.Placement)
            cell.Ship = ship;
    }

    /// <summary>Updates the ship's placement list to the new cell references without modifying cell.Ship.</summary>
    private static void UpdateShipPlacement(IShip ship, List<Coordinate> coords, IBoard board)
    {
        ship.Placement = [..coords.Select(c => board.Cell[(int)c.X, (int)c.Y])];
    }

    /// <summary>Captures current ship positions, orientations, and cursor into a PlacementState.</summary>
    private PlacementState CreateSnapshot(List<IShip> ships)
    {
        var shipStates = ships
            .Select(s => (s, s.Placement?.Select(c => c.Coordinate).ToList() ?? [], s.Orientation))
            .ToList();
        return new PlacementState(shipStates, ships.IndexOf(_selectedShip!), _indexPlayerCursor);
    }

    /// <summary>Clears the board and restores all ship placements from a PlacementState.</summary>
    private void ApplySnapshot(PlacementState state, IBoard board, List<IShip> ships)
    {
        // Clear all cells
        for (int x = 0; x < board.Size; x++)
        for (int y = 0; y < board.Size; y++)
            board.Cell[x, y].Ship = null;

        _selectedShip = state.SelectedShipIndex >= 0 ? ships[state.SelectedShipIndex] : null;
        _indexPlayerCursor = state.Cursor;

        // Restore ship placements from snapshot coordinates
        foreach (var (ship, coords, orientation) in state.ShipStates)
        {
            ship.Orientation = orientation;
            ship.Placement = [.. coords.Select(c => board.Cell[(int)c.X, (int)c.Y])];
        }

        // Land all ships except the selected one (which stays lifted)
        foreach (var ship in ships.Where(s => s != _selectedShip))
            LandShip(ship);
    }

    /// <summary>Pushes the current state onto the undo stack, evicting the oldest entry if at capacity.</summary>
    private void PushUndo(List<IShip> ships)
    {
        if (_undoStack.Count >= MaxUndoSteps)
            _undoStack.RemoveAt(0);
        _undoStack.Add(CreateSnapshot(ships));
    }

    /// <summary>Places all ships vertically in columns as the initial default arrangement.</summary>
    private static List<IShip> PlaceShipsDefault(IBoard board)
    {
        var ships = new List<IShip>();
        var shipTypes = Enum.GetValues<ShipType>();

        for (int col = 0; col < shipTypes.Length; col++)
        {
            var shipType = shipTypes[col];
            var ship = new Ship(shipType, Orientation.Vertical);
            var placement = new List<ICell>();

            for (int row = 0; row < (int)shipType; row++)
            {
                var cell = board.Cell[col*2, row];
                cell.Ship = ship;
                placement.Add(cell);
            }

            ship.Placement = placement;
            ships.Add(ship);
        }

        return ships;
    }

    /// <summary>Resets to player 0 and returns the initial attack-phase state with no prior attack.</summary>
    public AttackResponseDto StartAttackPhase()
    {
        _indexCurrentPlayer = 0;
        CurrentPlayer = _players[0];
        var opponent = _players[1];
        return new AttackResponseDto(
            _playerBoard[CurrentPlayer],
            _playerBoard[opponent],
            CurrentPlayer,
            null, null, false, null
        );
    }

    /// <summary>Resolves an attack on the opponent's board, switches turns, and returns the updated state.</summary>
    public AttackResponseDto MakeAttack(AttackDto dto)
    {
        var attacker = CurrentPlayer!;
        var opponent = _players.First(p => p != attacker);
        var attackerBoard = _playerBoard[attacker];
        var opponentBoard = _playerBoard[opponent];
        var cell = opponentBoard.Cell[(int)dto.Target.X, (int)dto.Target.Y];

        if (cell.ReceivedAttackResult != null)
        {
            _messageProvider?.Invoke("That cell was already attacked!", MessageType.Error);
            return new AttackResponseDto(attackerBoard, opponentBoard, attacker, dto.Target, cell.ReceivedAttackResult, false, null);
        }

        AttackResult result;
        if (cell.Ship != null)
        {
            cell.ReceivedAttackResult = AttackResult.Hit;
            var shipPlacement = cell.Ship.Placement!;
            bool sunk = shipPlacement.All(c => c.ReceivedAttackResult != null);
            if (sunk)
            {
                foreach (var c in shipPlacement)
                    c.ReceivedAttackResult = AttackResult.Sunk;
                result = AttackResult.Sunk;
                _messageProvider?.Invoke($"{attacker.Name} sunk {opponent.Name}'s {cell.Ship.ShipType}!", MessageType.Info);
            }
            else
            {
                result = AttackResult.Hit;
                _messageProvider?.Invoke($"{attacker.Name} scored a hit!", MessageType.Info);
            }
        }
        else
        {
            cell.ReceivedAttackResult = AttackResult.Miss;
            result = AttackResult.Miss;
            _messageProvider?.Invoke($"{attacker.Name} missed.", MessageType.Info);
        }

        bool gameOver = _playerShips[opponent].All(s => s.Placement!.All(c => c.ReceivedAttackResult != null));
        if (gameOver)
            return new AttackResponseDto(attackerBoard, opponentBoard, attacker, dto.Target, result, true, attacker);

        CurrentPlayer = opponent;
        _indexCurrentPlayer = _players.IndexOf(opponent);
        return new AttackResponseDto(_playerBoard[opponent], attackerBoard, opponent, dto.Target, result, false, null);
    }
}
