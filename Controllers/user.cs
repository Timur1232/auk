using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using App.Attributes;
using App.Extentions;
using App.Services;
namespace App.Controllers;

[ApiController]
[Route("user")]
[Authorize]
[GetUserStrict, AddViewData, HtmxServe]
public class UserController(
        AuctionDbContext db,
        PasswordHasher ph
        ) : Controller
{
    [HttpGet]
    public async Task<IActionResult> UserInfo()
    {
        var user = HttpContext.GetUser()!;
        return View("user_info", user.ToDto());
    }

    [HttpGet("lots")]
    public async Task<IActionResult> UserLots()
    {
        var user = HttpContext.GetUser()!;
        return Ok(db.lots.Where(l => l.user_login == user.login));
    }

    [HttpGet("password_change")]
    public async Task<IActionResult> ChangePasswordForm()
    {
        return View("change_password_form");
    }

    [HttpPatch("password_change")]
    public async Task<IActionResult> ChangePassword([FromForm] string old_password, [FromForm] string new_password)
    {
        var user = HttpContext.GetUser()!;
        if (ph.Varify(old_password, user.password_hash)) {
            return View("change_password_form", "Неправильный пароль.");
        }

        var new_hash = ph.Hash(new_password);
        user.password_hash = new_hash;
        db.Update(user);
        await db.SaveChangesAsync();

        return Redirect("/user");
    }
}
