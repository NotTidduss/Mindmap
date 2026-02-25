using System;

public static class TitleFuzzyMatcher
{
    public static int GetScore(string query, string candidate)
    {
        var normalizedQuery = Normalize(query);
        var normalizedCandidate = Normalize(candidate);

        if (string.IsNullOrEmpty(normalizedQuery) || string.IsNullOrEmpty(normalizedCandidate))
        {
            return -1;
        }

        var containsIndex = normalizedCandidate.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (containsIndex >= 0)
        {
            var containsScore = 2000;
            containsScore -= containsIndex * 5;
            containsScore -= Math.Abs(normalizedCandidate.Length - normalizedQuery.Length);
            return containsScore;
        }

        var queryIndex = 0;
        var firstMatchedIndex = -1;
        var lastMatchedIndex = -1;

        for (var candidateIndex = 0; candidateIndex < normalizedCandidate.Length && queryIndex < normalizedQuery.Length; candidateIndex++)
        {
            if (normalizedCandidate[candidateIndex] != normalizedQuery[queryIndex])
            {
                continue;
            }

            if (firstMatchedIndex < 0)
            {
                firstMatchedIndex = candidateIndex;
            }

            lastMatchedIndex = candidateIndex;
            queryIndex++;
        }

        if (queryIndex != normalizedQuery.Length)
        {
            return -1;
        }

        var span = lastMatchedIndex - firstMatchedIndex + 1;
        var compactnessPenalty = Math.Max(0, span - normalizedQuery.Length);
        var fuzzyScore = 1000;
        fuzzyScore -= compactnessPenalty * 3;
        fuzzyScore -= firstMatchedIndex;
        return fuzzyScore;
    }

    private static string Normalize(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? string.Empty
            : text.Trim().ToLowerInvariant();
    }
}