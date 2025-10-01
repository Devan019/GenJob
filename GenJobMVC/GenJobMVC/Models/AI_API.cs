namespace GenJobMVC.Models;

public class AI_API
{
    public string Base { get; set; }
    public EndPoints EndPoints { get; set; }
}

public class EndPoints
{
    public string PredictSalary { get; set; }
    public string GetOtherData { get; set; }
    public string GetCompanies { get; set; }
    
    public string JobRolesAnalysis { get; set; }
    
    public string Test { get; set; }
    
    public string ResumeGen { get; set; }
}