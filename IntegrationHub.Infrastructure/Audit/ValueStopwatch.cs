using System.Diagnostics;

namespace IntegrationHub.Infrastructure.Audit;

public readonly struct ValueStopwatch
{
    private static readonly double TickFrequency = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    private readonly long _startTimestamp;

    private ValueStopwatch(long start) => _startTimestamp = start;

    public static ValueStopwatch StartNew() => new ValueStopwatch(Stopwatch.GetTimestamp());

    public TimeSpan GetElapsedTime()
    {
        long delta = Stopwatch.GetTimestamp() - _startTimestamp;
        long ticks = (long)(delta * TickFrequency);
        return new TimeSpan(ticks);
    }
}
