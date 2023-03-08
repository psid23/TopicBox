////////////////////////////////////////////////////////////////////////////////////////////////////////
//FileName: GraphEmailClient.cs
//FileType: Visual C# Source file
//Author : Siddharth Phatarphod
//Created On : 03/08/2023 20:00 PM
//Last Modified On : 03/08/2023 20:00 PM
//Copyrights : MIT License on Github
//Description : Class for Graph Email Client
////////////////////////////////////////////////////////////////////////////////////////////////////////
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TopicBox.Graph
{
    public class GraphEmailClient
    {
        private readonly ILogger<GraphEmailClient> _logger = null;
        private readonly GraphServiceClient _graphServiceClient = null;
        /// <summary>
        /// Constructor of GraphEmailClient class
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="graphServiceClient"></param>
        public GraphEmailClient(ILogger<GraphEmailClient> logger,GraphServiceClient graphServiceClient)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient;
        }
        /// <summary>
        /// Gets User Messages
        /// </summary>
        /// <returns>User Messages</returns>
        public async Task<IEnumerable<Message>> GetUserMessages()
        {
            try
            {
                var emails = await _graphServiceClient.Me.Messages
            .Request()
            .Select(msg => new
            {
                msg.Subject,
                msg.BodyPreview,
                msg.ReceivedDateTime,
                msg.WebLink
            })
            .OrderBy("receivedDateTime desc")
            .Top(10)
            .GetAsync();
                return emails.CurrentPage;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling Graph /me/messages: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Gets Users Messages Page wise
        /// </summary>
        /// <param name="nextPageLink"></param>
        /// <param name="top"></param>
        /// <returns>Users Messages Page wise</returns>
        public async Task<(IEnumerable<Message> Messages, string NextLink)> GetUserMessagesPage(
            string nextPageLink = null, int top = 10)
        {
            IUserMessagesCollectionPage pagedMessages;

            try
            {
                if (nextPageLink == null)
                {
                    // Get initial page of messages
                    pagedMessages = await _graphServiceClient.Me.Messages
                            .Request()
                            .Select(msg => new
                            {
                                msg.Subject,
                                msg.BodyPreview,
                                msg.ReceivedDateTime,
                                msg.WebLink
                            })
                            .Top(top)
                            .OrderBy("receivedDateTime desc")
                            .GetAsync();
                }
                else
                {
                    // Use the next page request URI value to get the page of messages
                    var messagesCollectionRequest = new UserMessagesCollectionRequest(nextPageLink, _graphServiceClient, null);
                    pagedMessages = await messagesCollectionRequest.GetAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error calling Graph /me/messages to page messages: {ex.Message}");
                throw;
            }

            return (Messages: pagedMessages,
                    NextLink: GetNextLink(pagedMessages));
        }
        /// <summary>
        /// Gets Next Link for the messages
        /// </summary>
        /// <param name="pagedMessages"></param>
        /// <returns>Next Link for the messages</returns>
        private string GetNextLink(IUserMessagesCollectionPage pagedMessages) {
            if (pagedMessages.NextPageRequest != null)
            {
                // Get the URL for the next batch of records
                return pagedMessages.NextPageRequest.GetHttpRequestMessage().RequestUri?.OriginalString;
            }
            return null;
        }
    }
}