namespace DotnetQuest.Api.Features.Sessions;

/// <summary>
/// Şıkları her oyun için karıştırır ama aynı oyunda hep aynı sırayı üretir (deterministik).
/// Böylece sayfa yenilense bile şık sırası değişmez ve doğru cevap her zaman "B" şıkkı olmaz.
/// </summary>
public static class OptionShuffler
{
    /// <returns>permutation[gösterilenIndeks] = orijinalIndeks</returns>
    public static int[] GetPermutation(Guid sessionId, int questionId, int count, bool avoidOriginalOrder)
    {
        var permutation = Enumerable.Range(0, count).ToArray();

        // Guid.GetHashCode ve seed'li Random süreçler arası deterministiktir.
        // (HashCode.Combine KULLANILMAMALI: her süreçte farklı seed üretir.)
        var random = new Random(sessionId.GetHashCode() ^ (questionId * 397));
        random.Shuffle(permutation);

        // Sıralama sorularında öğeleri zaten doğru sırada göstermek oyunu bozar.
        if (avoidOriginalOrder && count > 1 && IsIdentity(permutation))
        {
            permutation = [.. permutation[1..], permutation[0]];
        }

        return permutation;
    }

    public static IReadOnlyList<string> Apply(IReadOnlyList<string> items, int[] permutation) =>
        permutation.Select(index => items[index]).ToArray();

    private static bool IsIdentity(int[] permutation)
    {
        for (var i = 0; i < permutation.Length; i++)
        {
            if (permutation[i] != i)
            {
                return false;
            }
        }

        return true;
    }
}
