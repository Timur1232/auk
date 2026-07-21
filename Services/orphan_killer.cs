using Microsoft.EntityFrameworkCore;
using App.Models;
namespace App.Services;

public class OrphanImagesKiller : BackgroundService
{
    private string uploads_path = null!;
    private IServiceScopeFactory scope_factory;
    private IWebHostEnvironment env;

    public OrphanImagesKiller(IServiceScopeFactory scope_factory, IWebHostEnvironment env) {
        this.scope_factory = scope_factory;
        this.env = env;
        uploads_path = Path.Combine(env.WebRootPath, "uploads", "lot_images");
        Directory.CreateDirectory(uploads_path);
    }

    protected override async Task ExecuteAsync(CancellationToken stopping_token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(60));
        while (await timer.WaitForNextTickAsync(stopping_token))
        {
            await FindAndKill(stopping_token);
        }
    }

    public async Task FindAndKill(CancellationToken stopping_token)
    {
        try {
            using var scope = scope_factory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();

            var files = System.IO.Directory.GetFiles(uploads_path).Select(f => Path.GetFileName(f));

            var images_to_delete = new List<string>();
            var images = await db.lot_images.Select(i => Path.GetFileName(i.image_path)).ToListAsync();

            foreach (var file in files) {
                if (!images.Contains(file)) {
                    images_to_delete.Add(file);
                    G.Log(LogLevel.Info, $"Deleting orphan image: {file}");
                }
            }

            foreach (var file_to_delete in images_to_delete) {
                var file_path = Path.Combine(uploads_path, file_to_delete);
                if (System.IO.File.Exists(file_path)) {
                    System.IO.File.Delete(file_path);
                }
            }
        } catch (Exception e) {
            G.Log(LogLevel.Error, e.Message);
        }
    }
}
