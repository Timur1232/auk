using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace App.Models;

public enum ModelError {
    None,
    DbError,
    NotExist,
}

public static class DeleteErrorMethod {
    extension(ModelError err) {
        public string GetMessage()
        {
            return err switch {
                ModelError.None => "Нет ошибки.",
                ModelError.DbError => "Ошибка записи в базу данных.",
                ModelError.NotExist => "Записи не существует.",
                _ => G.Unreachable<string>(nameof(ModelError)),
            };
        }
    }
}

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
            G.Log(LogLevel.Error, e.ToString());
            return false;
        }
        return true;
    }
}
