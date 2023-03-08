////////////////////////////////////////////////////////////////////////////////////////////////////////
//FileName: Error.cshtml.cs
//FileType: Visual C# Source file
//Author : Siddharth Phatarphod
//Created On : 03/08/2023 20:00 PM
//Last Modified On : 03/08/2023 20:00 PM
//Copyrights : MIT License on Github
//Description : Class for ErrorModel
////////////////////////////////////////////////////////////////////////////////////////////////////////
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
namespace TopicBox.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        private readonly ILogger<ErrorModel> _logger;
        /// <summary>
        /// Constructor of ErrorModel
        /// </summary>
        /// <param name="logger"></param>
        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Gets the Activity Id or HTTP Context Trace Identifier
        /// </summary>
        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        }
    }
}