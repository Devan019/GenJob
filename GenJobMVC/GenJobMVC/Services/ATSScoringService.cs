using System.Text.RegularExpressions;
using GenJobMVC.Models;

namespace GenJobMVC.Services
{
    public interface IATSScoringService
    {
        Task<ATSResultModel> CalculateScoreAsync(ResumeData resumeData, string jobDescription, string? companyName = null);
    }

    public class ATSScoringService : IATSScoringService
    {
        private readonly ILogger<ATSScoringService> _logger;

        private readonly Dictionary<string, double> _skillWeights = new()
        {
            { "technical", 0.3 },
            { "soft", 0.2 },
            { "experience", 0.25 },
            { "education", 0.15 },
            { "format", 0.1 }
        };

        private readonly Dictionary<string, List<string>> _technicalSkills = new()
        {
            { "programming", new List<string> { "python", "java", "javascript", "c++", "c#", "php", "ruby", "go", "swift", "kotlin", "rust", "scala" } },
            { "web_tech", new List<string> { "html", "css", "react", "angular", "vue", "node.js", "django", "flask", "spring", "laravel", "express" } },
            { "databases", new List<string> { "sql", "mysql", "postgresql", "mongodb", "redis", "elasticsearch", "oracle", "sqlite", "soql", "sosl" } },
            { "cloud", new List<string> { "aws", "azure", "gcp", "docker", "kubernetes", "jenkins", "terraform", "ansible" } },
            { "tools", new List<string> { "git", "github", "gitlab", "jira", "confluence", "slack", "trello", "asana" } },
            { "ai_ml", new List<string> { "machine learning", "deep learning", "tensorflow", "pytorch", "scikit-learn", "pandas", "numpy" } },
            { "methodologies", new List<string> { "agile", "scrum", "devops", "ci/cd", "microservices", "api", "rest", "graphql" } },
            { "salesforce", new List<string> { "salesforce", "apex", "lwc", "lightning web components", "aura components", "visualforce", "sales cloud", "service cloud", "salesforce admin", "soap", "soql", "sosl", "salesforce platform developer", "einstein analytics", "wave analytics" } }
        };

        private readonly List<string> _softSkills = new()
        {
            "leadership", "communication", "teamwork", "problem solving", "analytical",
            "creative", "adaptable", "organized", "detail-oriented", "time management",
            "collaboration", "mentoring", "presentation", "negotiation", "critical thinking"
        };

        public ATSScoringService(ILogger<ATSScoringService> logger)
        {
            _logger = logger;
        }

        public async Task<ATSResultModel> CalculateScoreAsync(ResumeData resumeData, string jobDescription, string? companyName = null)
        {
            try
            {
                // Extract job requirements
                var jobRequirements = AnalyzeJobDescription(jobDescription);

                // Calculate individual scores
                var keywordScore = await CalculateKeywordScoreAsync(resumeData, jobRequirements);
                var skillScore = await CalculateSkillScoreAsync(resumeData, jobRequirements);
                var experienceScore = await CalculateExperienceScoreAsync(resumeData, jobRequirements);
                var educationScore = await CalculateEducationScoreAsync(resumeData, jobRequirements);
                var formatScore = await CalculateFormatScoreAsync(resumeData);

                // Calculate weighted total score
                var totalScore = (
                    keywordScore * _skillWeights["technical"] +
                    skillScore * _skillWeights["technical"] +
                    experienceScore * _skillWeights["experience"] +
                    educationScore * _skillWeights["education"] +
                    formatScore * _skillWeights["format"]
                );

                // Generate recommendations
                var recommendations = await GenerateRecommendationsAsync(resumeData, jobRequirements, new Dictionary<string, double>
                {
                    { "keyword_score", keywordScore },
                    { "skill_score", skillScore },
                    { "experience_score", experienceScore },
                    { "education_score", educationScore },
                    { "format_score", formatScore }
                });

                return new ATSResultModel
                {
                    TotalScore = Math.Round(totalScore, 1),
                    Scores = new ScoreBreakdown
                    {
                        KeywordMatch = Math.Round(keywordScore, 1),
                        SkillMatch = Math.Round(skillScore, 1),
                        ExperienceMatch = Math.Round(experienceScore, 1),
                        EducationMatch = Math.Round(educationScore, 1),
                        FormatScore = Math.Round(formatScore, 1)
                    },
                    JobRequirements = jobRequirements,
                    ResumeAnalysis = new ResumeAnalysis
                    {
                        SkillsFound = resumeData.Skills,
                        ExperienceYears = EstimateExperienceYears(resumeData),
                        EducationLevel = GetEducationLevel(resumeData),
                        KeywordDensity = CalculateKeywordDensity(resumeData.Text, jobRequirements.Keywords)
                    },
                    Recommendations = recommendations,
                    MissingKeywords = FindMissingKeywords(resumeData, jobRequirements),
                    Strengths = IdentifyStrengths(resumeData, jobRequirements),
                    CompanyMatch = companyName,
                    IsSuccess = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating ATS score");
                return new ATSResultModel
                {
                    IsSuccess = false,
                    ErrorMessage = "An error occurred while calculating the ATS score."
                };
            }
        }

        private JobRequirements AnalyzeJobDescription(string jobDescription)
        {
            var text = jobDescription.ToLower();

            // Extract skills
            var requiredSkills = new List<string>();
            foreach (var category in _technicalSkills.Values)
            {
                foreach (var skill in category)
                {
                    if (text.Contains(skill))
                    {
                        requiredSkills.Add(skill);
                    }
                }
            }

            // Extract soft skills
            var requiredSoftSkills = _softSkills.Where(skill => text.Contains(skill)).ToList();

            // Extract experience requirements
            var expPattern = @"(\d+)\+?\s*(?:years?|yrs?)\s*(?:of\s*)?(?:experience|exp)";
            var expMatches = Regex.Matches(text, expPattern);
            var requiredExperience = expMatches.Count > 0 ? expMatches.Cast<Match>().Max(m => int.Parse(m.Groups[1].Value)) : 0;

            // Extract education requirements
            var educationKeywords = new[] { "bachelor", "master", "phd", "degree", "diploma", "certification" };
            var educationRequired = educationKeywords.Any(keyword => text.Contains(keyword));

            // Extract keywords
            var keywords = ExtractJobKeywords(text);

            return new JobRequirements
            {
                RequiredSkills = requiredSkills,
                RequiredSoftSkills = requiredSoftSkills,
                RequiredExperience = requiredExperience,
                EducationRequired = educationRequired,
                Keywords = keywords,
                JobTitle = ExtractJobTitle(jobDescription),
                CompanyMentions = ExtractCompanyMentions(jobDescription)
            };
        }

        private Task<double> CalculateKeywordScoreAsync(ResumeData resumeData, JobRequirements jobRequirements)
        {
            var resumeText = resumeData.Text.ToLower();
            var jobKeywords = jobRequirements.Keywords;

            if (!jobKeywords.Any())
                return Task.FromResult(50.0);

            var matches = jobKeywords.Count(keyword => resumeText.Contains(keyword.ToLower()));
            return Task.FromResult(Math.Min(100.0, (double)matches / jobKeywords.Count * 100));
        }

        private Task<double> CalculateSkillScoreAsync(ResumeData resumeData, JobRequirements jobRequirements)
        {
            var resumeSkills = resumeData.Skills.Select(s => s.ToLower()).ToList();
            var requiredSkills = jobRequirements.RequiredSkills;

            if (!requiredSkills.Any())
                return Task.FromResult(50.0);

            var matches = requiredSkills.Count(skill => 
                resumeSkills.Any(resumeSkill => 
                    resumeSkill.Contains(skill.ToLower()) || skill.ToLower().Contains(resumeSkill)));

            return Task.FromResult(Math.Min(100.0, (double)matches / requiredSkills.Count * 100));
        }

        private Task<double> CalculateExperienceScoreAsync(ResumeData resumeData, JobRequirements jobRequirements)
        {
            var requiredExp = jobRequirements.RequiredExperience;
            if (requiredExp == 0)
                return Task.FromResult(75.0);

            var estimatedExp = EstimateExperienceYears(resumeData);

            if (estimatedExp >= requiredExp)
                return Task.FromResult(100.0);
            else if (estimatedExp >= requiredExp * 0.8)
                return Task.FromResult(85.0);
            else if (estimatedExp >= requiredExp * 0.6)
                return Task.FromResult(70.0);
            else
                return Task.FromResult(Math.Max(20.0, (double)estimatedExp / requiredExp * 100));
        }

        private Task<double> CalculateEducationScoreAsync(ResumeData resumeData, JobRequirements jobRequirements)
        {
            if (!jobRequirements.EducationRequired)
                return Task.FromResult(75.0);

            var education = resumeData.Education;
            if (!education.Any())
                return Task.FromResult(30.0);

            var resumeText = resumeData.Text.ToLower();
            var degreeKeywords = new[] { "bachelor", "master", "phd", "degree", "diploma" };
            var hasDegree = degreeKeywords.Any(keyword => resumeText.Contains(keyword));

            return Task.FromResult(hasDegree ? 100.0 : 40.0);
        }

        private Task<double> CalculateFormatScoreAsync(ResumeData resumeData)
        {
            var text = resumeData.Text;
            var score = 50.0;

            // Check for proper sections
            var sections = new[] { "experience", "education", "skills", "summary" };
            foreach (var section in sections)
            {
                if (text.ToLower().Contains(section))
                    score += 10.0;
            }

            // Check for contact information
            var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
            if (Regex.IsMatch(text, emailPattern))
                score += 10.0;

            return Task.FromResult(Math.Min(100.0, score));
        }

        private int EstimateExperienceYears(ResumeData resumeData)
        {
            var text = resumeData.Text;

            var expPatterns = new[]
            {
                @"(\d+)\+?\s*(?:years?|yrs?)\s*(?:of\s*)?(?:experience|exp)",
                @"(\d{4})\s*[-–]\s*(\d{4}|\bpresent\b)",
                @"(\d{4})\s*[-–]\s*(\d{2,4})"
            };

            var years = new List<int>();
            foreach (var pattern in expPatterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        if (match.Groups.Count == 3)
                        {
                            var start = int.Parse(match.Groups[1].Value);
                            var end = match.Groups[2].Value.ToLower() == "present" ? DateTime.Now.Year : int.Parse(match.Groups[2].Value);
                            years.Add(end - start);
                        }
                        else
                        {
                            if (int.TryParse(match.Groups[1].Value, out var year))
                            {
                                years.Add(year);
                            }
                        }
                    }
                }
            }

            return years.Any() ? years.Max() : 0;
        }

        private string GetEducationLevel(ResumeData resumeData)
        {
            var text = resumeData.Text.ToLower();

            if (text.Contains("phd") || text.Contains("doctorate") || text.Contains("doctoral"))
                return "PhD";
            else if (text.Contains("master") || text.Contains("mba") || text.Contains("ms") || text.Contains("ma"))
                return "Masters";
            else if (text.Contains("bachelor") || text.Contains("bs") || text.Contains("ba") || text.Contains("degree"))
                return "Bachelors";
            else
                return "High School/Other";
        }

        private Dictionary<string, double> CalculateKeywordDensity(string resumeText, List<string> keywords)
        {
            var text = resumeText.ToLower();
            var totalWords = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
            var density = new Dictionary<string, double>();

            foreach (var keyword in keywords)
            {
                var count = Regex.Matches(text, Regex.Escape(keyword.ToLower())).Count;
                density[keyword] = totalWords > 0 ? (double)count / totalWords * 100 : 0;
            }

            return density;
        }

        private List<string> ExtractJobKeywords(string text)
        {
            var words = Regex.Matches(text, @"\b[a-zA-Z]{3,}\b")
                .Cast<Match>()
                .Select(m => m.Value.ToLower())
                .ToList();

            var stopWords = new HashSet<string>
            {
                "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
                "will", "be", "are", "is", "was", "were", "have", "has", "had", "been", "being",
                "this", "that", "these", "those", "a", "an", "as", "we", "you", "they", "it",
                "our", "your", "their", "my", "me", "him", "her", "us", "them"
            };

            return words
                .Where(w => !stopWords.Contains(w) && w.Length > 2)
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(15)
                .ToList();
        }

        private string ExtractJobTitle(string jobDescription)
        {
            var titlePatterns = new[]
            {
                @"(?:looking for|seeking|hiring)\s+(?:a\s+)?([A-Z][a-zA-Z\s]+?)(?:\s+to|\s+for|\s*$)",
                @"(?:position|role|job):\s*([A-Z][a-zA-Z\s]+?)(?:\s*$)"
            };

            foreach (var pattern in titlePatterns)
            {
                var match = Regex.Match(jobDescription, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return "Software Developer";
        }

        private List<string> ExtractCompanyMentions(string jobDescription)
        {
            var words = Regex.Matches(jobDescription, @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*\b")
                .Cast<Match>()
                .Select(m => m.Value)
                .ToList();

            var excludeWords = new HashSet<string>
            {
                "Company", "Inc", "Corp", "LLC", "Ltd", "Software", "Technology", "Solutions"
            };

            return words
                .Where(word => !excludeWords.Contains(word) && word.Length > 3)
                .Take(5)
                .ToList();
        }

        private List<string> FindMissingKeywords(ResumeData resumeData, JobRequirements jobRequirements)
        {
            var resumeText = resumeData.Text.ToLower();
            var jobKeywords = jobRequirements.Keywords;

            return jobKeywords
                .Where(keyword => !resumeText.Contains(keyword.ToLower()))
                .Take(10)
                .ToList();
        }

        private List<string> IdentifyStrengths(ResumeData resumeData, JobRequirements jobRequirements)
        {
            var strengths = new List<string>();

            // Check skill matches
            var resumeSkills = resumeData.Skills.Select(s => s.ToLower()).ToList();
            var requiredSkills = jobRequirements.RequiredSkills;

            var matchedSkills = requiredSkills
                .Where(skill => resumeSkills.Any(resumeSkill => 
                    resumeSkill.Contains(skill.ToLower()) || skill.ToLower().Contains(resumeSkill)))
                .Take(5)
                .ToList();

            if (matchedSkills.Any())
            {
                strengths.Add($"Strong technical skills: {string.Join(", ", matchedSkills)}");
            }

            // Check experience
            var estimatedExp = EstimateExperienceYears(resumeData);
            var requiredExp = jobRequirements.RequiredExperience;

            if (estimatedExp >= requiredExp)
            {
                strengths.Add($"Meets experience requirements ({estimatedExp} years)");
            }

            // Check education
            var educationLevel = GetEducationLevel(resumeData);
            if (educationLevel == "Masters" || educationLevel == "PhD")
            {
                strengths.Add($"Advanced education: {educationLevel}");
            }

            return strengths;
        }

        private Task<List<string>> GenerateRecommendationsAsync(ResumeData resumeData, JobRequirements jobRequirements, Dictionary<string, double> scores)
        {
            var recommendations = new List<string>();

            // Keyword recommendations
            if (scores["keyword_score"] < 70)
            {
                var missingKeywords = FindMissingKeywords(resumeData, jobRequirements);
                if (missingKeywords.Any())
                {
                    recommendations.Add($"Include these keywords: {string.Join(", ", missingKeywords.Take(5))}");
                }
            }

            // Skill recommendations
            if (scores["skill_score"] < 70)
            {
                var missingSkills = jobRequirements.RequiredSkills
                    .Where(skill => !resumeData.Skills.Any(resumeSkill => 
                        resumeSkill.ToLower().Contains(skill.ToLower())))
                    .Take(3)
                    .ToList();

                if (missingSkills.Any())
                {
                    recommendations.Add($"Highlight these skills: {string.Join(", ", missingSkills)}");
                }
            }

            // Experience recommendations
            if (scores["experience_score"] < 70)
            {
                recommendations.Add("Quantify your achievements with specific numbers and metrics");
            }

            // Format recommendations
            if (scores["format_score"] < 80)
            {
                recommendations.Add("Improve resume structure with clear sections (Experience, Education, Skills)");
            }

            // General recommendations
            if (!recommendations.Any())
            {
                recommendations.Add("Your resume looks strong! Consider adding quantifiable achievements.");
            }

            return Task.FromResult(recommendations);
        }
    }
}
