using System.Collections.Concurrent;

namespace UniversityLostFound.Application.Common;

/// <summary>
/// Caps failed guesses per key (a university id), independent of the caller's ip, so a forged
/// forwarding header cannot buy unlimited tries at a login or tracking-code check.
/// </summary>
// ponytail: in-memory, per instance — with N API instances the real cap is N x limit and a restart
// clears it. Move the counter to the database or a shared cache if the API is ever scaled out.
public sealed class AttemptGuard(IClock clock)
{
    public const int LoginLimit = 5;
    public const int TrackLimit = 10;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    private const int PruneAbove = 10_000;

    private readonly ConcurrentDictionary<string, (int Count, DateTimeOffset Since)> _failures = new();

    public void EnsureAllowed(string key, int limit)
    {
        if (_failures.TryGetValue(key, out var failures)
            && clock.UtcNow - failures.Since < Window
            && failures.Count >= limit)
        {
            throw new TooManyAttemptsException("Too many failed attempts. Try again later.");
        }
    }

    public void RecordFailure(string key)
    {
        var now = clock.UtcNow;
        _failures.AddOrUpdate(
            key,
            _ => (1, now),
            (_, f) => now - f.Since >= Window ? (1, now) : (f.Count + 1, f.Since));

        // Random ids would otherwise grow the table without bound.
        if (_failures.Count > PruneAbove)
        {
            foreach (var (k, f) in _failures)
            {
                if (now - f.Since >= Window)
                {
                    _failures.TryRemove(k, out _);
                }
            }
        }
    }

    public void Reset(string key) => _failures.TryRemove(key, out _);
}
