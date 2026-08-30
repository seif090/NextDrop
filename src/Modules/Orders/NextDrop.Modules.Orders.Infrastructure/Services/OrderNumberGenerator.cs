using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NextDrop.Infrastructure.Persistence;
using NextDrop.Modules.Orders.Application.Abstractions;

namespace NextDrop.Modules.Orders.Infrastructure.Services;

public class OrderNumberGenerator : IOrderNumberGenerator
{
    private readonly NextDropDbContext _context;

    public OrderNumberGenerator(NextDropDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var year = DateTime.UtcNow.Year;
            var randomPart = GetRandomAlphanumeric(8);
            var orderNumber = $"ND-{year}-{randomPart}";

            var exists = await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber, cancellationToken);
            if (!exists)
                return orderNumber;
        }
    }

    private static string GetRandomAlphanumeric(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[length];
        var bytes = RandomNumberGenerator.GetBytes(length);
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[bytes[i] % chars.Length];
        }
        return new string(result);
    }
}
