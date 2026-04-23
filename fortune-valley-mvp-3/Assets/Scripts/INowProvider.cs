using System;

namespace FortuneValley.Core
{
    /// <summary>
    /// Abstraction over wall-clock time. Production binding returns
    /// <see cref="DateTime.UtcNow"/>; tests inject a fake clock for deterministic
    /// cooldown and debounce coverage.
    /// </summary>
    public interface INowProvider
    {
        DateTime UtcNow { get; }
    }
}
