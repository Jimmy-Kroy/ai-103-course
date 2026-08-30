using System;
using System.Collections.Generic;
using System.Text;

namespace chat_completion
{
    /// <summary>
    /// Abstraction over the chat/response call so consumers can be unit tested
    /// against a mock/fake instead of the real Azure OpenAI service.
    /// </summary>
    public interface IOpenAIClient
    {
        /// <summary>
        /// Sends a single user message and returns the assistant's text response.
        /// </summary>
        /// <param name="userMessage">The prompt/question to send.</param>
        /// <param name="cancellationToken">Cancellation token for the request.</param>
        Task<string> GetResponseAsync(string userMessage, CancellationToken cancellationToken = default);
    }
}
