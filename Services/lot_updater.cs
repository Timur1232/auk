using Microsoft.EntityFrameworkCore;
using App.Models;
namespace App.Services;

public class AuctionClosingService(IServiceScopeFactory scope_factory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stopping_token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync(stopping_token))
        {
            await CheckAndCloseAuctions(stopping_token);
        }
    }

    public async Task CheckAndCloseAuctions(CancellationToken stopping_token)
    {
        using var scope = scope_factory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

        try {
            var now = DateTime.UtcNow;
            var expired = await db.lots
                .Where(l => !l.closed)
                .ToListAsync();

            foreach (var l in expired) {
                l.UpdateClosed();
            }

            await db.SaveChangesAsync();
        } catch (Exception e) {
            G.Log(LogLevel.Error, e.Message);
        }
    }
}
