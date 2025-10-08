using GenJobMVC.Models;
using System.Collections.Generic;

namespace GenJobMVC.Models
{
    public class DashboardViewModel
    {
        public List<JobItem> Jobs { get; set; } = new();
        public List<Company> Companies { get; set; } = new();
    }
}
