using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace BobsBookstoreClassic.Data
{
    // PORT-TODO
    // {"was": "System.Configuration.ConfigurationManager", "note": "BookstoreConfiguration now reads from IConfiguration. In Bookstore.Web Program.cs, call BookstoreConfiguration.Initialize(builder.Configuration) right after WebApplication.CreateBuilder(args), before anything reads a setting. Move the web.config appSettings/connectionStrings into appsettings.json: keys that used '/' (e.g. 'Services/Authentication') become nested sections (Services:Authentication), and connection strings go under ConnectionStrings.", "files": ["../Bookstore.Web/Program.cs", "../Bookstore.Web/appsettings.json"]}
    public sealed class BookstoreConfiguration
    {
        private static readonly Lazy<BookstoreConfiguration> Lazy = new Lazy<BookstoreConfiguration>(() => new BookstoreConfiguration());

        private static BookstoreConfiguration Instance => Lazy.Value;

        private static IConfiguration _configuration;

        // Explicit overrides added at runtime via AddSetting / AddConnectionString take precedence over IConfiguration.
        private readonly Dictionary<string, string> _appSettings = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _connectionStrings = new Dictionary<string, string>();

        private BookstoreConfiguration()
        {
        }

        /// <summary>
        /// Initializes the configuration source. Must be called by the host at startup
        /// (e.g. BookstoreConfiguration.Initialize(builder.Configuration)).
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        private static IConfiguration Configuration =>
            _configuration ?? throw new InvalidOperationException(
                "BookstoreConfiguration.Initialize was not called at startup.");

        // Legacy keys use '/' as a hierarchy separator; IConfiguration uses ':'.
        private static string MapKey(string key) => key.Replace('/', ':');

        public static void AddSetting(string key, string value)
        {
            Instance._appSettings[key] = value;
        }

        public static string GetSetting(string key)
        {
            if (Instance._appSettings.TryGetValue(key, out var overridden))
            {
                return overridden;
            }

            // Preserve the legacy behavior where an environment variable named exactly as the key overrides the setting.
            var environmentValue = Environment.GetEnvironmentVariable(key);
            if (environmentValue != null)
            {
                return environmentValue;
            }

            var value = Configuration[MapKey(key)];
            if (value == null)
            {
                throw new KeyNotFoundException($"The configuration setting '{key}' was not found.");
            }

            return value;
        }

        public static T GetSetting<T>(string key)
        {
            var value = GetSetting(key);

            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static void AddConnectionString(string key, string value)
        {
            Instance._connectionStrings[key] = value;
        }

        public static string GetConnectionString(string key)
        {
            if (Instance._connectionStrings.TryGetValue(key, out var overridden))
            {
                return overridden;
            }

            var value = Configuration.GetConnectionString(MapKey(key));
            if (value == null)
            {
                throw new KeyNotFoundException($"The connection string '{key}' was not found.");
            }

            return value;
        }

    }
}
