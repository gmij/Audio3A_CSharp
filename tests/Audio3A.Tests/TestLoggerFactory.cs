using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Audio3A.Tests;

/// <summary>
/// Helper class for creating test loggers
/// </summary>
public static class TestLoggerFactory
{
    /// <summary>
    /// Creates a null logger for testing (logs are discarded)
    /// </summary>
    public static ILogger<T> CreateNullLogger<T>()
    {
        return NullLogger<T>.Instance;
    }
}
