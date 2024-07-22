namespace OneGuard.Core;

public interface IHashService
{
    string Hash(params string[] values);
}

public enum HashingType
{
    HMACMD5,
    HMACSHA1,
    HMACSHA256,
    HMACSHA384,
    HMACSHA512
}