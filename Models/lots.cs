using Microsoft.EntityFrameworkCore;
namespace App.Models;

public class Aboba : ICloneable
{
    public Lot lot = null!;

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

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

    public IQueryable<Lot> GetFilteredQuery(HomePageQuery query)
    {
        var q = db.lots.AsQueryable();

        if (query.tag_id.HasValue) {
            q = q.Where(l => l.tag_id == query.tag_id.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.search)) {
            var searchLower = query.search.ToLower();
            q = q.Where(l =>
                l.title.ToLower().Contains(searchLower) ||
                (l.city != null && l.city.ToLower().Contains(searchLower)) ||
                l.user_login.ToLower().Contains(searchLower)
            );
        }

        if (!query.show_closed) {
            q = q.Where(l => !l.closed);
        }

        q = query.sort_by switch {
            "price" => query.sort_dir == "asc" ? q.OrderBy(l => l.current_price) : q.OrderByDescending(l => l.current_price),
            "title" => query.sort_dir == "asc" ? q.OrderBy(l => l.title) : q.OrderByDescending(l => l.title),
            _ => query.sort_dir == "asc" ? q.OrderBy(l => l.end_time.ToString()) : q.OrderByDescending(l => l.end_time.ToString()),
        };

        return q;
    }

    public async Task<List<HomePageModel.LotCard>> GetLotCardsPage(HomePageQuery query)
    {
        var lots = await GetFilteredQuery(query)
            .Skip(query.page * query.page_size)
            .Take(query.page_size)
            .Include(l => l.images)
            .ToListAsync();

        var cards = new List<HomePageModel.LotCard>();
        foreach (var lot in lots) {
            var image = lot.images.FirstOrDefault();
            cards.Add(new HomePageModel.LotCard {
                id = lot.id,
                image_path = image?.image_path,
                title = lot.title,
                price = lot.current_price,
                end_time = lot.end_time,
                closed = lot.closed,
            });
        }

        return cards;
    }

    public async Task<HomePageModel> HomePage(HomePageQuery query) {
        var total = await GetFilteredQuery(query).CountAsync();
        int pages = total == 0 ? 1 : (int)Math.Ceiling((double)total / query.page_size);

        if (query.page >= pages) query = query with { page = pages - 1 };
        if (query.page < 0) query = query with { page = 0 };

        var tags = await db.tags.ToListAsync();
        var cards = await GetLotCardsPage(query);

        return new HomePageModel {
            cards = cards,
            tags = tags,
            query = query,
            total_count = total,
            pages_count = pages
        };
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

    public async Task<(Lot? deleted_lot, ModelError err)> DeleteById(uint id)
    {
        var lot = await GetById(id);
        if (lot == null) {
            return (null, ModelError.NotExist);
        }

        var images = lot.images;
        var uploads_path = Path.Combine(env.WebRootPath, "uploads", "lot_images");
        await DeleteLotImagesFiles(images, uploads_path);

        db.lots.Remove(lot);
        if (!await db.TrySaveChangesAsync()) {
            return (null, ModelError.DbError);
        }

        return (lot, ModelError.None);
    }

    public async Task<(Lot? updated_lot, List<string> errors)> UpdateById(uint id, Lot.EditRequest req)
    {
        var errors = Validate.AllEdit(req, db);
        if (errors.Count > 0) return (null, errors);

        var lot = await db.lots.Include(l => l.images).FirstOrDefaultAsync(l => l.id == id);
        if (lot == null) {
            errors.Add("Лота не существует.");
            return (null, errors);
        }

        lot.Update(req);

        var uploads_path = Path.Combine(env.WebRootPath, "uploads", "lot_images");
        Directory.CreateDirectory(uploads_path);

        var images = new List<LotImage>(lot.images);

        db.lot_images.RemoveRange(lot.images);
        if (!await db.TrySaveChangesAsync()) {
            errors.Add("Ошибка сохранения в базе данных.");
            return (null, errors);
        }

        await DeleteLotImagesFiles(images, uploads_path);
        await SaveLotImagesFiles(lot, req.thumbnail, req.images, uploads_path);

        if (!await db.TrySaveChangesAsync()) {
            errors.Add("Ошибка сохранения в базе данных.");
            return (null, errors);
        }

        return (lot, errors);
    }

    public static async Task SaveLotImagesFiles(Lot lot, IFormFile? thumbnail, IFormFileCollection? images, string uploads_path)
    {
        var images_to_save = new List<IFormFile>();
        if (thumbnail != null) images_to_save.Add(thumbnail);
        if (images != null)    images_to_save.AddRange(images);

        foreach (var file in images_to_save) {
            if (file.Length == 0) continue;
            var ext = Path.GetExtension(file.FileName);
            var file_name = $"{Guid.NewGuid()}{ext}";
            var file_path = Path.Combine(uploads_path, file_name);

            using (var stream = new FileStream(file_path, FileMode.Create)) {
                await file.CopyToAsync(stream);
            }

            var lot_image = new LotImage {
                image_path = $"/uploads/lot_images/{file_name}"
            };

            lot.images.Add(lot_image);
        }
    }

    public static async Task DeleteLotImagesFiles(ICollection<LotImage> images, string uploads_path)
    {
        foreach (var img in images) {
            var file_name = Path.GetFileName(img.image_path);
            var file_path = Path.Combine(uploads_path, file_name);
            if (System.IO.File.Exists(file_path)) {
                System.IO.File.Delete(file_path);
            }
        }
    }

    public async Task<(Lot? lot, List<string> errors)> CreateLot(Lot.CreateRequest req, string user_login)
    {
        var errors = Validate.AllCreate(req, db);
        if (errors.Count > 0) return (null, errors);

        var lot = Lot.From(req, user_login);

        var uploads_path = Path.Combine(env.WebRootPath, "uploads", "lot_images");
        Directory.CreateDirectory(uploads_path);

        await SaveLotImagesFiles(lot, req.thumbnail, req.images, uploads_path);

        for (int i = 0; i < 30; i++) {
            db.lots.Add(lot);
        }
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
        public static List<string> AllCreate(Lot.CreateRequest req, AuctionDbContext db)
        {
            var errors = new List<string>();
            Title(req.title, ref errors);
            Count(req.count, ref errors);
            Price(req.price, ref errors);
            EndTime(req.end_time, ref errors);
            TagExists(req.tag_id, db, ref errors);
            Images(req.thumbnail, req.images, ref errors);
            City(req.city, ref errors);
            PaymentMethod(req.payment_method, ref errors);
            DeliveryPayment(req.delivery_payment, ref errors);
            return errors;
        }

        public static List<string> AllEdit(Lot.EditRequest req, AuctionDbContext db)
        {
            var errors = new List<string>();
            Title(req.title, ref errors);
            Count(req.count, ref errors);
            EndTime(req.end_time, ref errors);
            TagExists(req.tag_id, db, ref errors);
            Images(req.thumbnail, req.images, ref errors);
            City(req.city, ref errors);
            PaymentMethod(req.payment_method, ref errors);
            DeliveryPayment(req.delivery_payment, ref errors);
            return errors;
        }

        public static void Title(string? title, ref List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(title))
                errors.Add("Название не может быть пустым.");
        }

        public static void Count(int? count, ref List<string> errors)
        {
            if (count == null || count < 1)
                errors.Add("Количество должно быть не меньше 1.");
        }

        public static void Price(decimal? price, ref List<string> errors)
        {
            if (price == null || price <= 0)
                errors.Add("Цена должна быть положительной.");
        }

        public static void EndTime(DateTimeOffset? end_time, ref List<string> errors)
        {
            if (end_time == null || end_time <= DateTimeOffset.Now)
                errors.Add("Дата окончания должна быть в будущем.");
        }

        public static void TagExists(uint? tag_id, AuctionDbContext db, ref List<string> errors)
        {
            const string msg = "Выбранная категория не существует.";

            if (tag_id == null) {
                errors.Add(msg);
                return;
            }

            var tag_exists = db.tags.Any(t => t.id == tag_id);
            if (!tag_exists) {
                errors.Add(msg);
                return;
            }
        }

        public static void Images(IFormFile? thumbnail, IFormFileCollection? images, ref List<string> errors)
        {
            if (thumbnail == null && (images == null || images.Count == 0))
                errors.Add("Необходимо добавить хотя бы одно изображение.");
        }

        public static void City(string? city, ref List<string> errors)
        {
            if (string.IsNullOrEmpty(city))
                errors.Add("Необходимо указать город.");
        }

        public static void PaymentMethod(string? payment_method, ref List<string> errors)
        {
            if (string.IsNullOrEmpty(payment_method)) {
                errors.Add("Необходимо указать способ оплаты.");
                return;
            }
            bool ok = false;
            foreach (var p in G.EnumIterate<PaymentMethod>()) {
                if (p.ToString() == payment_method) {
                    ok = true;
                    break;
                }
            }
            if (!ok) {
                errors.Add($"Способ оплаты не поддерживается.");
            }
        }

        public static void DeliveryPayment(string? delivery_payment, ref List<string> errors)
        {
            if (string.IsNullOrEmpty(delivery_payment)) {
                errors.Add("Необходимо указать, кто оплачивает доставку.");
                return;
            }
            bool ok = false;
            foreach (var p in G.EnumIterate<DeliveryPayment>()) {
                if (p.ToString() == delivery_payment) {
                    ok = true;
                    break;
                }
            }
            if (!ok) {
                errors.Add($"Вариант оплаты {delivery_payment} не поддерживается.");
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
