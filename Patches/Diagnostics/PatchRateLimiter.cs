namespace SOD_CityRelations.Patches.Diagnostics;

internal sealed class PatchRateLimiter
{
    private readonly int maxPerMinute;
    private DateTime windowStartUtc = DateTime.UtcNow;
    private int count;

    public PatchRateLimiter(int maxPerMinute)
    {
        this.maxPerMinute = Math.Max(1, maxPerMinute);
    }

    public bool TryAcquire()
    {
        var now = DateTime.UtcNow;
        if (now - windowStartUtc >= TimeSpan.FromMinutes(1))
        {
            windowStartUtc = now;
            count = 0;
        }

        if (count >= maxPerMinute)
        {
            return false;
        }

        count++;
        return true;
    }
}
