namespace Common.SystemConfiguration
{
    public interface ISystemConfiguration
    {
        string? GetValue(string key);
        T? GetSection<T>(string sectionName) where T : class, new();
    }
}