using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

public static partial class NavigationTestRegexes
{
    [GeneratedRegex("active")]
    public static partial Regex Active();

    [GeneratedRegex("/tours$")]
    public static partial Regex Tours();

    [GeneratedRegex("/$")]
    public static partial Regex Home();

    [GeneratedRegex("/addtour$")]
    public static partial Regex AddTour();

    [GeneratedRegex(@"/tours/[\da-f-]+")]
    public static partial Regex Tour();

    [GeneratedRegex("/bookings$")]
    public static partial Regex Bookings();

    [GeneratedRegex("/customers$")]
    public static partial Regex Customers();

    [GeneratedRegex("/customers/create")]
    public static partial Regex CreateCustomer();

    [GeneratedRegex("/customers/create/personal-info$")]
    public static partial Regex PersonalInfo();
}
