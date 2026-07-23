using Microsoft.EntityFrameworkCore;
namespace App.Models;

public class TagsModel(AuctionDbContext db)
{
    public enum Error {
        None,
        NameEmpty,
        NameExists,
        NotExists,
        DbError,
    }

    public static string ErrorToString(Error err)
    {
        return err switch {
            Error.None       => "",
            Error.NameEmpty  => "Название категории не может быть пустым.",
            Error.NameExists => "Категория с таким названием уже существует.",
            Error.NotExists  => "Записи не существует.",
            Error.DbError    => "Ошибка сохранения категории.",
            _ => G.Unreachable<string>(nameof(ErrorToString)),
        };
    }

    public async Task<List<Tag>> GetAll(string? search)
    {
        var q = db.tags.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) {
            q = q.Where(t => t.name.Contains(search));
        }
        return await q.OrderBy(t => t.id).ToListAsync();
    }

    public async Task<(Tag? tag, List<Error> errors)> CreateTag(Tag.CreateRequest req)
    {
        var errors = new List<Error>();
        if (string.IsNullOrWhiteSpace(req.name)) {
            errors.Add(Error.NameEmpty);
        }

        var exists = await db.tags.AnyAsync(t => t.name == req.name);
        if (exists) {
            errors.Add(Error.NameExists);
        }

        if (errors.Count > 0) {
            return (null, errors);
        }

        var tag = Tag.From(req);
        db.tags.Add(tag);
        if (!await db.TrySaveChangesAsync()) {
            errors.Add(Error.DbError);
        }

        return (tag, errors);
    }

    public async Task<(Tag?, Error)> DeleteById(uint id)
    {
        var tag = await db.tags.FindAsync(id);
        if (tag == null) {
            return (null, Error.NotExists);
        }
        db.tags.Remove(tag);
        if (!await db.TrySaveChangesAsync()) {
            return (null, Error.DbError);
        }
        return (tag, Error.None);
    }

    public async Task<(Tag? updatedTag, List<Error> errors)> UpdateById(uint id, Tag.UpdateRequest req)
    {
        var errors = new List<Error>();
        var tag = await db.tags.FindAsync(id);
        if (tag == null) {
            errors.Add(Error.NotExists);
            return (null, errors);
        }

        if (string.IsNullOrWhiteSpace(req.name)) {
            errors.Add(Error.NameEmpty);
        } else {
            var exists = await db.tags.AnyAsync(t => t.name == req.name && t.id != id);
            if (exists) {
                errors.Add(Error.NameExists);
            }
        }

        if (errors.Count > 0) return (null, errors);

        tag.name = req.name;
        db.tags.Update(tag);
        if (!await db.TrySaveChangesAsync()) {
            errors.Add(Error.DbError);
        }
        return (tag, errors);
    }
}
