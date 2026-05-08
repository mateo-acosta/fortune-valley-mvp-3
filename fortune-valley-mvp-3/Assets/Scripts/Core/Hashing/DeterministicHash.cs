namespace FortuneValley.Core.Hashing
{
    /// <summary>
    /// Process-stable string hash. Use this whenever a hash needs to be reproducible
    /// across runs, machines, or .NET versions. Do NOT use string.GetHashCode() or
    /// System.HashCode for these cases: both are randomized per process in modern .NET.
    /// Implementation: 32-bit FNV-1a.
    /// </summary>
    public static class DeterministicHash
    {
        private const uint FnvOffsetBasis = 2166136261u;
        private const uint FnvPrime = 16777619u;

        public static int FromString(string input)
        {
            if (input == null) return 0;
            uint hash = FnvOffsetBasis;
            for (int i = 0; i < input.Length; i++)
            {
                hash ^= input[i];
                hash *= FnvPrime;
            }
            return unchecked((int)hash);
        }
    }
}
