using System.Text.RegularExpressions;

public static class Validator
{
    public static bool ValidateLogin(string login, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(login)) { error = "Логин не может быть пустым!"; return false; }
        if (login.Length < 3 || login.Length > 20) { error = "Логин от 3 до 20 символов!"; return false; }
        // Regex (требование 3.e)
        if (!Regex.IsMatch(login, @"^[a-zA-Z0-9_]+$")) { error = "Только буквы, цифры и _"; return false; }
        return true;
    }

    public static bool ValidatePassword(string password, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(password)) { error = "Пароль не может быть пустым!"; return false; }
        if (password.Length < 6) { error = "Пароль мин. 6 символов!"; return false; }
        return true;
    }
}