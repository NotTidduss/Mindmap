using System;

public static class Utils
{
    public static bool IsFuzzyMatch(string query, string candidate)
        => GetScore(query, candidate) > 0;

    private static int GetScore(string query, string candidate)
    {
        string normalizedQuery = Normalize(query);
        string normalizedCandidate = Normalize(candidate);

        if (string.IsNullOrEmpty(normalizedQuery) || string.IsNullOrEmpty(normalizedCandidate))
            return -1;

        int containsIndex = normalizedCandidate.IndexOf(normalizedQuery, StringComparison.Ordinal);
        if (containsIndex >= 0)
        {
            int containsScore = 2000;
            containsScore -= containsIndex * 5;
            containsScore -= Math.Abs(normalizedCandidate.Length - normalizedQuery.Length);
            return containsScore;
        }

        int queryIndex = 0;
        int firstMatchedIndex = -1;
        int lastMatchedIndex = -1;

        for (int candidateIndex = 0; candidateIndex < normalizedCandidate.Length && queryIndex < normalizedQuery.Length; candidateIndex++)
        {
            if (normalizedCandidate[candidateIndex] != normalizedQuery[queryIndex]) continue;
            if (firstMatchedIndex < 0) firstMatchedIndex = candidateIndex;

            lastMatchedIndex = candidateIndex;
            queryIndex++;
        }

        if (queryIndex != normalizedQuery.Length)
            return -1;

        int span = lastMatchedIndex - firstMatchedIndex + 1;
        int compactnessPenalty = Math.Max(0, span - normalizedQuery.Length);
        int fuzzyScore = 1000;
        fuzzyScore -= compactnessPenalty * 3;
        fuzzyScore -= firstMatchedIndex;
        return fuzzyScore;
    }

    private static string Normalize(string text)
        => string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim().ToLowerInvariant();
}