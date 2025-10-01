using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GenJobMVC.Models
{
    public class ATSAnalysisModel
    {
        // [Required(ErrorMessage = "Please upload your resume")] // Temporarily disabled for testing
        public IFormFile? ResumeFile { get; set; }

        [Required(ErrorMessage = "Please provide a job description")]
        [Display(Name = "Job Description")]
        public string JobDescription { get; set; } = string.Empty;

        [Display(Name = "Company Name (Optional)")]
        public string? CompanyName { get; set; }
    }

    public class ATSResultModel
    {
        [JsonPropertyName("total_score")]
        public double TotalScore { get; set; }
        
        [JsonPropertyName("scores")]
        public ScoreBreakdown Scores { get; set; } = new();
        
        [JsonPropertyName("job_requirements")]
        public JobRequirements JobRequirements { get; set; } = new();
        
        [JsonPropertyName("resume_analysis")]
        public ResumeAnalysis ResumeAnalysis { get; set; } = new();
        
        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();
        
        [JsonPropertyName("missing_keywords")]
        public List<string> MissingKeywords { get; set; } = new();
        
        [JsonPropertyName("strengths")]
        public List<string> Strengths { get; set; } = new();
        
        [JsonPropertyName("company_match")]
        public string? CompanyMatch { get; set; }
        
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ScoreBreakdown
    {
        [JsonPropertyName("keyword_match")]
        public double KeywordMatch { get; set; }
        
        [JsonPropertyName("skill_match")]
        public double SkillMatch { get; set; }
        
        [JsonPropertyName("experience_match")]
        public double ExperienceMatch { get; set; }
        
        [JsonPropertyName("education_match")]
        public double EducationMatch { get; set; }
        
        [JsonPropertyName("format_score")]
        public double FormatScore { get; set; }
    }

    public class JobRequirements
    {
        [JsonPropertyName("required_skills")]
        public List<string> RequiredSkills { get; set; } = new();
        
        [JsonPropertyName("required_soft_skills")]
        public List<string> RequiredSoftSkills { get; set; } = new();
        
        [JsonPropertyName("required_experience")]
        public int RequiredExperience { get; set; }
        
        [JsonPropertyName("education_required")]
        public bool EducationRequired { get; set; }
        
        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();
        
        [JsonPropertyName("job_title")]
        public string JobTitle { get; set; } = string.Empty;
        
        [JsonPropertyName("company_mentions")]
        public List<string> CompanyMentions { get; set; } = new();
    }

    public class ResumeAnalysis
    {
        [JsonPropertyName("skills_found")]
        public List<string> SkillsFound { get; set; } = new();
        
        [JsonPropertyName("experience_years")]
        public int ExperienceYears { get; set; }
        
        [JsonPropertyName("education_level")]
        public string EducationLevel { get; set; } = string.Empty;
        
        [JsonPropertyName("keyword_density")]
        public Dictionary<string, double> KeywordDensity { get; set; } = new();
    }

    public class ResumeData
    {
        public string RawText { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        public List<WorkExperience> Experience { get; set; } = new();
        public List<Education> Education { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public ContactInfo ContactInfo { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
    }

    public class WorkExperience
    {
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }

    public class Education
    {
        public string Degree { get; set; } = string.Empty;
        public string Institution { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
    }

    public class ContactInfo
    {
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
    }
}
