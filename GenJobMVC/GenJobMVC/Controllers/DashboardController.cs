using System.Diagnostics;
using System.Text.Json;
using GenJobMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

        public DashboardController(ILogger<DashboardController> logger, HttpClient http, RedisConnectionProvider redis, UserManager<User> userManager)
        {
            _logger = logger;
            _httpClient = http;
            _provider = redis;
            _company = _provider.RedisCollection<CompanyModel>();
            _userManager = userManager;
        }

        // [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> SalaryPrediction()
        {
            string api = "http://localhost:8000/get-company-names";
            string userId = _userManager.GetUserId(User);

            var exits = _company.FindById(userId);
            if (exits != null)
            {
                return Json(exits.company);
            }
            
            try
            {
               
                HttpResponseMessage response = await _httpClient.GetAsync(api);
                response.EnsureSuccessStatusCode();

               
                string body = await response.Content.ReadAsStringAsync();

                JsonDocument companies = JsonDocument.Parse(body);
                var list = companies.RootElement.GetProperty("company_list");
                var companyList = list.EnumerateArray().Select(x=>x.GetString()).ToList();
                CompanyModel companyModel = new CompanyModel();
                companyModel.company = companyList;
                companyModel.Id = userId;
                _company.Insert(companyModel);
                return Json(companyList);
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
        

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}