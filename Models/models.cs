using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
namespace App.Models;

[Index(nameof(email), IsUnique = true)]
public class User
{
    [Key]          public required string login {get; set;}
    [EmailAddress] public required string? email {get; set;}
    [Required]     public required string password_hash {get; set;}

    public bool is_admin {get; set;} = false;

    [InverseProperty(nameof(Lot.user))]
    public ICollection<Lot> lots {get; set;} = new List<Lot>();
    [InverseProperty(nameof(Bid.user))]
    public ICollection<Bid> bets {get; set;} = new List<Bid>();
    [InverseProperty(nameof(Purchase.user))]
    public ICollection<Purchase> purchases {get; set;} = new List<Purchase>();

    public record LoginRequest(string login_or_email, string password, string? last_saved_location = null);
    public record RegisterRequest(string login, string? email, string password = "", string password_confirm = "", string? last_saved_location = null);

    public record Dto(string login, string? email);
    public Dto ToDto() => new Dto(login, email);
}

public enum PaymentMethod
{
    None,
    OnMeeting,
    ViaBank,
}

public enum DeliveryPayment
{
    PaidBySeller,
    PaidByCustomer,
}

public enum LotStatus
{
    Playing,
    WaitingPurchaseConfirm,
    Purchased,
    Denied,
}

public class Lot
{
    [Key] public uint id {get; set;}

    [Required] public string user_login {get; set;} = null!;
    [Required] public uint tag_id {get; set;}

    [Required] public string title            {get; set;} = null!;
               public int count               {get; set;} = 1;
    [Required] public decimal initial_price   {get; set;}
               public decimal current_price   {get; set;}

    [Required] public DateTimeOffset end_time {get; set;}

    public string? description {get; set;}
    public string? city        {get; set;}
    [Required] public string payment_method   {get; set;} = PaymentMethod.None.ToString();
    [Required] public string delivery_payment {get; set;} = DeliveryPayment.PaidByCustomer.ToString();

    public string? leader_login {get; set;}
    public bool closed {get; set;} = false;

    public string status {get; set;} = LotStatus.Playing.ToString();

    public bool UpdateClosed()
    {
        var now = DateTime.UtcNow;
        if (end_time <= now) {
            closed = true;
            status = LotStatus.WaitingPurchaseConfirm.ToString();
        }
        return closed;
    }

    [ForeignKey(nameof(leader_login))]
    public User? leader {get; set;}
    [ForeignKey(nameof(user_login))]
    public User? user {get; set;}
    [ForeignKey(nameof(tag_id))]
    public Tag? tag {get; set;}

    [InverseProperty(nameof(LotImage.lot))]
    public ICollection<LotImage> images {get; set;} = new List<LotImage>();

    public LotImage? FirstImage()
    {
        return images.OrderBy(i => i.id).FirstOrDefault();
    }

    [InverseProperty(nameof(Bid.lot))]
    public ICollection<Bid> bids {get; set;} = new List<Bid>();

    public record CreateRequest(
        string title,
        string? description,
        string city,
        string payment_method,
        string delivery_payment,
        int count,
        decimal price,
        DateTime end_time,
        uint tag_id,
        IFormFile? thumbnail,
        IFormFileCollection? images
    );

    public record EditRequest(
        string? title,
        string? description,
        string? city,
        string? payment_method,
        string? delivery_payment,
        int? count,
        DateTime? end_time,
        uint? tag_id,
        IFormFile? thumbnail,
        IFormFileCollection? images
    );

    public static Lot From(CreateRequest data, string user_login)
    {
        var lot = new Lot {
            title = data.title,
            user_login = user_login,
            tag_id = data.tag_id,
            count = data.count,
            initial_price = data.price,
            current_price = data.price,
            end_time = data.end_time,
            description = data.description,
            city = data.city,
            payment_method = data.payment_method,
            delivery_payment = data.delivery_payment,
        };
        return lot;
    }

    public void Update(EditRequest data)
    {
        title = data.title!;
        tag_id = (uint)data.tag_id!;
        count = (int)data.count!;
        end_time = (DateTimeOffset)data.end_time!;
        description = data.description;
        city = data.city;
        payment_method = data.payment_method!;
        delivery_payment = data.delivery_payment!;
    }

    public decimal RecalculatePrice()
    {
        var last_bet = bids.Where(b => b.lot_id == id)
            .OrderByDescending(b => b.id)
            .FirstOrDefault();
        if (last_bet == null) {
            current_price = initial_price;
            return current_price;
        }
        current_price = last_bet.price;
        return current_price;
    }
}

public class LotImage
{
    [Key] public uint id {get; set;}
    [ForeignKey(nameof(Lot))] public uint lot_id {get; set;}
    [Required] public string image_path {get; set;} = null!;

    [ForeignKey(nameof(lot_id))]
    public Lot? lot {get; set;}
}

[Index(nameof(name), IsUnique = true)]
public class Tag
{
    [Key] public uint id {get; set;}
    [Required] public string name {get; set;} = null!;

    public record CreateRequest(string name);
    public record UpdateRequest(string name);

    public static Tag From(CreateRequest req)
    {
        return new Tag {
            name = req.name,
        };
    }

    [InverseProperty(nameof(Lot.tag))]
    public ICollection<Lot> lots {get; set;} = new List<Lot>();
}

public class Bid
{
    [Key] public uint id {get; set;}
    [Required] public required string user_login {get; set;}
    [Required] public uint lot_id {get; set;}

    [Required] public decimal price {get; set;}

    [ForeignKey(nameof(user_login))]
    public User? user {get; set;}
    [ForeignKey(nameof(lot_id))]
    public Lot? lot {get; set;}
}

public class Purchase
{
    [Key] public uint id {get; set;}
    [Required] public required string user_login {get; set;}
    [Required] public uint lot_id {get; set;}

    [Required] public decimal locked_price {get; set;}

    [ForeignKey(nameof(user_login))]
    public User? user {get; set;}
    [ForeignKey(nameof(lot_id))]
    public Lot? lot {get; set;}
}
