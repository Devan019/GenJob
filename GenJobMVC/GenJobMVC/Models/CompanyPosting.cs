using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GenJobMVC.Models
{
    public class CompanyResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("request_id")]
        public string RequestId { get; set; }

        [JsonPropertyName("parameters")]
        public CompanyParameters Parameters { get; set; }

        [JsonPropertyName("data")]
        public List<Company> Data { get; set; }
    }

    public class CompanyParameters
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }
    }

    // Complete Company class with ALL API fields
    public class Company
    {
        [JsonPropertyName("company_id")]
        public int CompanyId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("company_link")]
        public string CompanyLink { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("review_count")]
        public int? ReviewCount { get; set; }

        [JsonPropertyName("salary_count")]
        public int? SalaryCount { get; set; }

        [JsonPropertyName("job_count")]
        public int? JobCount { get; set; }

        [JsonPropertyName("headquarters_location")]
        public string HeadquartersLocation { get; set; }

        [JsonPropertyName("logo")]
        public string Logo { get; set; }

        [JsonPropertyName("company_size")]
        public string CompanySize { get; set; }

        [JsonPropertyName("company_size_category")]
        public string CompanySizeCategory { get; set; }

        [JsonPropertyName("company_description")]
        public string CompanyDescription { get; set; }

        [JsonPropertyName("industry")]
        public string Industry { get; set; }

        [JsonPropertyName("website")]
        public string Website { get; set; }

        [JsonPropertyName("company_type")]
        public string CompanyType { get; set; }

        [JsonPropertyName("revenue")]
        public string Revenue { get; set; }

        [JsonPropertyName("business_outlook_rating")]
        public double? BusinessOutlookRating { get; set; }

        [JsonPropertyName("career_opportunities_rating")]
        public double? CareerOpportunitiesRating { get; set; }

        [JsonPropertyName("ceo")]
        public string Ceo { get; set; }

        [JsonPropertyName("ceo_rating")]
        public double? CeoRating { get; set; }

        [JsonPropertyName("compensation_and_benefits_rating")]
        public double? CompensationAndBenefitsRating { get; set; }

        [JsonPropertyName("culture_and_values_rating")]
        public double? CultureAndValuesRating { get; set; }

        [JsonPropertyName("diversity_and_inclusion_rating")]
        public double? DiversityAndInclusionRating { get; set; }

        [JsonPropertyName("recommend_to_friend_rating")]
        public double? RecommendToFriendRating { get; set; }

        [JsonPropertyName("senior_management_rating")]
        public double? SeniorManagementRating { get; set; }

        [JsonPropertyName("work_life_balance_rating")]
        public double? WorkLifeBalanceRating { get; set; }

        [JsonPropertyName("stock")]
        public string Stock { get; set; }

        [JsonPropertyName("year_founded")]
        public int? YearFounded { get; set; }

        [JsonPropertyName("reviews_link")]
        public string ReviewsLink { get; set; }

        [JsonPropertyName("jobs_link")]
        public string JobsLink { get; set; }

        [JsonPropertyName("faq_link")]
        public string FaqLink { get; set; }

        [JsonPropertyName("competitors")]
        public List<Competitor> Competitors { get; set; }

        [JsonPropertyName("office_locations")]
        public List<OfficeLocation> OfficeLocations { get; set; }

        [JsonPropertyName("best_places_to_work_awards")]
        public List<Award> BestPlacesToWorkAwards { get; set; }
    }

    public class Competitor
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class OfficeLocation
    {
        [JsonPropertyName("city")]
        public string City { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }

    public class Award
    {
        [JsonPropertyName("time_period")]
        public string TimePeriod { get; set; }

        [JsonPropertyName("rank")]
        public int? Rank { get; set; }
    }
}
