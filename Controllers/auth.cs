using Microsoft.AspNetCore.Mvc;
using App.Attributes;
using App.Models;
using App.Helpers;
using App.Services;
using App.Extentions;

namespace App.Controllers;

[ApiController]
[Route("auth")]
[Title("Авторизация")]
[HtmxServe, AddViewData]
[SaveLocation]
public class AuthController(
    PasswordHasher password_hasher,
    JwtTokenService token_service,
    AuctionDbContext db)
    : Controller
{
    AuthModel model = new AuthModel(db, password_hasher);

    [HttpGet("login")]
    public async Task<IActionResult> Login() {
        return View("login");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] User.LoginRequest login_req)
    {
        var (user, err) = await model.ValidateLoginForm(login_req);

        if (err != AuthModel.Error.None || user == null) {
            ViewData.SetResults(ViewMessage.Error(AuthModel.ErrorToString(err)));
            if (Request.IsHtmx()) return View("results");
            return View("login");
        }

        token_service.AppendTokenCookie(Response.Cookies, user);

        var url = "/";
        if (login_req.last_saved_location != null) {
            url = Uri.UnescapeDataString(login_req.last_saved_location);
        }
        return Redirect(url);
    }

    [HttpGet("register")]
    public async Task<IActionResult> Register() => View("register");

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromForm] User.RegisterRequest register_req)
    {
        var (new_user, errors) = await model.RegisterNewUser(register_req);

        if (errors.Count > 0 || new_user == null) {
            ViewData.SetResults(ViewMessage.MapCollectionError(errors, AuthModel.ErrorToString));
            if (Request.IsHtmx()) return View("results");
            return View("register");
        }

        token_service.AppendTokenCookie(Response.Cookies, new_user);

        var url = "/";
        if (register_req.last_saved_location != null) {
            url = Uri.UnescapeDataString(register_req.last_saved_location);
        }
        return Redirect(url);
    }

    [Htmx]
    [HttpPost("login/user_exists")]
    public async Task<IActionResult> LoginCheckUserExists([FromForm] string? login_or_email)
    {
        if (!await model.IsUserExists(login_or_email)) {
            ViewData.SetResults(ViewMessage.Error(AuthModel.ErrorToString(AuthModel.Error.UserNotExists)));
            return View("results");
        }
        return Ok();
    }

    [Htmx]
    [HttpPost("register/user_exists")]
    public async Task<IActionResult> RegisterCheckUserExists([FromForm] string? login, [FromForm] string? email)
    {
        var errors = await model.ValidateRegisterForm(new User.RegisterRequest(
            login ?? "",
            email
        ));

        if (errors.Count > 0) {
            ViewData.SetResults(ViewMessage.MapCollectionError(errors, AuthModel.ErrorToString));
            return View("erri");
        }

        return Ok();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies["jwt_token"];
        var refresh = Request.Cookies["jwt_refresh_token"];

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refresh)) {
            return Unauthorized("No jwt_token or jwt_refresh_token");
        }

        var user_login = await token_service.ValidateRefreshToken(token, refresh);
        if (user_login == null) {
            return Unauthorized("");
        }

        token_service.RemoveRefreshToken(refresh);

        var user = await db.GetUserByLogin(user_login);
        if (user == null) {
            return NotFound();
        }
        token_service.AppendTokenCookie(Response.Cookies, user);

        return Ok();
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["jwt_token"];
        var refresh = Request.Cookies["jwt_refresh_token"];

        Response.Cookies.Delete("jwt_token");
        Response.Cookies.Delete("jwt_refresh_token");

        if (!string.IsNullOrEmpty(refresh)) {
            token_service.RemoveRefreshToken(refresh);
        }

        return Redirect(ViewData.GetSavedLocation());
    }
}
