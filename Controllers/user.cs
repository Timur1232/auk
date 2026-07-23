using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using App.Models;
using App.Helpers;
using App.Attributes;
using App.Extentions;
using App.Services;
namespace App.Controllers;

[ApiController]
[Route("user")]
[AddViewData, HtmxServe, GetUser]
public class PublicUserController(
        AuctionDbContext db,
        IWebHostEnvironment env
        ) : Controller
{
    private LotsModel lots_model = new(db, env);

    [HttpGet, Authorize, GetUserStrict]
    public IActionResult MyUserInfo()
    {
        var user = HttpContext.GetUser()!;
        return Redirect($"/user/{user.login}");
    }

    [HttpGet("{user_login}")]
    public async Task<IActionResult> UserInfo(string user_login)
    {
        var user = await db.GetUserByLogin(user_login);
        if (user == null) {
            return NotFound();
        }
        var lots = await lots_model.GetUserLots(user.login);
        var data = new UserInfoData {
            user_login = user.login,
            lots = lots
        };
        return View("User/user_info", data);
    }

    [HttpGet("{user_login}/lots-list")]
    public async Task<IActionResult> GetUserLotsList(string user_login)
    {
        var lots = await lots_model.GetUserLots(user_login);
        var data = new UserInfoData {
            user_login = user_login,
            lots = lots,
        };
        return View("User/user_lots", data);
    }
}

[ApiController]
[Route("user/{user_login}")]
[Authorize]
[GetUserStrict, AddViewData, HtmxServe]
public class UserController(
        AuctionDbContext db,
        PasswordHasher ph,
        IWebHostEnvironment env
        ) : Controller
{
    private LotsModel lots_model = new(db, env);

    [HttpGet("password_change")]
    public async Task<IActionResult> ChangePasswordForm()
    {
        return View("change_password_form");
    }

    [HttpPatch("password_change")]
    public async Task<IActionResult> ChangePassword(string user_login, [FromForm] ChangePasswordData req)
    {
        var user = HttpContext.GetUser()!;
        if (user.login != user_login) {
            return Unauthorized();
        }

        if (string.IsNullOrEmpty(req.old_password)) {
            ViewData.SetResults(ViewMessage.Error("Необходимо ввести старый пароль."));
            return View("change_password_form", req);
        }
        if (string.IsNullOrEmpty(req.new_password)) {
            ViewData.SetResults(ViewMessage.Error("Необходимо ввести новый пароль."));
            return View("change_password_form", req);
        }

        if (req.new_password == req.old_password) {
            ViewData.SetResults(ViewMessage.Error("Старый и новый пароли не могут совпадать."));
            return View("change_password_form", req);
        }

        if (!ph.Varify(req.old_password, user.password_hash)) {
            ViewData.SetResults(ViewMessage.Error("Неправильный пароль."));
            return View("change_password_form", req);
        }

        var new_hash = ph.Hash(req.new_password);
        user.password_hash = new_hash;
        db.Update(user);
        if (!await db.TrySaveChangesAsync()) {
            ViewData.SetResults(ViewMessage.Error("Ошибка сохранения. Попробуйте снова."));
            return View("change_password_form", req);
        }

        return View("change_password_result");
    }

    [HttpGet("bets-list")]
    public async Task<IActionResult> GetUserBetsList(string user_login)
    {
        var user = HttpContext.GetUser()!;
        if (user.login != user_login) {
            return Unauthorized();
        }
        var bets = await db.bids
            .Where(b => b.user_login == user.login)
            .OrderByDescending(b => b.id)
            .Include(b => b.lot)
            .ThenInclude(l => l!.images)
            .ToListAsync();

        bets.Sort((b1, b2) => b2.lot!.closed.CompareTo(b1.lot!.closed));

        return View("user_bets", bets);
    }

    [HttpGet("purchases-list")]
    public async Task<IActionResult> GetUserPurchasesList(string user_login)
    {
        var user = HttpContext.GetUser()!;
        if (user.login != user_login) {
            return Unauthorized();
        }

        var purchases = await db.purchases
            .Where(p => p.user_login == user_login)
            .OrderByDescending(p => p.id)
            .Include(p => p.lot)
            .ThenInclude(l => l!.images)
            .ToListAsync();

        return View("user_purchases", purchases);
    }
}
