using Blackbird.Filters.Transformations;

namespace Apps.XTM.Extensions;

public static class TransformationExtensions
{
    public static void MarkNotCompletedByStateQualifiers(
        this Transformation transformation,
        IEnumerable<string>? qualifiers)
    {
        var lookup = qualifiers?.Where(x => !string.IsNullOrWhiteSpace(x)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (lookup is null || lookup.Count == 0)
            return;

        var segments = transformation.GetUnits().SelectMany(x => x.Segments);
        
        foreach (var segment in segments)
        {
            string? stateQualifier = segment.TargetAttributes.FirstOrDefault(a => a.Name == "state-qualifier")?.Value;

            if (!string.IsNullOrWhiteSpace(stateQualifier) && lookup.Contains(stateQualifier))
                segment.State = null;
        }
    }
}