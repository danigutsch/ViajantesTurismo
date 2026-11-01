using System.Buffers;
using System.Text;
using PandaBear.InputNormalization;
using PandaBear.Results;

namespace ViajantesTurismo.Catalog.Domain.PublicContent;

/// <summary>The canonical identity of an editable public-content entry.</summary>
public sealed record PublicContentKey : IComparable<PublicContentKey>
{
    private const int ReversedByteOrderMark = 0xFFFE;

    private PublicContentKey(string value) => Value = value;

    /// <summary>Gets the sanitized, uppercase content identity.</summary>
    public string Value
    {
        get;
    }

    /// <summary>Creates a key using the existing Catalog Unicode normalization and length rules.</summary>
    /// <param name="value">The editor-entered key.</param>
    /// <returns>The canonical key, or the existing key validation error.</returns>
    public static Result<PublicContentKey> Create(string? value)
    {
        var remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            // .NET normalization rejects U+FFFE in addition to malformed UTF-16.
            if (Rune.DecodeFromUtf16(remaining, out var scalar, out var consumed) != OperationStatus.Done
                || scalar.Value == ReversedByteOrderMark)
            {
                return PublicContentErrors.InvalidKeyUnicode().ConvertError<PublicContentKey>();
            }

            remaining = remaining[consumed..];
        }

        var canonical = (StringSanitizer.Sanitize(value) ?? string.Empty).ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(canonical))
        {
            return PublicContentErrors.EmptyKey().ConvertError<PublicContentKey>();
        }

        if (canonical.Length > CatalogDomainLimits.MaxDefaultLength)
        {
            return PublicContentErrors.KeyTooLong(CatalogDomainLimits.MaxDefaultLength, canonical.Length).ConvertError<PublicContentKey>();
        }

        return Result.Ok(new PublicContentKey(canonical));
    }

    /// <summary>Materializes an already-canonical persisted identity without changing its meaning.</summary>
    /// <param name="value">The persisted key.</param>
    /// <returns>The validated key.</returns>
    /// <exception cref="InvalidOperationException">The persisted value is invalid or not canonical.</exception>
    public static PublicContentKey RequireCanonical(string value)
    {
        var result = Create(value);
        // Legacy keys normalize before uppercasing. Uppercasing can produce a non-NFC value, so compare canonical
        // forms only to validate the persisted text; never replace that exact storage identity with the comparison form.
        return value is not null && result.IsSuccess && value.Length <= CatalogDomainLimits.MaxDefaultLength
            && string.Equals(result.Value.Value.Normalize(NormalizationForm.FormC), value.Normalize(NormalizationForm.FormC), StringComparison.Ordinal)
            ? new PublicContentKey(value)
            : throw new InvalidOperationException("The persisted public content key must be canonical.");
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Compares canonical key text using ordinal ordering.</summary>
    /// <param name="other">The key to compare.</param>
    /// <returns>The relative order of the keys.</returns>
    public int CompareTo(PublicContentKey? other) => other is null ? 1 : string.Compare(Value, other.Value, StringComparison.Ordinal);

    /// <summary>Compares two content keys using ordinal ordering, with null before a present key.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns>Whether the first key sorts before the second key.</returns>
    public static bool operator <(PublicContentKey? left, PublicContentKey? right) => Comparer<PublicContentKey>.Default.Compare(left, right) < 0;

    /// <summary>Compares two content keys using ordinal ordering, with null before a present key.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns>Whether the first key sorts at or before the second key.</returns>
    public static bool operator <=(PublicContentKey? left, PublicContentKey? right) => Comparer<PublicContentKey>.Default.Compare(left, right) <= 0;

    /// <summary>Compares two content keys using ordinal ordering, with null before a present key.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns>Whether the first key sorts after the second key.</returns>
    public static bool operator >(PublicContentKey? left, PublicContentKey? right) => Comparer<PublicContentKey>.Default.Compare(left, right) > 0;

    /// <summary>Compares two content keys using ordinal ordering, with null before a present key.</summary>
    /// <param name="left">The first key.</param>
    /// <param name="right">The second key.</param>
    /// <returns>Whether the first key sorts at or after the second key.</returns>
    public static bool operator >=(PublicContentKey? left, PublicContentKey? right) => Comparer<PublicContentKey>.Default.Compare(left, right) >= 0;
}
