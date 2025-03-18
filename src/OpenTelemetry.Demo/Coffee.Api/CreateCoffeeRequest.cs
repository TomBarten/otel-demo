namespace Coffee.Api;

public sealed record CreateCoffeeRequest
{
    public required string Name { get; init; }
    
    public double CaffeineAmount { get; init; }
    
    public double Price { get; init; }
}