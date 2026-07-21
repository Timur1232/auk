using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Abstractions;

using App.Models;
using App.Extentions;

namespace App.Attributes;

public class GetUserOpt(GetUserOpt.Opt opt) : ActionFilterAttribute
{
    public enum Opt {
        None,
        Strict,
        Admin,
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<AuctionDbContext>();
        var user = await db.GetUserByClaims(context.HttpContext.User);

        if (opt == Opt.Strict && user == null) {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (opt == Opt.Admin && (user == null || !user.is_admin)) {
            context.Result = new UnauthorizedResult();
            return;
        }

        context.HttpContext.Items["user"] = user;
        context.ActionArguments["user"] = user;
        if (context.Controller is Controller c) {
            c.ViewData["user"] = user;
        }

        await next();
    }
}

public class GetUser : GetUserOpt
{
    public GetUser() : base(GetUserOpt.Opt.None) {}
}

public class GetUserStrict : GetUserOpt
{
    public GetUserStrict() : base(GetUserOpt.Opt.Strict) {}
}

public class GetUserAdmin : GetUserOpt
{
    public GetUserAdmin() : base(GetUserOpt.Opt.Admin) {}
}

// Supporting only russian for now
public enum Language {
    Ru,
}

// TODO: Introduce language changing
public class Title(string title, Language language = Language.Ru) : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        _ = language;
        if (context.Controller is Controller c) {
            c.ViewData["title"] = $"{Config.APP_NAME} - {title}";
        }
    }
}

public class Htmx(bool requred = true) : ActionMethodSelectorAttribute
{
    public override bool IsValidForRequest(RouteContext route_context, ActionDescriptor action)
    {
        return requred == route_context.HttpContext.Request.IsHtmx();
    }
}

public class HtmxServe : ActionFilterAttribute
{
    public const string HX_REDIRECT_HEADER = "HX-Redirect";

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Controller is Controller c) {
            if (c.Request.IsHtmx()) {
                if (context.Result is RedirectResult r) {
                    c.Response.Headers.Append(HX_REDIRECT_HEADER, r.Url);
                    context.Result = new OkResult();
                }
            }
        }
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is Controller c) {
            if (!c.Request.IsHtmx()) c.ViewData.SetLayout();
            c.ViewBag.htmx = c.Request.IsHtmx();
        }
    }
}

public class SaveLocation : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is Controller c) {
            var query_loc = c.Request.Query["saved_location"].FirstOrDefault();
            if (query_loc != null) {
                c.ViewData.SaveLocation(query_loc);
            }
        }
    }
}

public class AddViewData : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is Controller c) {
            c.ViewData.SetCurrentPath(c.Request.Path.ToString());
            c.ViewData["page"] = c.Request.Query["page"].FirstOrDefault();
        }
    }
}
