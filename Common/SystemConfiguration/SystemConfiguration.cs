using Microsoft.Extensions.Configuration;

namespace Common.SystemConfiguration
{
    public class SystemConfiguration : ISystemConfiguration
    {
        private readonly IConfiguration _configuration;

        public SystemConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string? GetValue(string key)
        {
            return _configuration[key];
        }

        public T? GetSection<T>(string sectionName) where T : class, new()
        {
            var section = new T();
            _configuration.GetSection(sectionName).Bind(section);
            return section;
        }
    }
}
