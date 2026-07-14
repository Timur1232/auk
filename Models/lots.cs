using Microsoft.EntityFrameworkCore;
namespace App.Models;

public class LotsModel(AuctionDbContext db)
{
    public const int DEFAULT_PAGE_SIZE = 15;

    public async Task<Lot?> GetById(uint id)
    {
        var lot = await db.lots.FindAsync(id);
        return lot;
    }

    public IQueryable<Lot> GetPage(uint? tag_id, int page, int page_size = DEFAULT_PAGE_SIZE) {
        var lots_query = tag_id switch {
            null => db.lots,
            _ => db.lots.Where(l => l.tag_id == tag_id)
        };
        return lots_query.Skip(page*page_size).Take(page_size);
    }

    public async Task<HomePageModel> HomePage(uint? tag_id, int page, int page_size = DEFAULT_PAGE_SIZE)
    {
        var lots = GetPage(tag_id, page, page_size);
        var lot_cards = new List<HomePageModel.LotCard>();
        foreach (var lot in lots) {
            var image = await lot.GetImages(db).FirstOrDefaultAsync();
            lot_cards.Add(new HomePageModel.LotCard{
                id = lot.id,
                image_path = image?.image_path,
                title = lot.title,
                price = lot.current_price,
            });
        }
        var tags = await db.tags.ToListAsync();
        var home_page = new HomePageModel {
            cards = lot_cards,
            tags = tags,
        };
        return home_page;
    }

    public async Task<(Lot? deleted_lot, ModelError err)> DeleteById(uint id)
    {
        var lot = await db.lots.FindAsync(id);
        if (lot == null) {
            return (null, ModelError.NotExist);
        }
        var entry = db.lots.Remove(lot);
        if (!await db.TrySaveChangesAsync()) {
            return (null, ModelError.DbError);
        }
        return (entry.Entity, ModelError.None);
    }

    public async Task<(Lot? updated_lot, ModelError err)> UpdateById(uint id, Lot.CreateRequest req)
    {
        var lot = await db.lots.FindAsync(id);
        if (lot == null) {
            return (null, ModelError.NotExist);
        }
        var entry = db.lots.Update(lot);
        if (!await db.TrySaveChangesAsync()) {
            return (null, ModelError.DbError);
        }
        return (entry.Entity, ModelError.None);
    }
}
