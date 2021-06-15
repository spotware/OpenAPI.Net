using ASP.NET.Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenAPI.Net.Auth;
using System.Threading.Tasks;

namespace ASP.NET.Demo.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApiCredentials _apiCredentials;

        public IndexModel(ILogger<IndexModel> logger, ApiCredentials apiCredentials)
        {
            _logger = logger;
            _apiCredentials = apiCredentials;
        }

        [BindProperty(Name = "code", SupportsGet = true)]
        public string AuthCode { get; set; }

        public App App => new(_apiCredentials.ClientId, _apiCredentials.Secret, $"{(Request.IsHttps ? "https" : "http")}://{Request.Host}{Request.Path}");

        public ActionResult OnGetAddAccount()
        {
            return Redirect(App.GetAuthUri().ToString());
        }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrWhiteSpace(AuthCode))
            {
                var token = await TokenFactory.GetToken(AuthCode, App);
            }
        }
    }
}