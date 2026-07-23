using App.Models;
using App.Helpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
namespace App.Extentions;

public static class MyExtentions
{
    extension(HttpContext c) {
        public User? GetUser() => c.Items["user"] as User;
    }

    public const string HX_REQUEST_HEADER = "HX-Request";
    extension(HttpRequest req) {
        public bool IsHtmx() => req.Headers[HX_REQUEST_HEADER] == "true";
    }

    public const string MAIN_LAYOUT_NAME = "main";
    public static readonly ViewPath layout_path = new ViewPath{ path_prefix = "Views/Layouts/" };

    extension(ViewDataDictionary view_data) {
        public void SetLayout(string layout_name = MAIN_LAYOUT_NAME) => view_data["layout"] = layout_path.GetPath(layout_name);

        public string GetCurrentPath() => (string?)view_data["current_path"] ?? "";
        public void SetCurrentPath(string path) => view_data["current_path"] = path;

        public string GetSavedLocation() => Uri.UnescapeDataString((string?)view_data["saved_location"] ?? "");
        public void SaveLocation(string url) => view_data["saved_location"] = Uri.EscapeDataString(url);

        public void SetUser(User user) => view_data["user"] = user;
        public User? GetUser() => view_data["user"] as User;

        public void SetResults(List<ViewMessage> results) => view_data["results"] = results;
        public void SetResults(ViewMessage result) => view_data["results"] = result;
        public List<ViewMessage> GetResults()
        {
            var obj = view_data["results"];
            if (obj is List<ViewMessage> rs) {
                return rs;
            } else if (obj is ViewMessage r) {
                return new List<ViewMessage>([r]);
            } else {
                return new List<ViewMessage>();
            }
        }
    }
}

public static class EnumExtentions
{
    extension (PaymentMethod p) {
        public string GetDescription()
        {
            return p switch {
            PaymentMethod.None      => "Не указано",
            PaymentMethod.OnMeeting => "При встрече",
            PaymentMethod.ViaBank   => "На карту",
            _ => G.Unreachable<string>(nameof(PaymentMethod)),
            };
        }
    }

    extension (DeliveryPayment d) {
        public string GetDescription()
        {
            return d switch {
            DeliveryPayment.PaidBySeller   => "Продавец",
            DeliveryPayment.PaidByCustomer => "Покупатель",
            _ => G.Unreachable<string>(nameof(DeliveryPayment)),
            };
        }
    }

    extension (LotStatus s) {
        public string GetDescription()
        {
            return s switch {
            LotStatus.Playing => "Разыгрывается",
            LotStatus.WaitingPurchaseConfirm => "Ожидание подтверждения",
            LotStatus.Purchased => "Выкуплено",
            LotStatus.Denied => "Отказано",
            _ => G.Unreachable<string>(nameof(LotStatus)),
            };
        }
    }
}

