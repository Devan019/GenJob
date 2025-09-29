namespace GenJobMVC.ViewModel;

public class JobAnalysisViewModel
{
    public Dictionary<string, int> RolesCount { get; set; }
    public Dictionary<string, decimal> AverageSalary { get; set; }
    public Dictionary<string, int> CompanyCount { get; set; }
    public Dictionary<string, decimal> LocationSalary { get; set; }
    public Dictionary<string, double> Rating { get; set; }
    
    // Helper properties for summary cards
    public string HighestPayingRole { get; set; }
    public string HighestPayingRoleSalary { get; set; }
    public string MostJobOpeningsLocation { get; set; }
    public int MostJobOpeningsCount { get; set; }
    public string HighestRatedLocation { get; set; }
    public double HighestRating { get; set; }
}