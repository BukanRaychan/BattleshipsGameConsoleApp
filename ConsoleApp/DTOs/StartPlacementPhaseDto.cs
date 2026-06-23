namespace ConsoleApp.DTOs;

public record StartPlacementPhaseDto(
    string PlayerOneName, 
    string PlayerTwoName, 
    int BoardSize = 10
);
