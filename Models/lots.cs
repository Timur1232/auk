using Microsoft.EntityFrameworkCore;
namespace App.Models;

public class LotsModel(AuctionDbContext db, IWebHostEnvironment env)
{
    public const int DEFAULT_PAGE_SIZE = 15;

    public async Task<Lot?> GetById(uint id)
    {
        // TODO: maybe separate into different methods or query view parameters
        var lot = await db.lots
            .Include(l => l.images)
            .Include(l => l.user)
            .Include(l => l.leader)
            .Include(l => l.bids.OrderByDescending(b => b.id))
            .FirstOrDefaultAsync(l => l.id == id);
        return lot;
    }

    public IQueryable<Lot> GetPage(uint? tag_id, int page, int page_size = DEFAULT_PAGE_SIZE) {
        var lots_query = tag_id switch {
            null => db.lots,
            _ => db.lots.Where(l => l.tag_id == tag_id)
        };
        return lots_query
            .Skip(page*page_size)
            .Take(page_size)
            .Include(l => l.images);
    }

    public async Task<HomePageModel> HomePage(uint? tag_id, int page, int page_size = DEFAULT_PAGE_SIZE)
    {
        var lots = GetPage(tag_id, page, page_size);
        var lot_cards = new List<HomePageModel.LotCard>();
        foreach (var lot in lots) {
            var image = lot.images.FirstOrDefault();
            lot_cards.Add(new HomePageModel.LotCard{
                id = lot.id,
                image_path = image?.image_path,
                title = lot.title,
                price = lot.current_price,
                end_time = lot.end_time,
                closed = lot.closed,
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
        var lot = await GetById(id);
        if (lot == null) {
            return (null, ModelError.NotExist);
        }

        var images = lot.images;

        var uploadsPath = Path.Combine(env.WebRootPath, "uploads", "lot_images");
        foreach (var img in images) {
            var file_name = Path.GetFileName(img.image_path);
            var file_path = Path.Combine(uploadsPath, file_name);
            if (System.IO.File.Exists(file_path)) {
                System.IO.File.Delete(file_path);
            }
        }

        db.lots.Remove(lot);

        if (!await db.TrySaveChangesAsync()) {
            return (null, ModelError.DbError);
        }

        return (lot, ModelError.None);
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

    public async Task<(Lot? lot, List<string> errors)> CreateLot(Lot.CreateRequest req, string user_login)
    {
        var errors = Validate.All(req, db);
        if (errors.Count > 0) return (null, errors);

        var lot = Lot.From(req, user_login);

        var uploads_path = Path.Combine(env.WebRootPath, "uploads", "lot_images");
        Directory.CreateDirectory(uploads_path);

        var images_to_save = new List<IFormFile>();
        if (req.thumbnail != null) images_to_save.Add(req.thumbnail);
        if (req.images != null)    images_to_save.AddRange(req.images);

        foreach (var file in images_to_save) {
            if (file.Length == 0) continue;
            var ext = Path.GetExtension(file.FileName);
            var file_name = $"{Guid.NewGuid()}{ext}";
            var file_path = Path.Combine(uploads_path, file_name);

            using (var stream = new FileStream(file_path, FileMode.Create)) {
                await file.CopyToAsync(stream);
            }

            var lot_image = new LotImage {
                lot_id = lot.id,
                image_path = $"/uploads/lot_images/{file_name}"
            };

            lot.images.Add(lot_image);
        }

        db.lots.Add(lot);
        if (!await db.TrySaveChangesAsync()) {
            errors.Add("Ошибка сохранения в базу данных.");
            foreach (var img in lot.images) {
                var file_name = Path.GetFileName(img.image_path);
                var file_path = Path.Combine(uploads_path, file_name);
                if (System.IO.File.Exists(file_path)) {
                    System.IO.File.Delete(file_path);
                }
            }
            return (null, errors);
        }

        return (lot, errors);
    }

    public static class Validate
    {
        public static List<string> All(Lot.CreateRequest req, AuctionDbContext db)
        {
            var errors = new List<string>();
            Title(req, ref errors);
            Count(req, ref errors);
            Price(req, ref errors);
            EndTime(req, ref errors);
            TagExists(req, db, ref errors);
            EndTime(req, ref errors);
            Images(req, ref errors);
            City(req, ref errors);
            PaymentMethod(req, ref errors);
            DeliveryPayment(req, ref errors);
            return errors;
        }

        public static void Title(Lot.CreateRequest req, ref List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(req.title))
                errors.Add("Название не может быть пустым.");
        }

        public static void Count(Lot.CreateRequest req, ref List<string> errors)
        {
            if (req.count < 1)
                errors.Add("Количество должно быть не меньше 1.");
        }

        public static void Price(Lot.CreateRequest req, ref List<string> errors)
        {
            if (req.price <= 0)
                errors.Add("Цена должна быть положительной.");
        }

        public static void EndTime(Lot.CreateRequest req, ref List<string> errors)
        {
            if (req.end_time <= DateTimeOffset.Now)
                errors.Add("Дата окончания должна быть в будущем.");
        }

        public static void TagExists(Lot.CreateRequest req, AuctionDbContext db, ref List<string> errors)
        {
            var tag_exists = db.tags.Any(t => t.id == req.tag_id);
            if (!tag_exists)
                errors.Add("Выбранная категория не существует.");
        }

        public static void Images(Lot.CreateRequest req, ref List<string> errors)
        {
            if (req.thumbnail == null && (req.images == null || req.images.Count == 0))
                errors.Add("Необходимо добавить хотя бы одно изображение.");
        }

        public static void City(Lot.CreateRequest req, ref List<string> errors)
        {
            if (string.IsNullOrEmpty(req.city))
                errors.Add("Необходимо указать город.");
        }

        public static void PaymentMethod(Lot.CreateRequest req, ref List<string> errors)
        {
            if (string.IsNullOrEmpty(req.payment_method))
                errors.Add("Необходимо указать способ оплаты.");
            bool ok = false;
            foreach (var p in G.EnumIterate<PaymentMethod>()) {
                if (p.ToString() == req.payment_method) {
                    ok = true;
                    break;
                }
            }
            if (!ok) {
                errors.Add($"Способ {req.payment_method} не поддерживается.");
            }
        }

        public static void DeliveryPayment(Lot.CreateRequest req, ref List<string> errors)
        {
            if (string.IsNullOrEmpty(req.delivery_payment))
                errors.Add("Необходимо указать, кто оплачивает доставку.");
            bool ok = false;
            foreach (var p in G.EnumIterate<DeliveryPayment>()) {
                if (p.ToString() == req.delivery_payment) {
                    ok = true;
                    break;
                }
            }
            if (!ok) {
                errors.Add($"Вариант оплаты {req.delivery_payment} не поддерживается.");
            }
        }
    }

    public async Task<List<UserLotCard>> GetUserLots(string user_login)
    {
        var user = await db.users
            .Where(u => u.login == user_login)
            .Include(u => u.lots)
            .FirstOrDefaultAsync();

        if (user == null) {
            return new();
        }

        var cards = new List<UserLotCard>();
        foreach (var lot in user.lots) {
            var firstImage = await db.lot_images
                .Where(i => i.lot_id == lot.id)
                .OrderBy(i => i.id)
                .FirstOrDefaultAsync();

            cards.Add(new UserLotCard {
                id = lot.id,
                title = lot.title,
                current_price = lot.current_price,
                thumbnail_path = firstImage?.image_path,
            });
        }
        return cards;
    }

    public string? ValidateBid(Lot? lot, decimal new_price, string user_login)
    {
        if (lot == null)
            return "Лот не найден.";

        if (lot.end_time <= DateTimeOffset.Now)
            return "Лот закрыт для ставок.";

        if (lot.user_login == user_login)
            return "Вы не можете делать ставку на свой лот.";

        if (new_price <= lot.current_price)
            return "Ставка должна быть больше текущей цены.";

        return null;
    }

    public async Task<(Lot? lot, string? err)> MakeBid(uint id, decimal new_price, string user_login)
    {
        var lot = await db.lots
            .Include(l => l.user)
            .Include(l => l.bids)
            .FirstOrDefaultAsync(l => l.id == id);

        var error = ValidateBid(lot, new_price, user_login);

        if (lot == null || error != null) {
            return (lot, error);
        }

        var bet = new Bid {
            user_login = user_login,
            lot_id = id,
            price = new_price
        };

        lot.bids.Add(bet);
        lot.current_price = bet.price;
        lot.leader_login = user_login;

        if (!await db.TrySaveChangesAsync())
            return (lot, "Ошибка сохранения ставки.");

        return (await db.lots.Include(l => l.user).Include(l => l.bids.OrderByDescending(b => b.id)).FirstOrDefaultAsync(l => l.id == id), null);
    }

    public async Task<(Lot? lot, string? err)> CancelLastBid(uint lot_id, string user_login)
    {
        var lot = await db.lots
            .Include(l => l.user)
            .Include(l => l.bids.OrderByDescending(b => b.id))
            .FirstOrDefaultAsync(l => l.id == lot_id);

        if (lot == null) {
            return (lot, "Лот не найден.");
        }

        // TODO: this is not good. change
        var error = ValidateBid(lot, lot.current_price+1, user_login);

        if (error != null) {
            return (lot, error);
        }

        var last_2_bid = lot.bids.Take(2).ToList();
        if (last_2_bid.Count == 0) {
            return (lot, "Ставок еще не сделано.");
        }

        var last_bid = last_2_bid.FirstOrDefault();

        if (last_bid?.user_login != user_login) {
            return (lot, "Нелегальная операция.");
        }

        lot.bids.Remove(last_bid);
        lot.RecalculatePrice();

        if (last_2_bid.Count > 1) {
            lot.leader_login = last_2_bid.Last().user_login;
        } else {
            lot.leader_login = null;
        }
        if (!await db.TrySaveChangesAsync())
            return (null, "Ошибка сохранения ставки.");


        return (await db.lots.Include(l => l.user).Include(l => l.bids.OrderByDescending(b => b.id)).FirstOrDefaultAsync(l => l.id == lot_id), null);
    }
}
