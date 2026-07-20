namespace App.Models;

public class HomePageModel
{
    public struct LotCard
    {
        public uint id;
        public string? image_path;
        public string title;
        public decimal price;
    }

    public IEnumerable<LotCard> cards = null!;
    public List<Tag> tags = null!;
}

public class CreateFormData
{
    public List<Tag> tags = new();
    public Lot? current_lot = null;
}

public class TagNavigation
{
    public uint id;
    public required string name;
}

public class LotDetailsViewModel
{
    public Lot lot = new();
    public List<LotImage> images = new();
    public User? seller;
    public bool is_owner;
}

public class UserLotsViewModel
{
    public User.Dto user = null!;
    public List<UserLotCard> lots = new();
}

public class UserLotCard
{
    public uint id;
    public string title = null!;
    public decimal current_price;
    public string? thumbnail_path;
}

public class BidFormData
{
    public uint lot_id;
    public decimal price;
    public bool price_changed;
}
