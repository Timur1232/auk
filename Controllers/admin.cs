using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using App.Helpers;
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
        var data = new AdminPageData { tags = tags, search = search };
        if (Request.IsHtmx()) {
            return View("tag_list", data);
        }
        return View("index", data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTag([FromForm] Tag.CreateRequest req, [FromQuery] string? search)
    {
        var (tag, errors) = await tags_model.CreateTag(req);

        var data = new AdminPageData { search = search };

        if (errors.Count > 0 || tag == null) {
            ViewData.SetResults(ViewMessage.MapCollectionError(errors, TagsModel.ErrorToString));
            return View("tag_form", data);
        }

        ViewData["result_messages"] = "Категория успешно создана!";
        var tags = await tags_model.GetAll(search);
        data.tags = tags;
        data.success = true;
        return View("tag_form", data);

    }

    [HttpDelete("{tag_id}")]
    public async Task<IActionResult> DeleteTag([FromRoute] uint tag_id)
    {
        var (tag, err) = await tags_model.DeleteById(tag_id);
        if (err != TagsModel.Error.None || tag == null) {
            Response.StatusCode = 400;
            var err_msg = TagsModel.ErrorToString(err);
            Log.Error(err_msg);
            ViewData.SetResults(ViewMessage.Error(err_msg));
            return View("results");
        }
        return Ok();
    }

    [HttpPatch("{tag_id}")]
    public async Task<IActionResult> UpdateTag([FromRoute] uint tag_id, [FromForm] Tag.UpdateRequest req, [FromQuery] string? search)
    {
        var (updated_tag, errors) = await tags_model.UpdateById(tag_id, req);
        if (errors.Count > 0 || updated_tag == null) {
            ViewData.SetResults(ViewMessage.MapCollectionError(errors, TagsModel.ErrorToString));
            return View("tag_edit_form", new AdminPageData { success = false, search = search });
        }
        return View("tag_item", updated_tag);
    }

    [HttpGet("{tag_id}/edit-form")]
    public async Task<IActionResult> EditForm([FromRoute] uint tag_id)
    {
        var tag = await db.tags.FindAsync(tag_id);
        if (tag == null) return NotFound();
        var form_data = new TagEditFormData {
            name = tag.name,
            tag_id = tag.id,
        };
        return View("tag_edit_form", form_data);
    }
}
