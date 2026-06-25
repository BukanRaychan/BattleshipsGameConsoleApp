using ConsoleApp.Models;
using ConsoleApp.Types;

namespace ConsoleApp.DTOs;

public record AttackResponseDto(
    IBoard PlayerBoard,
    IBoard OpponentBoard,
    IPlayer CurrentPlayer,
    Coordinate? LastAttack,
    AttackResult? LastResult,
    bool IsGameOver,
    IPlayer? Winner
);
