using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using ViajantesTurismo.Branding.Contracts.Http;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>An RFC 9110 <c>If-Match</c> condition evaluated with strong comparison.</summary>
internal sealed class BrandingIfMatch
{
    private readonly bool _matchesAny;
    private readonly IReadOnlyList<EntityTagHeaderValue> _tags;

    private BrandingIfMatch(bool matchesAny, IReadOnlyList<EntityTagHeaderValue> tags)
    {
        _matchesAny = matchesAny;
        _tags = tags;
    }

    /// <summary>Parses the field value; <see langword="null" /> when the syntax is not a valid <c>If-Match</c>.</summary>
    public static BrandingIfMatch? Parse(StringValues values)
    {
        if (!EntityTagHeaderValue.TryParseStrictList(values, out var parsed) || parsed.Count == 0)
        {
            return null;
        }

        var hasAny = parsed.Any(tag => tag.Equals(EntityTagHeaderValue.Any));
        if (hasAny)
        {
            // "*" and an entity-tag list are alternative forms; combining them is malformed, not a list match.
            return parsed.Count == 1 ? new BrandingIfMatch(matchesAny: true, []) : null;
        }

        return new BrandingIfMatch(matchesAny: false, [.. parsed]);
    }

    /// <summary>Evaluates the condition against the current resource state.</summary>
    public bool Matches(BrandingSettingsSnapshot? current)
    {
        if (current is null)
        {
            return false;
        }

        if (_matchesAny)
        {
            return true;
        }

        var currentTag = new EntityTagHeaderValue(BrandingEntityTags.Format(current.SourceEpoch, current.SourceRevision));
        return _tags.Any(tag => tag.Compare(currentTag, useStrongComparison: true));
    }
}
