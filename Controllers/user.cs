using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using App.Models;
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
    public async Task<IActionResult> ChangePasswordForm(string user_login)
    {
        return View("change_password_form");
    }

    [HttpPatch("password_change")]
    public async Task<IActionResult> ChangePassword(string user_login, [FromForm] string old_password, [FromForm] string new_password)
    {
        var user = HttpContext.GetUser()!;
        if (user.login != user_login) {
            return Unauthorized();
        }

        if (ph.Varify(old_password, user.password_hash)) {
            return View("change_password_form", "Неправильный пароль.");
        }

        var new_hash = ph.Hash(new_password);
        user.password_hash = new_hash;
        db.Update(user);
        await db.SaveChangesAsync();

        return Redirect("/user");
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
            .ThenInclude(l => l.images)
            .ToListAsync();

        var cards = new List<UserBetCard>();
        foreach (var bet in bets)
        {
            var lot = bet.lot!;
            var first_image = lot.images.OrderBy(i => i.id).FirstOrDefault();
            cards.Add(new UserBetCard {
                lot_id = lot.id,
                title = lot.title,
                thumbnail_path = first_image?.image_path,
                end_time = lot.end_time,
                bet_price = bet.price,
                current_price = lot.current_price,
                is_leader = lot.leader_login == user.login
            });
        }
        return View("user_bets", cards);
    }

    [HttpGet("purchases-list")]
    public async Task<IActionResult> GetUserPurchasesList(string user_login)
    {
        var user = HttpContext.GetUser()!;
        if (user.login != user_login) {
            return Unauthorized();
        }
        return View("user_purchases");
    }
}
