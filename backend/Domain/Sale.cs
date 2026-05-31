namespace ConnectAnalyzer.Domain;

public record Sale(
    DateOnly Date,
    string CustomerId,
    string ProductName,
    int Quantity,
    decimal Amount);
