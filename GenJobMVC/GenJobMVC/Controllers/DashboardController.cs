using System.Diagnostics;
using System.Text.Json;
using GenJobMVC.Models;
using GenJobMVC.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Redis.OM;
using Redis.OM.Searching;

namespace GenJobMVC.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly HttpClient _httpClient;
        private readonly RedisConnectionProvider _provider;
        private readonly IRedisCollection<CompanyModel> _company;
        private readonly UserManager<User> _userManager;
        private readonly AI_API _aiApi;

        public DashboardController(ILogger<DashboardController> logger, HttpClient http, RedisConnectionProvider redis,
            UserManager<User> userManager, IOptions<AI_API> ai)
        {
            _logger = logger;
            _httpClient = http;
            _provider = redis;
            _company = _provider.RedisCollection<CompanyModel>();
            _userManager = userManager;
            _aiApi = ai.Value;
        }

        // [Authorize]
        public IActionResult Index()
        {
            return View();
        }


        [Authorize]
        [HttpGet]
        public async Task<IActionResult> SalaryPrediction()
        {
            string userId = _userManager.GetUserId(User);

            var exits = _company.FindById(userId);
            CompanyViewModel companyViewModel = new CompanyViewModel();
            if (exits != null)
            {
                companyViewModel.company = exits.company;
                return View(companyViewModel);
            }

            string api = _aiApi.Base + _aiApi.EndPoints.GetCompanies;

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(api);
                response.EnsureSuccessStatusCode();


                string body = await response.Content.ReadAsStringAsync();

                JsonDocument companies = JsonDocument.Parse(body);
                var list = companies.RootElement.GetProperty("company_list");
                var companyList = list.EnumerateArray().Select(x => x.GetString()).ToList();
                CompanyModel companyModel = new CompanyModel();
                companyModel.company = companyList;
                companyModel.Id = userId;
                _company.Insert(companyModel);

                companyViewModel.company = companyList;
                return View(companyViewModel);
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine(httpEx.Message);
                return StatusCode(500, "Error calling the API");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Something went wrong");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetCompanyDetails([FromBody] CompanyRequest company)
        {
            var response = await _httpClient.PostAsJsonAsync(_aiApi.Base + _aiApi.EndPoints.GetOtherData, company);
            var data = await response.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }


        [HttpPost]
        public async Task<IActionResult> Predict([FromBody] PredictRequest req)
        {
            var response = await _httpClient.PostAsJsonAsync(_aiApi.Base + _aiApi.EndPoints.PredictSalary, req);
            var data = await response.Content.ReadAsStringAsync();
            return Content(data, "application/json");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> JobRolesAnalysis()
        {
            try
            {
                var response = await _httpClient.GetAsync(_aiApi.Base + _aiApi.EndPoints.JobRolesAnalysis);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    var viewModel = JsonSerializer.Deserialize<JobAnalysisViewModel>(jsonString, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    // Calculate summary values
                    if (viewModel != null)
                    {
                        viewModel.HighestPayingRole = viewModel.AverageSalary?
                            .OrderByDescending(x => x.Value)
                            .FirstOrDefault().Key ?? "N/A";

                        viewModel.HighestPayingRoleSalary = viewModel.AverageSalary?
                            .OrderByDescending(x => x.Value)
                            .FirstOrDefault().Value.ToString("C0") ?? "N/A";

                        viewModel.MostJobOpeningsLocation = viewModel.CompanyCount?
                            .OrderByDescending(x => x.Value)
                            .FirstOrDefault().Key ?? "N/A";

                        viewModel.MostJobOpeningsCount = viewModel.CompanyCount?
                            .OrderByDescending(x => x.Value)
                            .FirstOrDefault().Value ?? 0;

                        viewModel.HighestRatedLocation = viewModel.Rating?
                            .OrderByDescending(x => x.Value)
                            .FirstOrDefault().Key ?? "N/A";

                        viewModel.HighestRating = viewModel.Rating?
                            .OrderByDescending(x => x.Value)
                            .FirstOrDefault().Value ?? 0;
                    }

                    return View(viewModel);
                }
                else
                {
                    // Handle API error - return empty view model or error page
                    return View(new JobAnalysisViewModel());
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                return View(new JobAnalysisViewModel());
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

public class CompanyRequest
{
    public string company_name { get; set; }
}

public class PredictRequest
{
    public string company_name { get; set; }
    public string job_role { get; set; }
    public string location { get; set; }
    public string status { get; set; }
}