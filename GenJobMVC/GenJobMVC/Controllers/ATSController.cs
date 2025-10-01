using Microsoft.AspNetCore.Mvc;
using GenJobMVC.Models;
using GenJobMVC.Services;
using GenJobMVC.Configuration;

namespace GenJobMVC.Controllers
{
    public class ATSController : Controller
    {
        private readonly ILogger<ATSController> _logger;
        private readonly IResumeParserService _resumeParserService;
        private readonly IATSScoringService _atsScoringService;
        private readonly IRealATSService _realATSService;
        private readonly IConfiguration _configuration;
        private readonly ATSConfiguration _atsConfig;

        public ATSController(
            ILogger<ATSController> logger,
            IResumeParserService resumeParserService,
            IATSScoringService atsScoringService,
            IRealATSService realATSService,
            IConfiguration configuration,
            ATSConfiguration atsConfig)
        {
            _logger = logger;
            _resumeParserService = resumeParserService;
            _atsScoringService = atsScoringService;
            _realATSService = realATSService;
            _configuration = configuration;
            _atsConfig = atsConfig;
        }

        public IActionResult Index()
        {
            return View(new ATSAnalysisModel());
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeResume(ATSAnalysisModel model)
        {
            // Check if this is an AJAX request
            var requestedWith = Request.Headers["X-Requested-With"].FirstOrDefault();
            var contentType = Request.Headers["Content-Type"].FirstOrDefault();
            var accept = Request.Headers["Accept"].FirstOrDefault();
            var isAjaxParam = Request.Query["ajax"].FirstOrDefault();
            
            _logger.LogInformation("Headers - X-Requested-With: {RequestedWith}, Content-Type: {ContentType}, Accept: {Accept}, AjaxParam: {AjaxParam}", 
                requestedWith, contentType, accept, isAjaxParam);
            
            bool isAjaxRequest = requestedWith == "XMLHttpRequest" || 
                                accept?.Contains("application/json") == true ||
                                isAjaxParam == "true";

            if (!ModelState.IsValid)
            {
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "Please correct the validation errors." });
                }
                return View("Index", model);
            }

            try
            {
                // Parse resume file or use text input
                ResumeData resumeData;
                if (model.ResumeFile != null)
                {
                    resumeData = await _resumeParserService.ParseResumeAsync(model.ResumeFile);
                }
                else
                {
                    // If no file uploaded, create a basic resume data from text input
                    resumeData = new ResumeData
                    {
                        Text = "Software Engineer with experience in various technologies",
                        Skills = new List<string> { "C#", ".NET", "Software Development" },
                        Experience = new List<WorkExperience>(),
                        Education = new List<Education>(),
                        Keywords = new List<string>(),
                        ContactInfo = new ContactInfo(),
                        Summary = "Experienced software engineer"
                    };
                }

                // Check if real ATS services are enabled
                var useRealATS = _atsConfig.UseRealATS;
                ATSResultModel result;

                if (useRealATS)
                {
                    // Try real ATS services first
                    result = await _realATSService.GetRealATSScoreAsync(
                        resumeData,
                        model.JobDescription,
                        model.CompanyName
                    );
                }
                else
                {
                    // Use local scoring
                    _logger.LogInformation("Using local scoring service");
                    result = await _atsScoringService.CalculateScoreAsync(
                        resumeData,
                        model.JobDescription,
                        model.CompanyName
                    );
                    _logger.LogInformation("Local scoring result: IsSuccess={IsSuccess}, TotalScore={TotalScore}", 
                        result.IsSuccess, result.TotalScore);
                }

                if (result.IsSuccess)
                {
                    if (isAjaxRequest)
                    {
                        return Json(new { success = true, data = result });
                    }
                    return View("Results", result);
                }
                else
                {
                    if (isAjaxRequest)
                    {
                        return Json(new { success = false, message = result.ErrorMessage ?? "An error occurred during analysis." });
                    }
                    ModelState.AddModelError("", result.ErrorMessage ?? "An error occurred during analysis.");
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ATS analysis");
                if (isAjaxRequest)
                {
                    return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
                }
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                return View("Index", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeResumeAjax(ATSAnalysisModel model, string provider = "auto")
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Please correct the validation errors." });
            }

            try
            {
                // Parse resume file or use text input
                ResumeData resumeData;
                if (model.ResumeFile != null)
                {
                    resumeData = await _resumeParserService.ParseResumeAsync(model.ResumeFile);
                }
                else
                {
                    // If no file uploaded, create a basic resume data from text input
                    resumeData = new ResumeData
                    {
                        Text = "Software Engineer with experience in various technologies",
                        Skills = new List<string> { "C#", ".NET", "Software Development" },
                        Experience = new List<WorkExperience>(),
                        Education = new List<Education>(),
                        Keywords = new List<string>(),
                        ContactInfo = new ContactInfo(),
                        Summary = "Experienced software engineer"
                    };
                }

                // Determine which provider to use based on selection
                ATSResultModel result;
                
                switch (provider.ToLower())
                {
                    case "local":
                        _logger.LogInformation("Using local scoring service (user selected)");
                        result = await _atsScoringService.CalculateScoreAsync(
                            resumeData,
                            model.JobDescription,
                            model.CompanyName
                        );
                        break;
                        
                    case "sharpapi":
                        _logger.LogInformation("Using SharpAPI service (user selected)");
                        result = await GetSharpAPIScoreAsync(resumeData, model.JobDescription, model.CompanyName);
                        break;
                        
                    case "magicalapi":
                        _logger.LogInformation("Using MagicalAPI service (user selected)");
                        result = await GetMagicalAPIScoreAsync(resumeData, model.JobDescription, model.CompanyName);
                        break;
                        
                    case "auto":
                    default:
                        _logger.LogInformation("Using auto mode - trying real ATS services first");
                        var useRealATS = _configuration.GetValue<bool>("ATSSettings:UseRealATS");
                        
                        if (useRealATS)
                        {
                            // Try real ATS services first
                            result = await _realATSService.GetRealATSScoreAsync(
                                resumeData,
                                model.JobDescription,
                                model.CompanyName
                            );
                            
                            // If real ATS fails, fallback to local
                            if (!result.IsSuccess)
                            {
                                _logger.LogInformation("Real ATS failed, falling back to local scoring");
                                result = await _atsScoringService.CalculateScoreAsync(
                                    resumeData,
                                    model.JobDescription,
                                    model.CompanyName
                                );
                            }
                        }
                        else
                        {
                            // Use local scoring
                            _logger.LogInformation("Real ATS disabled, using local scoring");
                            result = await _atsScoringService.CalculateScoreAsync(
                                resumeData,
                                model.JobDescription,
                                model.CompanyName
                            );
                        }
                        break;
                }
                
                _logger.LogInformation("Analysis result: IsSuccess={IsSuccess}, TotalScore={TotalScore}, Provider={Provider}", 
                    result.IsSuccess, result.TotalScore, provider);

                if (result.IsSuccess)
                {
                    return Json(new { success = true, data = result });
                }
                else
                {
                    return Json(new { success = false, message = result.ErrorMessage ?? "An error occurred during analysis." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ATS analysis");
                return Json(new { success = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> QuickScore(string resumeText, string jobDescription, string? companyName = null)
        {
            if (string.IsNullOrWhiteSpace(resumeText) || string.IsNullOrWhiteSpace(jobDescription))
            {
                return Json(new { success = false, message = "Resume text and job description are required." });
            }

            try
            {
                // Parse resume from text
                var resumeData = _resumeParserService.ParseResumeFromText(resumeText);

                // Check if real ATS services are enabled
                var useRealATS = _atsConfig.UseRealATS;
                ATSResultModel result;

                if (useRealATS)
                {
                    // Try real ATS services first
                    result = await _realATSService.GetRealATSScoreAsync(resumeData, jobDescription, companyName);
                }
                else
                {
                    // Use local scoring
                    _logger.LogInformation("QuickScore: Using local scoring service");
                    result = await _atsScoringService.CalculateScoreAsync(resumeData, jobDescription, companyName);
                    _logger.LogInformation("QuickScore: Local scoring result: IsSuccess={IsSuccess}, TotalScore={TotalScore}", 
                        result.IsSuccess, result.TotalScore);
                }
                
                return Json(new { success = result.IsSuccess, data = result, message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during quick score analysis");
                return Json(new { success = false, message = "An unexpected error occurred." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ATSStatus()
        {
            try
            {
                var realATSEnabled = _configuration.GetValue<bool>("ATSSettings:UseRealATS");
                
                if (!realATSEnabled)
                {
                    return Json(new { 
                        success = true, 
                        isAvailable = false,
                        services = new List<object>(),
                        realATSEnabled = false,
                        message = "Using local ATS scoring"
                    });
                }

                // Start background task to check real API status (non-blocking)
                _ = Task.Run(async () => await CheckRealAPIStatusAsync());
                
                // Return immediate response with configured services
                var services = new List<object>();
                var sharpApiKey = _atsConfig.SharpAPIKey;
                var magicalApiKey = _atsConfig.MagicalAPIKey;
                
                if (!string.IsNullOrEmpty(sharpApiKey))
                {
                    services.Add(new { 
                        name = "SharpAPI", 
                        isAvailable = true, 
                        status = "Checking...",
                        responseTime = 0
                    });
                }
                
                if (!string.IsNullOrEmpty(magicalApiKey))
                {
                    services.Add(new { 
                        name = "MagicalAPI", 
                        isAvailable = true, 
                        status = "Checking...",
                        responseTime = 0
                    });
                }
                
                return Json(new { 
                    success = true, 
                    isAvailable = services.Count > 0,
                    services = services,
                    realATSEnabled = realATSEnabled,
                    message = services.Count > 0 ? $"Real ATS Services Checking... ({services.Count} services)" : "Using local ATS scoring"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ATS service status");
                return Json(new { 
                    success = false, 
                    isAvailable = false,
                    services = new List<object>(),
                    realATSEnabled = false,
                    error = "Service check failed"
                });
            }
        }

        private async Task CheckRealAPIStatusAsync()
        {
            try
            {
                // This runs in background - doesn't block the UI
                var services = await _realATSService.GetAvailableServicesAsync();
                _logger.LogInformation("Background API status check completed. Services: {ServiceCount}", services.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background API status check failed");
            }
        }

        private async Task<ATSResultModel> GetSharpAPIScoreAsync(ResumeData resumeData, string jobDescription, string? companyName)
        {
            try
            {
                var apiKey = _atsConfig.SharpAPIKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    return new ATSResultModel { IsSuccess = false, ErrorMessage = "SharpAPI key not configured" };
                }

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var formData = new MultipartFormDataContent();
                
                // Create a text file from resume data
                var resumeBytes = System.Text.Encoding.UTF8.GetBytes(resumeData.Text);
                var resumeContent = new ByteArrayContent(resumeBytes);
                resumeContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                formData.Add(resumeContent, "file", "resume.txt");
                
                // Add job description
                formData.Add(new StringContent(jobDescription), "content");
                
                // Add language (optional)
                formData.Add(new StringContent("en"), "language");

                var response = await client.PostAsync("https://sharpapi.com/api/v1/hr/resume_job_match_score", formData);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ATSResultModel { IsSuccess = false, ErrorMessage = $"SharpAPI error: {response.StatusCode} - {errorContent}" };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jobResponse = System.Text.Json.JsonSerializer.Deserialize<SharpAPIJobResponse>(responseContent);
                
                if (string.IsNullOrEmpty(jobResponse?.JobId))
                {
                    return new ATSResultModel { IsSuccess = false, ErrorMessage = "Failed to get job ID from SharpAPI" };
                }

                // Poll for results
                return await PollSharpAPIResultsAsync(jobResponse.JobId, apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling SharpAPI");
                return new ATSResultModel { IsSuccess = false, ErrorMessage = $"SharpAPI error: {ex.Message}" };
            }
        }

        private async Task<ATSResultModel> GetMagicalAPIScoreAsync(ResumeData resumeData, string jobDescription, string? companyName)
        {
            try
            {
                var apiKey = _atsConfig.MagicalAPIKey;
                if (string.IsNullOrEmpty(apiKey))
                {
                    return new ATSResultModel { IsSuccess = false, ErrorMessage = "MagicalAPI key not configured" };
                }

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

                var json = System.Text.Json.JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://gw.magicalapi.com/resume-score", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    return new ATSResultModel { IsSuccess = false, ErrorMessage = $"MagicalAPI error: {response.StatusCode} - {errorContent}" };
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jobResponse = System.Text.Json.JsonSerializer.Deserialize<MagicalAPIJobResponse>(responseContent);
                
                if (string.IsNullOrEmpty(jobResponse?.RequestId))
                {
                    return new ATSResultModel { IsSuccess = false, ErrorMessage = "Failed to get request ID from MagicalAPI" };
                }

                // Poll for results
                return await PollMagicalAPIResultsAsync(jobResponse.RequestId, apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling MagicalAPI");
                return new ATSResultModel { IsSuccess = false, ErrorMessage = $"MagicalAPI error: {ex.Message}" };
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

        private async Task<ATSResultModel> PollSharpAPIResultsAsync(string jobId, string apiKey)
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
                        return new ATSResultModel { IsSuccess = false, ErrorMessage = $"SharpAPI status check failed: {response.StatusCode}" };
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var statusResponse = System.Text.Json.JsonSerializer.Deserialize<SharpAPIStatusResponse>(responseContent);

                    if (statusResponse?.Data?.Attributes?.Status == "success" && !string.IsNullOrEmpty(statusResponse.Data.Attributes.Result))
                    {
                        // Parse the result JSON string
                        var resultJson = statusResponse.Data.Attributes.Result;
                        var result = System.Text.Json.JsonSerializer.Deserialize<SharpAPIResult>(resultJson);

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
                        return new ATSResultModel { IsSuccess = false, ErrorMessage = "SharpAPI job failed" };
                    }

                    // Wait before next attempt
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 10000); // Exponential backoff, max 10 seconds
                }

                return new ATSResultModel { IsSuccess = false, ErrorMessage = "SharpAPI job did not complete within timeout" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling SharpAPI results");
                return new ATSResultModel { IsSuccess = false, ErrorMessage = $"SharpAPI polling error: {ex.Message}" };
            }
        }

        private async Task<ATSResultModel> PollMagicalAPIResultsAsync(string requestId, string apiKey)
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
                    var json = System.Text.Json.JsonSerializer.Serialize(request);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("https://gw.magicalapi.com/resume-score", content);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        return new ATSResultModel { IsSuccess = false, ErrorMessage = $"MagicalAPI status check failed: {response.StatusCode}" };
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var statusResponse = System.Text.Json.JsonSerializer.Deserialize<MagicalAPIStatusResponse>(responseContent);

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
                        return new ATSResultModel { IsSuccess = false, ErrorMessage = $"MagicalAPI job failed: {statusResponse.Error}" };
                    }

                    // Wait before next attempt
                    await Task.Delay(delayMs);
                    delayMs = Math.Min(delayMs * 2, 10000); // Exponential backoff, max 10 seconds
                }

                return new ATSResultModel { IsSuccess = false, ErrorMessage = "MagicalAPI job did not complete within timeout" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling MagicalAPI results");
                return new ATSResultModel { IsSuccess = false, ErrorMessage = $"MagicalAPI polling error: {ex.Message}" };
            }
        }

        // Response model classes for API integration
        public class SharpAPIJobResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("job_id")]
            public string? JobId { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("status_url")]
            public string? StatusUrl { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string? Status { get; set; }
        }

        public class SharpAPIStatusResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("data")]
            public SharpAPIData? Data { get; set; }
        }

        public class SharpAPIData
        {
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string? Type { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            public string? Id { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("attributes")]
            public SharpAPIAttributes? Attributes { get; set; }
        }

        public class SharpAPIAttributes
        {
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string? Status { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            public string? Type { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("result")]
            public string? Result { get; set; } // This is a JSON string that needs to be parsed
        }

        public class SharpAPIResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("match_scores")]
            public SharpAPIMatchScores? MatchScores { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("explanations")]
            public SharpAPIExplanations? Explanations { get; set; }
        }

        public class SharpAPIMatchScores
        {
            [System.Text.Json.Serialization.JsonPropertyName("overall_match")]
            public double? OverallMatch { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("skills_match")]
            public double? SkillsMatch { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("experience_match")]
            public double? ExperienceMatch { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("education_match")]
            public double? EducationMatch { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("job_title_relevance")]
            public double? JobTitleRelevance { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("technical_stack_match")]
            public double? TechnicalStackMatch { get; set; }
        }

        public class SharpAPIExplanations
        {
            [System.Text.Json.Serialization.JsonPropertyName("recommendations")]
            public List<string>? Recommendations { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("strengths")]
            public List<string>? Strengths { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("areas_for_improvement")]
            public List<string>? AreasForImprovement { get; set; }
        }

        // MagicalAPI Response Models
        public class MagicalAPIJobResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("request_id")]
            public string? RequestId { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string? Status { get; set; }
        }

        public class MagicalAPIStatusResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("status")]
            public string? Status { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("result")]
            public MagicalAPIResult? Result { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("error")]
            public string? Error { get; set; }
        }

        public class MagicalAPIResult
        {
            [System.Text.Json.Serialization.JsonPropertyName("score")]
            public double? Score { get; set; }
            
            [System.Text.Json.Serialization.JsonPropertyName("explanation")]
            public string? Explanation { get; set; }
        }
    }
}
