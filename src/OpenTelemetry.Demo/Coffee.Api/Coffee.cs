namespace Coffee.Api;

public sealed class Coffee
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public required string Name { get; init; }
    
    public double CaffeineAmount { get; init; }
    
    public double Price { get; init; }
    
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.Now;
}