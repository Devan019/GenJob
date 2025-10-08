using System.Text.Json.Serialization;

namespace GenJobMVC.Models
{
    public class JobResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public JobData Data { get; set; }
    }

    public class JobData
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }

        [JsonPropertyName("jobs")]
        public List<JobItem> Jobs { get; set; }
    }

    public class JobItem
    {
        [JsonPropertyName("job_id")]
        public long JobId { get; set; }

        [JsonPropertyName("job_title")]
        public string JobTitle { get; set; }

        [JsonPropertyName("company_name")]
        public string CompanyName { get; set; }

        [JsonPropertyName("company_logo")]
        public string CompanyLogo { get; set; }

        [JsonPropertyName("location_name")]
        public string LocationName { get; set; }

        [JsonPropertyName("job_link")]
        public string JobLink { get; set; }

        [JsonPropertyName("age_in_days")]
        public int AgeInDays { get; set; }

        [JsonPropertyName("salary_currency")]
        public string SalaryCurrency { get; set; }

        [JsonPropertyName("salary_min")]
        public decimal? SalaryMin { get; set; }

        [JsonPropertyName("salary_max")]
        public decimal? SalaryMax { get; set; }

        [JsonPropertyName("salary_period")]
        public string SalaryPeriod { get; set; }
    }

}
