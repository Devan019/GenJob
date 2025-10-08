using GenJobMVC.Models;
using GenJobMVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace GenJobMVC.Controllers
{
    public class JobPostingController : Controller
    {
        private readonly JobPostingService _jobPostingService;
        private readonly ILogger<JobPostingController> _logger;

        public JobPostingController(JobPostingService jobService, ILogger<JobPostingController> logger)
        {
            _jobPostingService = jobService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            _logger.LogInformation("Dashboard GET request received");
            return View(new DashboardViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> SearchJobs(string jobQuery, string location)
        {
            _logger.LogInformation("=== SearchJobs POST request received ===");
            _logger.LogInformation($"jobQuery: '{jobQuery}'");
            _logger.LogInformation($"location: '{location}'");

            if (string.IsNullOrWhiteSpace(jobQuery))
            {
                _logger.LogWarning("Job query is empty");
                ModelState.AddModelError("", "Please enter a job title or keyword");
                ViewBag.SearchPerformed = true;
                ViewBag.SearchType = "Jobs";
                return View("Dashboard", new DashboardViewModel());
            }

            try
            {
                _logger.LogInformation("Calling GetJobsAsync...");

                var jobs = await _jobPostingService.GetJobsAsync(jobQuery, location);

                _logger.LogInformation($"GetJobsAsync returned {jobs?.Count ?? 0} jobs");

                if (jobs != null && jobs.Any())
                {
                    _logger.LogInformation("First job title: " + jobs[0].JobTitle);
                }

                var model = new DashboardViewModel
                {
                    Jobs = jobs ?? new List<JobItem>()
                };

                ViewBag.SearchPerformed = true;
                ViewBag.SearchType = "Jobs";

                _logger.LogInformation("Returning view with model");
                return View("Dashboard", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in SearchJobs: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                ViewBag.SearchPerformed = true;
                ViewBag.SearchType = "Jobs";
                return View("Dashboard", new DashboardViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SearchCompanies(string companyName)
        {
            _logger.LogInformation("=== SearchCompanies POST request received ===");
            _logger.LogInformation($"companyName: '{companyName}'");

            if (string.IsNullOrWhiteSpace(companyName))
            {
                _logger.LogWarning("Company name is empty");
                ModelState.AddModelError("", "Please enter a company name");
                ViewBag.SearchPerformed = true;
                ViewBag.SearchType = "Companies";
                return View("Dashboard", new DashboardViewModel());
            }

            try
            {
                _logger.LogInformation("Calling GetCompaniesAsync...");
                var companies = await _jobPostingService.GetCompaniesAsync(companyName);

                _logger.LogInformation($"GetCompaniesAsync returned {companies?.Count ?? 0} companies");

                if (companies != null && companies.Any())
                {
                    _logger.LogInformation("First company name: " + companies[0].Name);
                }

                var model = new DashboardViewModel
                {
                    Companies = companies ?? new List<Company>()
                };

                ViewBag.SearchPerformed = true;
                ViewBag.SearchType = "Companies";

                _logger.LogInformation("Returning view with model");
                return View("Dashboard", model);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERROR in SearchCompanies: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                ViewBag.SearchPerformed = true;
                ViewBag.SearchType = "Companies";
                return View("Dashboard", new DashboardViewModel());
            }
        }
    }
}