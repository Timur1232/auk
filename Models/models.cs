using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
namespace App.Models;

[Index(nameof(email), IsUnique = true)]
public class User
{
    [Key]          public required string login {get; set;}
    [EmailAddress] public required string? email {get; set;}
    [Required]     public required string password_hash {get; set;}

    public record LoginRequest(string login_or_email, string password, string? last_saved_location = null);
    public record RegisterRequest(string login, string? email, string password = "", string password_confirm = "", string? last_saved_location = null);

    public record Dto(string login, string? email);
    public Dto Res() => new Dto(login, email);
}

public class Lot
{
    [Key] public uint id {get; set;}

    [Required]
    public string user_login {get; set;} = null!;

    public uint tag_id {get; set;}

    [Required] public string title {get; set;} = null!;
               public int count {get; set;} = 1;
    [Required] public decimal initial_price {get; set;}
               public decimal current_price {get; set;}
    [Required] public DateTimeOffset end_time {get; set;}


    public record CreateRequest(
        string title,
        int count,
        decimal initial_price,
        DateTime end_time,
        uint tag_id
    );

    public static Lot From(CreateRequest data, string user_login)
    {
        var lot = new Lot {
            title = data.title,
            user_login = user_login,
            tag_id = data.tag_id,
            count = data.count,
            initial_price = data.initial_price,
            current_price = data.initial_price,
            end_time = data.end_time,
        };
        return lot;
    }

    public async Task<Tag?> GetTag(AuctionDbContext db) => await db.tags.FindAsync(tag_id);
    public IQueryable<LotImage> GetImages(AuctionDbContext db) => db.lot_images.Where(i => i.lot_id == id);
}

public class LotImage
{
    [Key] public uint id {get; set;}
    public uint lot_id {get; set;}
    [Required] public string image_path {get; set;} = null!;
}

[Index(nameof(name), IsUnique = true)]
public class Tag
{
    [Key] public uint id {get; set;}
    [Required] public string name {get; set;} = null!;

    public record CreateRequest(string name);

    public static Tag From(CreateRequest req)
    {
        return new Tag {
            name = req.name,
        };
    }

    public IQueryable<Lot> GetLots(AuctionDbContext db) => db.lots.Where(l => l.tag_id == id);
}
