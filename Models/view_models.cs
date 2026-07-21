namespace App.Models;

public record HomePageQuery(
    int page = 0,
    int page_size = LotsModel.DEFAULT_PAGE_SIZE,
    uint? tag_id = null,
    string? search = null,
    string sort_by = "end_time",
    string sort_dir = "asc",
    bool show_closed = false
);

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

    public List<LotCard> cards = null!;
    public List<Tag> tags = null!;
    public HomePageQuery query = null!;
    public int total_count;
    public int pages_count;
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

public class AdminPageModel
{
    public List<Tag> Tags { get; set; } = new();
    public string? Search { get; set; }
    public bool Success { get; set; }
}

public class TagEditForm
{
    public uint TagId { get; set; }
    public string Name {get; set;} = null!;
    public bool Success {get; set;}
}
