using ASP.NET.Demo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenAPI.Net.Auth;
using System;
using System.Text.Json;
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

        public App App => new(_apiCredentials.ClientId, _apiCredentials.Secret, $"{(Request.IsHttps ? "https" : "http")}://{Request.Host}{Request.Path}");

        public ActionResult OnGetAddAccount()
        {
            return Redirect(App.GetAuthUri().ToString());
        }

        public async Task<ActionResult> OnGetAsync([FromQuery] string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;

            try
            {
                var token = await TokenFactory.GetToken(code, App);

                TempData["Token"] = JsonSerializer.Serialize(token);

                return RedirectToPage("ClientArea");
            }
            catch (Exception ex)
            {
                TempData["Exception"] = JsonSerializer.Serialize(ex);

                return RedirectToPage("Error");
            }
        }
    }
}