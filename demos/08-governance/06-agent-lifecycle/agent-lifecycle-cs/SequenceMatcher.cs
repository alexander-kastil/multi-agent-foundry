static class SequenceMatcher
{
    public static double Ratio(string a, string b)
    {
        Dictionary<char, List<int>> b2j = [];
        for (int i = 0; i < b.Length; i++)
        {
            if (!b2j.TryGetValue(b[i], out var indexes)) b2j[b[i]] = indexes = [];
            indexes.Add(i);
        }
        if (b.Length >= 200)
        {
            int limit = b.Length / 100 + 1;
            foreach (char popular in b2j.Where(e => e.Value.Count > limit).Select(e => e.Key).ToList()) b2j.Remove(popular);
        }

        int matched = 0;
        Queue<(int, int, int, int)> queue = new();
        queue.Enqueue((0, a.Length, 0, b.Length));
        while (queue.Count > 0)
        {
            var (alo, ahi, blo, bhi) = queue.Dequeue();
            var (i, j, k) = LongestMatch(a, b, b2j, alo, ahi, blo, bhi);
            if (k == 0) continue;
            matched += k;
            if (alo < i && blo < j) queue.Enqueue((alo, i, blo, j));
            if (i + k < ahi && j + k < bhi) queue.Enqueue((i + k, ahi, j + k, bhi));
        }
        return 2.0 * matched / (a.Length + b.Length);
    }

    static (int, int, int) LongestMatch(string a, string b, Dictionary<char, List<int>> b2j, int alo, int ahi, int blo, int bhi)
    {
        int besti = alo, bestj = blo, bestsize = 0;
        Dictionary<int, int> j2len = [];
        for (int i = alo; i < ahi; i++)
        {
            Dictionary<int, int> next = [];
            if (b2j.TryGetValue(a[i], out var indexes))
            {
                foreach (int j in indexes)
                {
                    if (j < blo) continue;
                    if (j >= bhi) break;
                    int size = next[j] = j2len.GetValueOrDefault(j - 1) + 1;
                    if (size > bestsize)
                    {
                        besti = i - size + 1;
                        bestj = j - size + 1;
                        bestsize = size;
                    }
                }
            }
            j2len = next;
        }
        while (besti > alo && bestj > blo && a[besti - 1] == b[bestj - 1])
        {
            besti--;
            bestj--;
            bestsize++;
        }
        while (besti + bestsize < ahi && bestj + bestsize < bhi && a[besti + bestsize] == b[bestj + bestsize]) bestsize++;
        return (besti, bestj, bestsize);
    }
}
