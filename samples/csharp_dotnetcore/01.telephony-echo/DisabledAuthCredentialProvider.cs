using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Connector.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// This disables authentication for all incoming requests.
    /// Do not use for production traffic!
    /// </summary>
    public class DisabledAuthCredentialProvider : ICredentialProvider
    {
        /// <summary>
        /// This gets the application password
        /// </summary>
        /// <param name="appId">The app id we need the password for</param>
        /// <returns>The password</returns>
        public Task<string> GetAppPasswordAsync(string appId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks to see if authentication is disabled or not (returns true to indicate disabled)
        /// </summary>
        /// <returns>true</returns>
        public Task<bool> IsAuthenticationDisabledAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Checks to see if the app id is valid
        /// </summary>
        /// <param name="appId">The appid we need to check</param>
        /// <returns>True if the app id is valid false otherwise</returns>
        public Task<bool> IsValidAppIdAsync(string appId)
        {
            throw new NotImplementedException();
        }
    }
}
