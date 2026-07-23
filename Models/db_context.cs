using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Security.Claims;
using App.Helpers;
namespace App.Models;

public class AuctionDbContext(DbContextOptions opt) : DbContext(opt)
{
    public DbSet<User>     users      {get; set;} = null!;
    public DbSet<Lot>      lots       {get; set;} = null!;
    public DbSet<LotImage> lot_images {get; set;} = null!;
    public DbSet<Tag>      tags       {get; set;} = null!;
    public DbSet<Bid>      bids       {get; set;} = null!;
    public DbSet<Purchase> purchases  {get; set;} = null!;

    public async Task<User?> GetUserByClaims(ClaimsPrincipal user_claims)
    {
        var login_claim = user_claims.FindFirst("user_login");
        if (login_claim == null || string.IsNullOrWhiteSpace(login_claim.Value)) {
            return null;
        }
        var user_login = login_claim.Value;
        var user = await GetUserByLogin(user_login);
        return user;
    }

    public async Task<User?> GetUserByLogin(string login)
    {
        return await users.FirstOrDefaultAsync(u => u.login == login);
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await users.FirstOrDefaultAsync(u => u.email == email);
    }

    public async Task<User?> GetUserByLoginOrEmail(string login_or_email)
    {
        return await users.FirstOrDefaultAsync(u => u.login == login_or_email || u.email == login_or_email);
    }

    public async Task<bool> TrySaveChangesAsync()
    {
        try {
            await SaveChangesAsync();
        } catch (Exception e) {
            Log.Error(e.ToString());
            return false;
        }
        return true;
    }

    protected override void OnModelCreating(ModelBuilder model_builder)
    {
        foreach (var entity_type in model_builder.Model.GetEntityTypes()) {
            foreach (var property in entity_type.GetProperties()) {
                if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?)) {
                    property.SetValueConverter(
                        new ValueConverter<DateTimeOffset, DateTimeOffset>(
                            v => v.ToUniversalTime(),
                            v => v
                        )
                    );
                }
            }
        }
    }
}
