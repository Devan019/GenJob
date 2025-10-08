using GenJobMVC.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GenJobMVC.Services
{
    public class JobPostingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<JobPostingService> _logger;

        public JobPostingService(HttpClient httpClient, ILogger<JobPostingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<List<Company>> GetCompaniesAsync(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName))
                return new List<Company>();

            string query = Uri.EscapeDataString(companyName);
            string apiKey = Environment.GetEnvironmentVariable("GLASSDOOR_RAPIDAPI_KEY") ?? string.Empty;
            string apiHost = Environment.GetEnvironmentVariable("GLASSDOOR_RAPIDAPI_HOST") ?? string.Empty;

            // Use the correct company search endpoint
            var url = $"https://real-time-glassdoor-data.p.rapidapi.com/company-search?query={query}&domain=www.glassdoor.com";

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-rapidapi-key", apiKey);
                request.Headers.Add("x-rapidapi-host", apiHost);

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch companies. Status: {response.StatusCode}");
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error response: {errorBody}");
                    return new List<Company>();
                }

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Company API Response: {body}");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var apiResponse = JsonSerializer.Deserialize<CompanyResponse>(body, options);

                // IMPORTANT: Data is directly an array of Company objects with all fields
                if (apiResponse?.Data == null || !apiResponse.Data.Any())
                {
                    _logger.LogWarning("No companies found in API response");
                    return new List<Company>();
                }

                _logger.LogInformation($"Successfully parsed {apiResponse.Data.Count} companies with all fields intact");

                // Return companies directly - all API fields are preserved in the Company model
                return apiResponse.Data;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP error while fetching companies: {httpEx.Message}");
                return new List<Company>();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"JSON parsing error while fetching companies: {jsonEx.Message}");
                return new List<Company>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error while fetching companies: {ex.Message}");
                return new List<Company>();
            }
        }

        public async Task<List<JobItem>> GetJobsAsync(string query, string location = null, int page = 1)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<JobItem>();

            string encodedQuery = Uri.EscapeDataString(query);
            string apiKey = Environment.GetEnvironmentVariable("GLASSDOOR_RAPIDAPI_KEY") ?? string.Empty;
            string apiHost = Environment.GetEnvironmentVariable("GLASSDOOR_RAPIDAPI_HOST") ?? string.Empty;

            // enforce default location
            string encodedLocation = Uri.EscapeDataString(location);

            var url =
                $"https://real-time-glassdoor-data.p.rapidapi.com/job-search?query={encodedQuery}&location={encodedLocation}&location_type=ANY&min_company_rating=ANY&easy_apply_only=false&remote_only=false&page={page}&domain=www.glassdoor.com";

            _logger.LogInformation($"Job API URL: {url}");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-rapidapi-key", apiKey);
                request.Headers.Add("x-rapidapi-host", apiHost);

                using var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch jobs. Status: {response.StatusCode}");
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Error response: {errorBody}");
                    return new List<JobItem>();
                }

                var body = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Job API Response length: {body.Length} characters");
                _logger.LogInformation($"Raw API Response: {body}"); // 🔍 debug JSON

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                var apiResponse = JsonSerializer.Deserialize<JobResponse>(body, options);

                if (apiResponse?.Data?.Jobs == null || !apiResponse.Data.Jobs.Any())
                {
                    _logger.LogWarning("No jobs found in API response");
                    return new List<JobItem>();
                }

                _logger.LogInformation($"Successfully parsed {apiResponse.Data.Jobs.Count} jobs");

                return apiResponse.Data.Jobs;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError($"HTTP error while fetching jobs: {httpEx.Message}");
                return new List<JobItem>();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"JSON parsing error while fetching jobs: {jsonEx.Message}");
                return new List<JobItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error while fetching jobs: {ex.Message}");
                return new List<JobItem>();
            }
        }

    }
}