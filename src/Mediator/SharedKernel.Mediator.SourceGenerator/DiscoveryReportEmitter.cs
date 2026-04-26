using System.Text;

namespace SharedKernel.Mediator.SourceGenerator;

/// <summary>
/// Emits the readable discovery report defined for the first generator marker.
/// </summary>
internal static class DiscoveryReportEmitter
{
    public static string Emit(DiscoveryCounts counts)
    {
        var builder = new StringBuilder();
        builder.AppendLine("namespace SharedKernel.Mediator.Generated;");
        builder.AppendLine();
        builder.AppendLine("internal static class MediatorDiscoveryReport");
        builder.AppendLine("{");
        builder.Append("    public const int RequestCount = ").Append(counts.RequestCount).AppendLine(";");
        builder.Append("    public const int HandlerCount = ").Append(counts.HandlerCount).AppendLine(";");
        builder.Append("    public const int PipelineCount = ").Append(counts.PipelineCount).AppendLine(";");
        builder.AppendLine("}");

        return builder.ToString();
    }
}
