using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenJobMVC.Models;
using GenJobMVC.Configuration;

namespace GenJobMVC.Services
{
    public interface IRealATSService
    {
        Task<ATSResultModel> GetRealATSScoreAsync(ResumeData resumeData, string jobDescription, string? companyName = null);
        Task<bool> IsATSServiceAvailableAsync();
        Task<List<ATSServiceInfo>> GetAvailableServicesAsync();
    }

    public class ATSServiceInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Status { get; set; } = string.Empty;
        public double ResponseTime { get; set; }
    }

    public class RealATSService : IRealATSService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RealATSService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IATSScoringService _fallbackService;
        private readonly ATSConfiguration _atsConfig;

        public RealATSService(
            HttpClient httpClient, 
            ILogger<RealATSService> logger, 
            IConfiguration configuration,
            IATSScoringService fallbackService,
            ATSConfiguration atsConfig)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _fallbackService = fallbackService;
            _atsConfig = atsConfig;
        }

        public async Task<ATSResultModel> GetRealATSScoreAsync(ResumeData resumeData, string jobDescription, string? companyName = null)
        {
            var services = new List<Func<Task<ATSResultModel?>>>
            {
                () => GetSharpAPIScoreAsync(resumeData, jobDescription, companyName),
                () => GetMagicalAPIScoreAsync(resumeData, jobDescription, companyName)
            };

            foreach (var service in services)
            {
                try
                {
                    var result = await service();
                    if (result != null && result.IsSuccess)
                    {
                        _logger.LogInformation("Successfully obtained ATS score from real ATS service");
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "ATS service failed, trying next service");
                }
            }

            _logger.LogWarning("All real ATS services failed, falling back to local scoring");
            return await _fallbackService.CalculateScoreAsync(resumeData, jobDescription, companyName);
        }

        public async Task<bool> IsATSServiceAvailableAsync()
        {
            try
            {
                var services = await GetAvailableServicesAsync();
                return services.Any(s => s.IsAvailable);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ATSServiceInfo>> GetAvailableServicesAsync()
        {
            var services = new List<ATSServiceInfo>();

            // Check SharpAPI
            services.Add(await CheckSharpAPIServiceAsync());

            // Check MagicalAPI
            services.Add(await CheckMagicalAPIServiceAsync());

            return services;
        }

        private async Task<ATSResultModel?> GetSharpAPIScoreAsync(ResumeData resumeData, string jobDescription, string? companyName)
        {
            try
            {
                var apiKey = _atsConfig.SharpAPIKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("SharpAPI key not configured");
                    return null;
                }

                // Step 1: Submit the job for processing
                var jobId = await SubmitSharpAPIJobAsync(resumeData, jobDescription, apiKey);
                if (string.IsNullOrEmpty(jobId))
                {
                    _logger.LogWarning("Failed to submit job to SharpAPI");
                    return null;
                }

                // Step 2: Poll for results
                return await PollSharpAPIResultsAsync(jobId, apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling SharpAPI");
                return null;
            }
        }

        private async Task<string?> SubmitSharpAPIJobAsync(ResumeData resumeData, string jobDescription, string apiKey)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var formData = new MultipartFormDataContent();
                
                // Create a text file from resume data
                var resumeBytes = Encoding.UTF8.GetBytes(resumeData.Text);
                var resumeContent = new ByteArrayContent(resumeBytes);
                resumeContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                formData.Add(resumeContent, "file", "resume.txt");
                
                // Add job description as content
                formData.Add(new StringContent(jobDescription), "content");
                
                // Add language (optional)
                formData.Add(new StringContent("en"), "language");

                var response = await client.PostAsync("https://sharpapi.com/api/v1/hr/resume_job_match_score", formData);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SharpAPI job submission failed with status: {StatusCode}", response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("SharpAPI error response: {Error}", errorContent);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jobResponse = JsonSerializer.Deserialize<SharpAPIJobResponse>(responseContent);
                
                return jobResponse?.JobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting job to SharpAPI");
                return null;
            }
        }

        private async Task<ATSResultModel?> PollSharpAPIResultsAsync(string jobId, string apiKey)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var maxAttempts = 10;
                var delayMs = 2000; // Start with 2 seconds

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var response = await client.GetAsync($"https://sharpapi.com/api/v1/job/status/{jobId}");
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("SharpAPI status check failed with status: {StatusCode}", response.StatusCode);
                        return null;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var statusResponse = JsonSerializer.Deserialize<SharpAPIStatusResponse>(responseContent);

                    if (statusResponse?.Data?.Attributes?.Status == "success" && !string.IsNullOrEmpty(statusResponse.Data.Attributes.Result))
                    {
                        // Parse the result JSON string
                        var resultJson = statusResponse.Data.Attributes.Result;
                        var result = JsonSerializer.Deserialize<SharpAPIResult>(resultJson);

                        if (result?.MatchScores != null)
                        {
                            var scores = result.MatchScores;
                            return new ATSResultModel
                            {
                                TotalScore = scores.OverallMatch ?? 0,
                                Scores = new ScoreBreakdown
                                {
                                    KeywordMatch = scores.TechnicalStackMatch ?? 0,
                                    SkillMatch = scores.SkillsMatch ?? 0,
                                    ExperienceMatch = scores.ExperienceMatch ?? 0,
                                    EducationMatch = scores.EducationMatch ?? 0,
                                    FormatScore = scores.JobTitleRelevance ?? 0
                                },
                                JobRequirements = new JobRequirements
                                {
                                    RequiredSkills = new List<string>(), // Will be populated from explanations
                                    Keywords = new List<string>(),
                                    JobTitle = "",
                                    RequiredExperience = 0,
                                    EducationRequired = (scores.EducationMatch ?? 0) > 50
                                },
                                ResumeAnalysis = new ResumeAnalysis
                                {
                                    SkillsFound = new List<string>(),
                                    ExperienceYears = 0,
                                    EducationLevel = "",
                                    KeywordDensity = new Dictionary<string, double>()
                                },
                                Recommendations = result.Explanations?.Recommendations ?? new List<string>(),
                                MissingKeywords = result.Explanations?.AreasForImprovement ?? new List<string>(),
                                Strengths = result.Explanations?.Strengths ?? new List<string>(),
                                CompanyMatch = "",
                                IsSuccess = true,
                                ErrorMessage = null
                            };
                        }
                    }
                    else if (statusResponse?.Data?.Attributes?.Status == "failed")
                    {
                        _logger.LogWarning("SharpAPI job failed");
                        return null;
                    }

                    // Wait before next attempt
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 10000); // Exponential backoff, max 10 seconds
                }

                _logger.LogWarning("SharpAPI job did not complete within timeout");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling SharpAPI results");
                return null;
            }
        }

        private async Task<ATSResultModel?> GetMagicalAPIScoreAsync(ResumeData resumeData, string jobDescription, string? companyName)
        {
            try
            {
                var apiKey = _atsConfig.MagicalAPIKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("MagicalAPI key not configured");
                    return null;
                }

                // Step 1: Submit the job for processing
                var requestId = await SubmitMagicalAPIJobAsync(resumeData, jobDescription, apiKey);
                if (string.IsNullOrEmpty(requestId))
                {
                    _logger.LogWarning("Failed to submit job to MagicalAPI");
                    return null;
                }

                // Step 2: Poll for results
                return await PollMagicalAPIResultsAsync(requestId, apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling MagicalAPI");
                return null;
            }
        }

        private async Task<string?> SubmitMagicalAPIJobAsync(ResumeData resumeData, string jobDescription, string apiKey)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("api-key", apiKey);
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                // For now, we'll create a temporary text file and upload it
                // In a real implementation, you'd need to upload the file to a public URL
                var request = new
                {
                    url = await CreateTemporaryResumeUrlAsync(resumeData.Text),
                    job_description = jobDescription
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://gw.magicalapi.com/resume-score", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("MagicalAPI job submission failed with status: {StatusCode}", response.StatusCode);
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("MagicalAPI error response: {Error}", errorContent);
                    return null;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jobResponse = JsonSerializer.Deserialize<MagicalAPIJobResponse>(responseContent);
                
                return jobResponse?.RequestId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting job to MagicalAPI");
                return null;
            }
        }

        private async Task<string> CreateTemporaryResumeUrlAsync(string resumeText)
        {
            // For demo purposes, we'll create a simple text file URL
            // In production, you'd upload to a file storage service (AWS S3, Azure Blob, etc.)
            // and return the public URL
            
            // For now, return a placeholder - in real implementation, upload file and return URL
            return "https://example.com/resume.txt"; // This would be the actual uploaded file URL
        }

        private async Task<ATSResultModel?> PollMagicalAPIResultsAsync(string requestId, string apiKey)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("api-key", apiKey);
                client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                var maxAttempts = 10;
                var delayMs = 2000; // Start with 2 seconds

                for (int attempt = 0; attempt < maxAttempts; attempt++)
                {
                    var request = new { request_id = requestId };
                    var json = JsonSerializer.Serialize(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("https://gw.magicalapi.com/resume-score", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("MagicalAPI status check failed with status: {StatusCode}", response.StatusCode);
                        return null;
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var statusResponse = JsonSerializer.Deserialize<MagicalAPIStatusResponse>(responseContent);

                    if (statusResponse?.Status == "completed" && statusResponse.Result != null)
                    {
                        var result = statusResponse.Result;
                        return new ATSResultModel
                        {
                            TotalScore = (result.Score ?? 0) * 10, // Convert 0-10 to 0-100
                            Scores = new ScoreBreakdown
                            {
                                KeywordMatch = result.Score ?? 0,
                                SkillMatch = result.Score ?? 0,
                                ExperienceMatch = result.Score ?? 0,
                                EducationMatch = result.Score ?? 0,
                                FormatScore = result.Score ?? 0
                            },
                            JobRequirements = new JobRequirements
                            {
                                RequiredSkills = new List<string>(),
                                Keywords = new List<string>(),
                                JobTitle = "",
                                RequiredExperience = 0,
                                EducationRequired = false
                            },
                            ResumeAnalysis = new ResumeAnalysis
                            {
                                SkillsFound = new List<string>(),
                                ExperienceYears = 0,
                                EducationLevel = "",
                                KeywordDensity = new Dictionary<string, double>()
                            },
                            Recommendations = new List<string> { result.Explanation ?? "" },
                            MissingKeywords = new List<string>(),
                            Strengths = new List<string>(),
                            CompanyMatch = "",
                            IsSuccess = true,
                            ErrorMessage = null
                        };
                    }
                    else if (statusResponse?.Status == "failed")
                    {
                        _logger.LogWarning("MagicalAPI job failed: {Error}", statusResponse.Error);
                        return null;
                    }

                    // Wait before next attempt
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 10000); // Exponential backoff, max 10 seconds
                }

                _logger.LogWarning("MagicalAPI job did not complete within timeout");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling MagicalAPI results");
                return null;
            }
        }


        private async Task<ATSServiceInfo> CheckSharpAPIServiceAsync()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var apiKey = _configuration["ATSServices:SharpAPI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return new ATSServiceInfo
                    {
                        Name = "SharpAPI",
                        Provider = "SharpAPI",
                        IsAvailable = false,
                        Status = "API Key not configured",
                        ResponseTime = 0
                    };
                }

                // Create a new HttpClient with shorter timeout for health check
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
                client.DefaultRequestHeaders.Add("User-Agent", "ATS-Calculator/1.0");

                var response = await client.GetAsync("https://sharpapi.com/api/v1/health");
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                return new ATSServiceInfo
                {
                    Name = "SharpAPI",
                    Provider = "SharpAPI",
                    IsAvailable = response.IsSuccessStatusCode,
                    Status = response.IsSuccessStatusCode ? "Available" : $"Error: {response.StatusCode}",
                    ResponseTime = responseTime
                };
            }
            catch (Exception ex)
            {
                return new ATSServiceInfo
                {
                    Name = "SharpAPI",
                    Provider = "SharpAPI",
                    IsAvailable = false,
                    Status = $"Error: {ex.Message}",
                    ResponseTime = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        }

        private async Task<ATSServiceInfo> CheckMagicalAPIServiceAsync()
        {
            var startTime = DateTime.UtcNow;
            try
            {
                var apiKey = _configuration["ATSServices:MagicalAPI:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return new ATSServiceInfo
                    {
                        Name = "MagicalAPI",
                        Provider = "MagicalAPI",
                        IsAvailable = false,
                        Status = "API Key not configured",
                        ResponseTime = 0
                    };
                }

                // Create a new HttpClient with shorter timeout for health check
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                client.DefaultRequestHeaders.Add("api-key", apiKey);
                client.DefaultRequestHeaders.Add("User-Agent", "ATS-Calculator/1.0");

                // Try to make a simple request to test the API
                var testRequest = new { url = "https://example.com/test.pdf", job_description = "test" };
                var json = JsonSerializer.Serialize(testRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://gw.magicalapi.com/resume-score", content);
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                return new ATSServiceInfo
                {
                    Name = "MagicalAPI",
                    Provider = "MagicalAPI",
                    IsAvailable = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest, // BadRequest means API is working but invalid data
                    Status = response.IsSuccessStatusCode ? "Available" : $"Error: {response.StatusCode}",
                    ResponseTime = responseTime
                };
            }
            catch (Exception ex)
            {
                return new ATSServiceInfo
                {
                    Name = "MagicalAPI",
                    Provider = "MagicalAPI",
                    IsAvailable = false,
                    Status = $"Error: {ex.Message}",
                    ResponseTime = (DateTime.UtcNow - startTime).TotalMilliseconds
                };
            }
        }

    }

    // API Response Models
    public class SharpAPIResponse
    {
        public double OverallScore { get; set; }
        public double KeywordScore { get; set; }
        public double SkillScore { get; set; }
        public double ExperienceScore { get; set; }
        public double EducationScore { get; set; }
        public double FormatScore { get; set; }
        public List<string> RequiredSkills { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public string JobTitle { get; set; } = string.Empty;
        public int RequiredExperience { get; set; }
        public bool EducationRequired { get; set; }
        public List<string> SkillsFound { get; set; } = new();
        public int ExperienceYears { get; set; }
        public string EducationLevel { get; set; } = string.Empty;
        public Dictionary<string, double> KeywordDensity { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public List<string> MissingKeywords { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
    }

    public class MagicalAPIResponse
    {
        public double ATS_Score { get; set; }
        public double Keyword_Match { get; set; }
        public double Skill_Alignment { get; set; }
        public double Experience_Match { get; set; }
        public double Education_Match { get; set; }
        public double Format_Score { get; set; }
        public List<string> Required_Skills { get; set; } = new();
        public List<string> Key_Keywords { get; set; } = new();
        public string Job_Title { get; set; } = string.Empty;
        public int Years_Experience { get; set; }
        public bool Education_Required { get; set; }
        public List<string> Skills_Found { get; set; } = new();
        public string Education_Level { get; set; } = string.Empty;
        public Dictionary<string, double> Keyword_Density { get; set; } = new();
        public List<string> Improvement_Suggestions { get; set; } = new();
        public List<string> Missing_Keywords { get; set; } = new();
        public List<string> Strengths { get; set; } = new();
    }

    // SharpAPI Response Models
    public class SharpAPIJobResponse
    {
        [JsonPropertyName("job_id")]
        public string? JobId { get; set; }
        
        [JsonPropertyName("status_url")]
        public string? StatusUrl { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class SharpAPIStatusResponse
    {
        [JsonPropertyName("data")]
        public SharpAPIData? Data { get; set; }
    }

    public class SharpAPIData
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        
        [JsonPropertyName("attributes")]
        public SharpAPIAttributes? Attributes { get; set; }
    }

    public class SharpAPIAttributes
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        
        [JsonPropertyName("result")]
        public string? Result { get; set; } // This is a JSON string that needs to be parsed
    }

    public class SharpAPIResult
    {
        [JsonPropertyName("match_scores")]
        public SharpAPIMatchScores? MatchScores { get; set; }
        
        [JsonPropertyName("explanations")]
        public SharpAPIExplanations? Explanations { get; set; }
    }

    public class SharpAPIMatchScores
    {
        [JsonPropertyName("overall_match")]
        public double? OverallMatch { get; set; }
        
        [JsonPropertyName("skills_match")]
        public double? SkillsMatch { get; set; }
        
        [JsonPropertyName("experience_match")]
        public double? ExperienceMatch { get; set; }
        
        [JsonPropertyName("education_match")]
        public double? EducationMatch { get; set; }
        
        [JsonPropertyName("certifications_match")]
        public double? CertificationsMatch { get; set; }
        
        [JsonPropertyName("job_title_relevance")]
        public double? JobTitleRelevance { get; set; }
        
        [JsonPropertyName("industry_experience_match")]
        public double? IndustryExperienceMatch { get; set; }
        
        [JsonPropertyName("project_experience_match")]
        public double? ProjectExperienceMatch { get; set; }
        
        [JsonPropertyName("technical_stack_match")]
        public double? TechnicalStackMatch { get; set; }
        
        [JsonPropertyName("methodologies_match")]
        public double? MethodologiesMatch { get; set; }
        
        [JsonPropertyName("soft_skills_match")]
        public double? SoftSkillsMatch { get; set; }
        
        [JsonPropertyName("language_proficiency_match")]
        public double? LanguageProficiencyMatch { get; set; }
        
        [JsonPropertyName("location_preference_match")]
        public double? LocationPreferenceMatch { get; set; }
        
        [JsonPropertyName("remote_work_flexibility")]
        public double? RemoteWorkFlexibility { get; set; }
        
        [JsonPropertyName("certifications_training_relevance")]
        public double? CertificationsTrainingRelevance { get; set; }
        
        [JsonPropertyName("years_experience_weighting")]
        public double? YearsExperienceWeighting { get; set; }
        
        [JsonPropertyName("recent_role_relevance")]
        public double? RecentRoleRelevance { get; set; }
        
        [JsonPropertyName("management_experience_match")]
        public double? ManagementExperienceMatch { get; set; }
        
        [JsonPropertyName("cultural_fit_potential")]
        public double? CulturalFitPotential { get; set; }
        
        [JsonPropertyName("stability_score")]
        public double? StabilityScore { get; set; }
    }

    public class SharpAPIExplanations
    {
        [JsonPropertyName("overall_explanation")]
        public string? OverallExplanation { get; set; }
        
        [JsonPropertyName("strengths")]
        public List<string>? Strengths { get; set; }
        
        [JsonPropertyName("areas_for_improvement")]
        public List<string>? AreasForImprovement { get; set; }
        
        [JsonPropertyName("recommendations")]
        public List<string>? Recommendations { get; set; }
    }

    // MagicalAPI Response Models
    public class MagicalAPIJobResponse
    {
        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    public class MagicalAPIStatusResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("result")]
        public MagicalAPIResult? Result { get; set; }
        
        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    public class MagicalAPIResult
    {
        [JsonPropertyName("score")]
        public double? Score { get; set; }
        
        [JsonPropertyName("explanation")]
        public string? Explanation { get; set; }
    }

}
