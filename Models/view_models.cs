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
