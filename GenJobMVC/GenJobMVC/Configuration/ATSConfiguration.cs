namespace GenJobMVC.Configuration
{
    public class ATSConfiguration
    {
        public string SharpAPIKey { get; set; } = string.Empty;
        public string MagicalAPIKey { get; set; } = string.Empty;
        public bool UseRealATS { get; set; } = true;
        public bool FallbackToLocal { get; set; } = true;
        public int TimeoutSeconds { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;

        public static ATSConfiguration LoadFromEnvironment()
        {
            return new ATSConfiguration
            {
                SharpAPIKey = Environment.GetEnvironmentVariable("SHARPAPI_KEY") ?? string.Empty,
                MagicalAPIKey = Environment.GetEnvironmentVariable("MAGICALAPI_KEY") ?? string.Empty,
                UseRealATS = bool.Parse(Environment.GetEnvironmentVariable("USE_REAL_ATS") ?? "true"),
                FallbackToLocal = bool.Parse(Environment.GetEnvironmentVariable("FALLBACK_TO_LOCAL") ?? "true"),
                TimeoutSeconds = int.Parse(Environment.GetEnvironmentVariable("TIMEOUT_SECONDS") ?? "30"),
                RetryAttempts = int.Parse(Environment.GetEnvironmentVariable("RETRY_ATTEMPTS") ?? "3")
            };
        }
    }
}
