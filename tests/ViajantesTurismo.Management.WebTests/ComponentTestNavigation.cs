using Microsoft.AspNetCore.Components;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>Provides the navigation manager that the component uses in its test context.</summary>
internal static class ComponentTestNavigation
{
    /// <summary>Gets the context's navigation manager for navigation and URI assertions.</summary>
    public static NavigationManager GetNavigationManager(this BunitContext context) =>
        context.Services.GetRequiredService<NavigationManager>();
}
