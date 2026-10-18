using System.Numerics;

static class KsTest
{
    public static (double Statistic, double PValue) TwoSample(double[] a, double[] b)
    {
        double d = a.Concat(b).Distinct().Max(v => Math.Abs((double)a.Count(x => x <= v) / a.Length - (double)b.Count(x => x <= v) / b.Length));
        return (d, ExactPValue(a.Length, b.Length, d));
    }

    static double ExactPValue(int m, int n, double d)
    {
        int g = (int)BigInteger.GreatestCommonDivisor(m, n);
        long lcm = (long)(m / g) * n;
        long h = (long)Math.Round(d * lcm);
        if (h == 0) return 1.0;

        BigInteger[,] paths = new BigInteger[m + 1, n + 1];
        for (int i = 0; i <= m; i++)
        {
            for (int j = 0; j <= n; j++)
            {
                bool inside = Math.Abs((long)i * n - (long)j * m) < h * g;
                if (!inside) continue;
                if (i == 0 && j == 0) paths[i, j] = 1;
                else paths[i, j] = (i > 0 ? paths[i - 1, j] : 0) + (j > 0 ? paths[i, j - 1] : 0);
            }
        }

        BigInteger total = 1;
        for (int k = 1; k <= m; k++) total = total * (n + k) / k;
        double probabilityInside = (double)paths[m, n] / (double)total;
        return Math.Clamp(1 - probabilityInside, 0, 1);
    }
}
