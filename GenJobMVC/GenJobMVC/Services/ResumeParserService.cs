using System.Text;
using System.Text.RegularExpressions;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GenJobMVC.Models;

namespace GenJobMVC.Services
{
    public interface IResumeParserService
    {
        Task<ResumeData> ParseResumeAsync(IFormFile file);
        ResumeData ParseResumeFromText(string text);
    }

    public class ResumeParserService : IResumeParserService
    {
        private readonly ILogger<ResumeParserService> _logger;

        public ResumeParserService(ILogger<ResumeParserService> logger)
        {
            _logger = logger;
        }

        public async Task<ResumeData> ParseResumeAsync(IFormFile file)
        {
            try
            {
                string text;
                
                switch (System.IO.Path.GetExtension(file.FileName).ToLower())
                {
                    case ".pdf":
                        text = await ExtractPdfTextAsync(file);
                        break;
                    case ".docx":
                        text = await ExtractDocxTextAsync(file);
                        break;
                    case ".doc":
                        throw new NotSupportedException("DOC files are not supported. Please convert to DOCX format.");
                    default:
                        throw new NotSupportedException($"File format {System.IO.Path.GetExtension(file.FileName)} is not supported.");
                }

                return ParseResumeFromText(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing resume file: {FileName}", file.FileName);
                throw;
            }
        }

        public ResumeData ParseResumeFromText(string text)
        {
            var resumeData = new ResumeData
            {
                RawText = text,
                Text = text,
                Skills = ExtractSkills(text),
                Experience = ExtractExperience(text),
                Education = ExtractEducation(text),
                Keywords = ExtractKeywords(text),
                ContactInfo = ExtractContactInfo(text),
                Summary = ExtractSummary(text)
            };

            return resumeData;
        }

        private async Task<string> ExtractPdfTextAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var reader = new PdfReader(stream);
            var text = new StringBuilder();

            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                text.Append(PdfTextExtractor.GetTextFromPage(reader, i));
                text.AppendLine();
            }

            return await Task.FromResult(text.ToString());
        }

        private async Task<string> ExtractDocxTextAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var document = WordprocessingDocument.Open(stream, false);
            var body = document.MainDocumentPart?.Document?.Body;
            
            if (body == null)
                return await Task.FromResult(string.Empty);

            var text = new StringBuilder();
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                text.AppendLine(paragraph.InnerText);
            }

            return await Task.FromResult(text.ToString());
        }

        private List<string> ExtractSkills(string text)
        {
            var skills = new HashSet<string>();
            var skillPatterns = new[]
            {
                @"\b(?:Python|Java|JavaScript|C\+\+|C#|PHP|Ruby|Go|Swift|Kotlin|Rust|Scala)\b",
                @"\b(?:React|Angular|Vue|Node\.?js|Django|Flask|Spring|Laravel|Express)\b",
                @"\b(?:AWS|Azure|GCP|Docker|Kubernetes|Jenkins|Git|GitHub|GitLab)\b",
                @"\b(?:SQL|MySQL|PostgreSQL|MongoDB|Redis|Elasticsearch|SOQL|SOSL)\b",
                @"\b(?:Machine Learning|AI|Deep Learning|TensorFlow|PyTorch|Scikit-learn)\b",
                @"\b(?:Agile|Scrum|DevOps|CI/CD|Microservices|REST|GraphQL)\b",
                @"\b(?:Salesforce|Apex|LWC|Lightning Web Components|Aura Components|Visualforce)\b",
                @"\b(?:Sales Cloud|Service Cloud|Einstein Analytics|Wave Analytics)\b",
                @"\b(?:SOAP|RESTful|API|GraphQL|JSON|XML)\b",
                @"\b(?:HTML|CSS|Bootstrap|Tailwind|jQuery|TypeScript)\b"
            };

            foreach (var pattern in skillPatterns)
            {
                var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    skills.Add(match.Value);
                }
            }

            return skills.ToList();
        }

        private List<WorkExperience> ExtractExperience(string text)
        {
            var experience = new List<WorkExperience>();
            
            // Look for experience section
            var expPattern = @"(?i)(?:experience|work\s+history|employment|professional\s+experience)";
            var expMatch = Regex.Match(text, expPattern);
            
            if (expMatch.Success)
            {
                var expSection = text.Substring(expMatch.Index, Math.Min(2000, text.Length - expMatch.Index));
                
                // Extract job titles and companies
                var jobPattern = @"(?i)(?:software\s+engineer|developer|analyst|manager|consultant|specialist|coordinator|director|lead|senior|junior|architect)\s+.*?(?:at|@|,)\s*([A-Z][a-zA-Z\s&]+)";
                var jobs = Regex.Matches(expSection, jobPattern);
                
                foreach (Match job in jobs.Cast<Match>().Take(5))
                {
                    experience.Add(new WorkExperience
                    {
                        Title = job.Groups[0].Value.Split(new[] { "at", "@", "," }, StringSplitOptions.RemoveEmptyEntries)[0].Trim(),
                        Company = job.Groups[1].Value.Trim(),
                        Description = "",
                        Duration = ""
                    });
                }
            }

            return experience;
        }

        private List<Education> ExtractEducation(string text)
        {
            var education = new List<Education>();
            
            var eduPatterns = new[]
            {
                @"(?i)(?:bachelor|master|phd|doctorate|associate|diploma|certificate)\s+(?:of|in)?\s*([a-zA-Z\s]+)",
                @"(?i)(?:university|college|institute|school)\s+of\s+([a-zA-Z\s]+)"
            };

            foreach (var pattern in eduPatterns)
            {
                var matches = Regex.Matches(text, pattern);
                foreach (Match match in matches.Cast<Match>().Take(3))
                {
                    education.Add(new Education
                    {
                        Degree = match.Groups[1].Value.Trim(),
                        Institution = "",
                        Year = ""
                    });
                }
            }

            return education;
        }

        private List<string> ExtractKeywords(string text)
        {
            // Simple keyword extraction - look for important nouns and technical terms
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

            var wordFreq = words
                .Where(w => !stopWords.Contains(w) && w.Length > 2)
                .GroupBy(w => w)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(30)
                .ToList();

            return wordFreq;
        }

        private ContactInfo ExtractContactInfo(string text)
        {
            var contact = new ContactInfo();

            // Email pattern
            var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
            var emailMatch = Regex.Match(text, emailPattern);
            if (emailMatch.Success)
            {
                contact.Email = emailMatch.Value;
            }

            // Phone pattern
            var phonePattern = @"(?:\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})";
            var phoneMatch = Regex.Match(text, phonePattern);
            if (phoneMatch.Success)
            {
                contact.Phone = phoneMatch.Value;
            }

            return contact;
        }

        private string ExtractSummary(string text)
        {
            var summaryPatterns = new[]
            {
                @"(?i)(?:summary|objective|profile|about)\s*[:\-]?\s*([^\n]{50,300})",
                @"(?i)(?:professional\s+summary|career\s+objective)\s*[:\-]?\s*([^\n]{50,300})"
            };

            foreach (var pattern in summaryPatterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            // If no summary found, take first few sentences
            var sentences = text.Split('.', StringSplitOptions.RemoveEmptyEntries).Take(3);
            return string.Join(". ", sentences).Trim();
        }
    }
}
