using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Extensions;

public static class LocalizationExtensions
{
    public static bool IsEnglish(this Controller controller)
    {
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en";
    }

    public static string L(this Controller controller, string vietnamese, string english)
    {
        return controller.IsEnglish() ? english : vietnamese;
    }
}
