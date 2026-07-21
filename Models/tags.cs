using Microsoft.EntityFrameworkCore;
namespace App.Models;

public class TagsModel(AuctionDbContext db)
{
    public async Task<List<Tag>> GetAll(string? search)
    {
        var q = db.tags.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) {
            q = q.Where(t => t.name.Contains(search));
        }
        return await q.OrderBy(t => t.id).ToListAsync();
    }

    public async Task<(Tag? tag, List<string> errors)> CreateTag(Tag.CreateRequest req)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(req.name)) {
            errors.Add("Название категории не может быть пустым.");
        }

        var exists = await db.tags.AnyAsync(t => t.name == req.name);
        if (exists) {
            errors.Add("Категория с таким названием уже существует.");
        }

        if (errors.Count > 0) {
            return (null, errors);
        }

        var tag = Tag.From(req);
        db.tags.Add(tag);
        if (!await db.TrySaveChangesAsync()) {
            errors.Add("Ошибка сохранения категории.");
        }

        return (tag, errors);
    }

    public async Task<(Tag?, ModelError)> DeleteById(uint id)
    {
        var tag = await db.tags.FindAsync(id);
        if (tag == null) {
            return (null, ModelError.NotExist);
        }
        db.tags.Remove(tag);
        if (!await db.TrySaveChangesAsync()) {
            return (null, ModelError.DbError);
        }
        return (tag, ModelError.None);
    }

    public async Task<(Tag? updatedTag, List<string> errors)> UpdateById(uint id, Tag.UpdateRequest req)
    {
        var errors = new List<string>();
        var tag = await db.tags.FindAsync(id);
        if (tag == null) {
            errors.Add("Категория не найдена."); return (null, errors);
        }

        if (string.IsNullOrWhiteSpace(req.name)) {
            errors.Add("Название не может быть пустым.");
        } else {
            var exists = await db.tags.AnyAsync(t => t.name == req.name && t.id != id);
            if (exists) {
                errors.Add("Категория с таким названием уже существует.");
            }
        }

        if (errors.Count > 0) return (null, errors);

        tag.name = req.name;
        db.tags.Update(tag);
        if (!await db.TrySaveChangesAsync()) {
            errors.Add("Ошибка обновления.");
        }
        return (errors.Count == 0 ? tag : null, errors);
    }
}
