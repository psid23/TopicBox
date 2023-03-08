////////////////////////////////////////////////////////////////////////////////////////////////////////
//FileName: Email.cshtml.cs
//FileType: Visual C# Source file
//Author : Siddharth Phatarphod
//Created On : 03/08/2023 20:00 PM
//Last Modified On : 03/08/2023 20:00 PM
//Copyrights : MIT License on Github
//Description : Class for EmailModel
////////////////////////////////////////////////////////////////////////////////////////////////////////
using TopicBox.Graph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace TopicBox.Pages
{
    [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
    public class EmailModel : PageModel
    {
        private readonly GraphEmailClient _graphEmailClient;
        private readonly IMemoryCache _memoryCache;
        public List<string> Topics { get; set; }
        private readonly GraphProfileClient _graphProfileClient;
        [BindProperty(SupportsGet = true)]
        public string NextLink { get; set; }
        public IEnumerable<Message> Messages { get; private set; }
        readonly ILogger<IndexModel> _logger;
        /// <summary>
        /// Constructor of EmailModel class
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="graphEmailClient"></param>
        /// <param name="graphProfileClient"></param>
        /// <param name="memoryCache"></param>
        public EmailModel(ILogger<IndexModel> logger, GraphEmailClient graphEmailClient, GraphProfileClient graphProfileClient, IMemoryCache memoryCache)
        {
            _logger = logger;
            _graphEmailClient = graphEmailClient;
            _graphProfileClient = graphProfileClient;
            _memoryCache = memoryCache;
        }
        /// <summary>
        /// Get Method to get the topics and filter messages
        /// </summary>
        /// <returns>Completed Task</returns>
        public async Task OnGetAsync()
        {
            try
            {
                // Get the list of topics from the cache or create a new list
                string cacheKeyName = "Topics!" + GetUserDisplayName().Result;
                var cachedTopics = _memoryCache.Get<List<string>>(cacheKeyName);
                Topics = cachedTopics ?? new List<string>();
                // Get the messages paging data
                var messagesPagingData = await _graphEmailClient.GetUserMessagesPage(NextLink);
                // Assign the messages and next link
                Messages = messagesPagingData.Messages;
                NextLink = messagesPagingData.NextLink;
                if (Topics.Count >= 1)
                {
                    // Filter the messages based on the specified topics in the subject line, case-insensitive
                    var filteredMessages = Messages.Where(message => Topics.Any(topic => message.Subject.IndexOf(topic, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
                    Messages = filteredMessages;
                    if (filteredMessages.Count < 10)
                    {
                        NextLink = null;
                    }
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Log the exception and handle the error
                _logger.LogError(ex, "An error occurred while getting the user messages.");
                // Return an error view or page
                // Alternatively, you can rethrow the exception to let the global error handler handle it
                throw;
            }
        }
        /// <summary>
        /// Gets the User Display Name
        /// </summary>
        /// <returns>User Display Name</returns>
        private async Task<string> GetUserDisplayName()
        {
            try
            {
                var user = await _graphProfileClient.GetUserProfile();
                if (user == null || string.IsNullOrEmpty(user.DisplayName))
                {
                    throw new Exception("User profile not found or display name is empty");
                }
                return user.DisplayName.Replace(" ", "");
            }
            catch (Exception ex)
            {
                // Log the error or handle it appropriately
                Console.WriteLine($"Error occurred while fetching user display name: {ex.Message}");
                return string.Empty;
            }
        }
    }
}