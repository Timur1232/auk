namespace App.Models;

public class HomePageModel
{
    public struct LotCard
    {
        public uint id;
        public string? image_path;
        public string title;
        public decimal price;
        public DateTimeOffset end_time;
        public bool closed;
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
    public bool price_changed = false;
}

public class UserInfoData
{
    public string user_login = null!;
    public List<UserLotCard> lots = new();
}

public class UserLotCard
{
    public uint id;
    public string title = null!;
    public decimal current_price;
    public string? thumbnail_path;
}
