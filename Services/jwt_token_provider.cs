using System.Security.Claims;
using System.Text;
using App.Models;
using App.Helpers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace App.Services;

public class JwtTokenService(IConfiguration configuration) : BackgroundService
{
    private JsonWebTokenHandler handler = new JsonWebTokenHandler();
    private static List<string> refresh_tokens = new();

    protected override async Task ExecuteAsync(CancellationToken stopping_token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (!stopping_token.IsCancellationRequested) {
            if (await timer.WaitForNextTickAsync(stopping_token)) {
                await UpdateRefreshTokens();
            }
        }
    }

    public async Task UpdateRefreshTokens()
    {
        var indecies_to_remove = new List<int>();
        foreach (var (i, t) in refresh_tokens.Index()) {
            var res = await handler.ValidateTokenAsync(t, MakeTokenValidationParameters(configuration));
            if (!res.IsValid) {
                indecies_to_remove.Add(i);
            }
        }
        for (var i = indecies_to_remove.Count-1; i >= 0; i -= 1) {
            refresh_tokens.RemoveAt(i);
        }
        Log.Info("Refresh tokens updated");
    }

    public static SecurityTokenDescriptor MakeTokenDescriptor(IConfiguration configuration, User user, DateTime expires)
    {
        string secret_key = configuration["Jwt:Secret"]!;
        var security_key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret_key));

        var credentials = new SigningCredentials(security_key, SecurityAlgorithms.HmacSha256);

        var token_descriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, user.login),
                new Claim(JwtRegisteredClaimNames.Email, user.email ?? ""),
                new Claim("user_login", user.login),
            ]),
            Expires = expires,
            SigningCredentials = credentials,
            Issuer = configuration["Jwt:Issuer"],
        };

        return token_descriptor;
    }

    public static TokenValidationParameters MakeTokenValidationParameters(IConfiguration configuration)
    {
        return new TokenValidationParameters {
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
            ValidIssuer = configuration["Jwt:Issuer"],
            // ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public (string token, string refresh) GenerateJwtToken(User user)
    {
        var token_descriptor = MakeTokenDescriptor(configuration, user, DateTime.UtcNow.AddHours(2));
        var token = handler.CreateToken(token_descriptor);

        token_descriptor.Expires = DateTime.UtcNow.AddHours(2);
        var refresh = GenerateRefreshToken(token_descriptor);

        return (token, refresh);
    }

    public string GenerateRefreshToken(SecurityTokenDescriptor token_descriptor)
    {
        var refresh = handler.CreateToken(token_descriptor);
        refresh_tokens.Add(refresh);
        Log.Info($"Refresh token added: count = {refresh_tokens.Count}");
        return refresh;
    }

    public void AppendTokenCookie(IResponseCookies cookies, User user)
    {
        (string token, string refresh) = GenerateJwtToken(user);

        var options = new CookieOptions{
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddMinutes(60),
        };
        cookies.Append("jwt_token", token, options);

        options.Expires = DateTimeOffset.UtcNow.AddHours(2);
        cookies.Append("jwt_refresh_token", refresh, options);
    }

    public async Task<string?> ValidateRefreshToken(string token, string refresh) {
        var validate_params = MakeTokenValidationParameters(configuration);

        var stored_refresh = refresh_tokens.Find(r => r == refresh);
        if (stored_refresh == null) {
            Log.Warning("Refresh token not found in store");
            foreach (var t in refresh_tokens) {
                Console.WriteLine(t);
            }
            return null;
        }

        var refresh_validated = await handler.ValidateTokenAsync(stored_refresh, validate_params);
        if (!refresh_validated.IsValid) {
            Log.Warning("Refresh token is invalid");
            return null;
        }

        var refresh_login = (string?)refresh_validated.Claims["user_login"];
        if (refresh_login == null) {
            Log.Warning("Not user_login in refresh token");
            return null;
        }

        var token_login = handler.ReadJsonWebToken(token).Claims.FirstOrDefault(c => c.Type == "user_login");
        if (token_login == null) {
            Log.Warning("No user_login in jwt token");
            return null;
        }

        if (token_login.Value != refresh_login) {
            Log.Warning($"User logins not match: from jwt_token = {token_login.Value}, from refresh token = {refresh_login}");
            return null;
        }

        return token_login.Value;
    }

    public void RemoveRefreshToken(string refresh)
    {
        refresh_tokens.Remove(refresh);
        Log.Info($"Refresh token removed: count = {refresh_tokens.Count}");
    }
}
