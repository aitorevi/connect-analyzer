namespace ConnectAnalytics.Domain;

// Read model: total amount aggregated per customer.
public record CustomerTotal(string CustomerId, decimal TotalAmount);
