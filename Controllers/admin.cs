using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using App.Attributes;
using App.Extentions;
namespace App.Controllers;

[ApiController, Route("admin")]
[Authorize, GetUserAdmin]
[HtmxServe, AddViewData]
[Title("Администрирование")]
public class AdminController(AuctionDbContext db) : Controller
{
    private TagsModel tags_model = new(db);

    [HttpGet]
    public async Task<IActionResult> GetPage([FromQuery] string? search)
    {
        var tags = await tags_model.GetAll(search);
        var model = new AdminPageModel { Tags = tags, Search = search };
        if (Request.IsHtmx()) {
            return View("tag_list", model);
        }
        return View("index", model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag([FromForm] Tag.CreateRequest req, [FromQuery] string? search)
    {
        var (tag, errors) = await tags_model.CreateTag(req);

        var model = new AdminPageModel { Search = search };

        if (errors.Count > 0 || tag == null) {
            ViewData["errors"] = errors;
            return View("tag_form", model);
        }

        ViewData["result_messages"] = "Категория успешно создана!";
        var tags = await tags_model.GetAll(search);
        model.Tags = tags;
        model.Success = true;
        return View("tag_form", model);

    }

    [HttpDelete("{tag_id}")]
    public async Task<IActionResult> DeleteTag([FromRoute] uint tag_id)
    {
        var (tag, err) = await tags_model.DeleteById(tag_id);
        if (err != ModelError.None || tag == null) {
            Response.StatusCode = 400;
            G.Log(LogLevel.Error, err.GetMessage());
            return Content(err.GetMessage());
        }
        return Ok();
    }

    [HttpPatch("{tag_id}")]
    public async Task<IActionResult> UpdateTag([FromRoute] uint tag_id, [FromForm] Tag.UpdateRequest req, [FromQuery] string? search)
    {
        var (updated_tag, errors) = await tags_model.UpdateById(tag_id, req);
        if (errors.Count > 0 || updated_tag == null) {
            ViewData["errors"] = errors;
            return View("tag_edit_form", new AdminPageModel { Success = false, Search = search });
        }
        return View("tag_item", updated_tag);
    }

    [HttpGet("{tag_id}/edit-form")]
    public async Task<IActionResult> EditForm([FromRoute] uint tag_id)
    {
        var tag = await db.tags.FindAsync(tag_id);
        if (tag == null) return NotFound();
        var form_data = new TagEditForm {
            Name = tag.name,
            TagId = tag.id,
        };
        return View("tag_edit_form", form_data);
    }
}
