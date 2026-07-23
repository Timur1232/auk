using Microsoft.EntityFrameworkCore;
using App.Helpers;
using App.Services;
using App.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;


CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

if (builder.Environment.IsDevelopment()) {
    Log.Info( "App in development mode");
} else {
    Log.Info( "App in production mode");
}

builder.Services.AddControllers();
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AuctionDbContext>(opt => {
    var conn_string = config.GetConnectionString("db_conn");
    opt.UseNpgsql(conn_string);
});

builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddHostedService<AuctionClosingService>();
builder.Services.AddHostedService<OrphanImagesKiller>();
builder.Services.AddScoped<PasswordHasher>();

builder.Services.AddAuthorizationBuilder()
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => {
        o.RequireHttpsMetadata = false;
        o.TokenValidationParameters = JwtTokenService.MakeTokenValidationParameters(config);
        o.Events = new JwtBearerEvents {
            OnMessageReceived = c => {
                var token_header = c.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(token_header)) {
                    c.Token = c.Request.Cookies["jwt_token"];
                } else {
                    c.Token = token_header;
                }
                return Task.CompletedTask;
            },
            OnChallenge = c => {
                var req_path = Uri.EscapeDataString(c.Request.Path.ToString());
                c.Response.Redirect($"/auth/login?saved_location={req_path}");
                c.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
    var pending_migrations = await db.Database.GetPendingMigrationsAsync();
    if (pending_migrations.Any()) {
        Log.Info("Migrating database");
        await db.Database.MigrateAsync();
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.UseStaticFiles();

app.MapControllers();

app.Run();
