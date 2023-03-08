////////////////////////////////////////////////////////////////////////////////////////////////////////
//FileName: Index.cshtml.cs
//FileType: Visual C# Source file
//Author : Siddharth Phatarphod
//Created On : 03/08/2023 20:00 PM
//Last Modified On : 03/08/2023 20:00 PM
//Copyrights : MIT License on Github
//Description : Class for IndexModel
////////////////////////////////////////////////////////////////////////////////////////////////////////
using TopicBox.Graph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace TopicBox.Pages
{
    [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
    public class IndexModel : PageModel
    {
        private readonly IMemoryCache _memoryCache;
        public List<string> Topics { get; set; }        
        public bool IsPost { get; set; }
        public string NewTopic { get; set; }
        private readonly GraphProfileClient _graphProfileClient;
        public string UserDisplayName { get; private set; } = "";
        public string UserPhoto { get; private set; }
        readonly ITokenAcquisition _tokenAcquisition;
        readonly ILogger<IndexModel> _logger;
        public Boolean isTopicsCountReached = false;
        const int topicsCount = 5;
        /// <summary>
        /// Post method to get the topics and filter messages
        /// </summary>
        /// <param name="topicInput"></param>
        /// <returns></returns>
        public async Task<IActionResult> OnPostAsync(string topicInput)
        {
            IsPost = true;
            string cacheKeyName = "Topics!" + GetUserDisplayName().Result;
            var cachedTopics = _memoryCache.Get<List<string>>(cacheKeyName);
            Topics = cachedTopics ?? new List<string>();
            // Check if the topic already exists in Topics (case-insensitive)
            var existingTopic = Topics.Find(t => t.Equals(topicInput, StringComparison.OrdinalIgnoreCase));
            // Add the new topic only if it does not exist and there is room for it
            if (existingTopic == null && Topics.Count < topicsCount)
            {
                Topics.Add(topicInput);
                _memoryCache.Set(cacheKeyName, Topics, TimeSpan.FromHours(1));
                NewTopic = topicInput;
            }
            else if (existingTopic != null)
            {
                // Topic already exists, do nothing
                NewTopic = existingTopic;
            }
            else
            {
                // Too many topics, cannot add more
                isTopicsCountReached = true;
            }
            return Page();
        }
        /// <summary>
        /// Constructor of IndexModel class
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="graphProfileClient"></param>
        /// <param name="tokenAcquisition"></param>
        /// <param name="memoryCache"></param>
        public IndexModel(ILogger<IndexModel> logger, GraphProfileClient graphProfileClient, ITokenAcquisition tokenAcquisition, IMemoryCache memoryCache)
        {
            _logger = logger;
            _graphProfileClient = graphProfileClient;
            _tokenAcquisition = tokenAcquisition;
            _memoryCache = memoryCache;
        }
        /// <summary>
        /// Get Method to get the topics
        /// </summary>
        /// <returns></returns>
        public async Task OnGetAsync()
        {
            var user = await _graphProfileClient.GetUserProfile(); 
            UserDisplayName = user.DisplayName.Split(' ')[0];
            UserPhoto = await _graphProfileClient.GetUserProfileImage();
            string cacheKeyName = "Topics!" + GetUserDisplayName().Result;
            var cachedTopics = _memoryCache.Get<List<string>>(cacheKeyName);
            Topics = cachedTopics ?? new List<string>();
            Topics ??= new List<string>();            
            if (Topics.Count == topicsCount)
            {
                isTopicsCountReached = true;
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
        /// <summary>
        /// Get Access Token
        /// </summary>
        /// <returns>Access Token</returns>
        public async Task OnGetAccessTokenAsync() {
            // A simple example of getting an access token and making a call to Graph to retrieve the 
            // user's display name. You can view the token in the console (after running dotnet run).
            // Visit https://jwt.ms, copy the token into the textbox, and you can see the scopes available to the 
            // token in addition to other information.
            // https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-web-app-call-api-acquire-token?tabs=aspnetcore
            // Acquire the access token.
            string[] scopes = new string[]{"user.read"};
            string accessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            _logger.LogInformation($"Token: {accessToken}");
            // Use the access token to call a protected web API.
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            string json = await client.GetStringAsync("https://graph.microsoft.com/v1.0/me?$select=displayName");
            _logger.LogInformation(json);
        }
        /// <summary>
        /// Called on Delete of Topic
        /// </summary>
        /// <param name="topic"></param>
        /// <returns>Action Result</returns>
        public async Task<IActionResult> OnPostDeleteAsync(string topic)
        {
            string cacheKeyName = "Topics!" + await GetUserDisplayName();
            if (_memoryCache.TryGetValue(cacheKeyName, out List<string> cachedTopics))
            {
                cachedTopics.Remove(topic);
                _memoryCache.Set(cacheKeyName, cachedTopics, TimeSpan.FromHours(1));
            }
            return RedirectToPage();
        }
    }
}