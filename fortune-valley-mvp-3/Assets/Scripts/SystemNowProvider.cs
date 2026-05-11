using System;

namespace FortuneValley.Core
{
    /// <summary>
    /// Production <see cref="INowProvider"/> backed by the OS clock.
    /// </summary>
    public class SystemNowProvider : INowProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
