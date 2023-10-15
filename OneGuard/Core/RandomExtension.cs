namespace OneGuard.Core;

public static class RandomExtension
{
    private const string CharsNumbers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string Numbers = "0123456789";

    public static string RandomCharsAndNumbers(this Random random, int length)
    {
        return new(Enumerable.Repeat(CharsNumbers, length)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }

    public static string RandomNumber(this Random random, int length)
    {
        return new(Enumerable.Repeat(Numbers, length)
            .Select(s => s[random.Next(s.Length)])
            .ToArray());
    }
}