using App.Services;
namespace App.Models;

// TODO: Add checks for empty fields.
// The thing is that if client send an empty field which is not marked as nullable inside `RegisterRequest` or `LoginRequest` record, then the framework returns 415 automaticly. And this is a problem, because we can't show proper error message.
public class AuthModel(AuctionDbContext db, PasswordHasher password_hasher)
{
    public enum Error {
        None,
        UserNotExists,
        IncorrectPassword,
        LoginExists,
        LoginEmpty,
        EmailExists,
        PasswordConfirmNotMatch,
        DbError,
    }

    public static string ErrorToString(Error err)
    {
        return err switch {
            Error.None                    => "Нет ошибки.",
            Error.UserNotExists           => "Пользователя не существует.",
            Error.IncorrectPassword       => "Неправильный пароль.",
            Error.LoginExists             => "Логин существует.",
            Error.LoginEmpty              => "Логин не может быть пустым.",
            Error.EmailExists             => "Почта существует.",
            Error.PasswordConfirmNotMatch => "Подтверждение пароля не совпадает.",
            Error.DbError                 => "Пароли не совпадают.",
            _ => G.Unreachable<string>(nameof(ErrorToString)),
        };
    }

    public async Task<bool> IsUserExists(string? login_or_email)
    {
        if (login_or_email == null || login_or_email == "") return false;
        var user = await db.GetUserByLoginOrEmail(login_or_email);
        return user != null;
    }

    public async Task<(User? user, Error err)> ValidateLoginForm(User.LoginRequest login_req)
    {
        var user = await db.GetUserByLoginOrEmail(login_req.login_or_email);
        if (user == null) {
            return (null, Error.UserNotExists);
        }

        if (!password_hasher.Varify(login_req.password, user.password_hash)) {
            return (null, Error.IncorrectPassword);
        }

        return (user, Error.None);
    }

    public async Task<List<Error>> ValidateRegisterForm(User.RegisterRequest register_req)
    {
        var errors = new List<Error>();
        if (register_req.login == null) {
            errors.Add(Error.LoginEmpty);
            return errors;
        }
        if (await db.GetUserByLogin(register_req.login) != null) {
            errors.Add(Error.LoginExists);
        }
        if (register_req.email != null && await db.GetUserByEmail(register_req.email) != null) {
            errors.Add(Error.EmailExists);
        }
        if (register_req.password != register_req.password_confirm) {
            errors.Add(Error.PasswordConfirmNotMatch);
        }
        return errors;
    }

    public async Task<(User? new_user, List<Error> errors)> RegisterNewUser(User.RegisterRequest register_req)
    {
        var errors = await ValidateRegisterForm(register_req);

        if (errors.Count > 0) {
            return (null, errors);
        }

        var password_hash = password_hasher.Hash(register_req.password);
        var new_user = new User{
            login = register_req.login,
            email = register_req.email,
            password_hash = password_hash,
        };
        db.users.Add(new_user);

        if (!await db.TrySaveChangesAsync()) {
            errors.Add(Error.DbError);
            return (null, errors);
        }

        return (new_user, new());
    }
}
