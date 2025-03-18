using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Coffee.Api.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class CoffeeController : ControllerBase
{
    private readonly CoffeeDbContext _dbContext;
    private readonly ILogger<CoffeeController> _logger;

    public CoffeeController(CoffeeDbContext dbContext, ILogger<CoffeeController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    [HttpPost(Name = "CreateCoffee")]
    public async Task<IActionResult> CreateCoffee(CreateCoffeeRequest request)
    {
        _logger.LogInformation("Adding coffee");

        if (await _dbContext.Coffees.CountAsync(coffee => coffee.Name == request.Name) > 0)
        {
            _logger.LogWarning("Coffee with name {name} already exists", request.Name);

            return Conflict("Coffee with the same name already exists");
        }
        
        _dbContext.Coffees.Add(new Coffee
        {
            Name = request.Name,
            CaffeineAmount = request.CaffeineAmount,
            Price = request.Price
        });
        
        await _dbContext.SaveChangesAsync();
        return CreatedAtAction(nameof(GetCoffee), new { name = request.Name }, request);
    }

    [HttpGet(Name = "GetCoffees")]
    public IAsyncEnumerable<Coffee> GetCoffee()
    {
        _logger.LogInformation("Retrieving coffees");
        
        return _dbContext.Coffees.AsAsyncEnumerable();
    }
}