using System.Text.Json.Nodes;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Arrangement helpers for the payment page authentication-change tests.</summary>
internal static class PaymentPageAuthentication
{
    /// <summary>Serves one payment instruction, created by another administrator, that is open for independent review.</summary>
    public static void ServeReviewableInstruction(PaymentPageHttpHandler handler)
    {
        handler.Accounts = PaymentPageData.Accounts;
        handler.FinancialHistory = PaymentPageData.History;
        var instruction = JsonNode.Parse(PaymentPageData.Instruction).ShouldNotBeNull();
        instruction["document"].ShouldNotBeNull()["status"] = 4;
        handler.Instructions = "[" + instruction.ToJsonString() + "]";
    }

    /// <summary>Returns the page buttons that carry exactly the given text.</summary>
    public static IReadOnlyList<AngleSharp.Dom.IElement> ReviewButtons<TComponent>(IRenderedComponent<TComponent> page, string text)
        where TComponent : Microsoft.AspNetCore.Components.IComponent =>
        [.. page.FindAll("button").Where(button => button.TextContent == text)];
}
