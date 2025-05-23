﻿// <copyright file="SimpleGraphClient.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Threading;


namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// This class is a wrapper for the Microsoft Graph API.
    /// See: https://developer.microsoft.com/en-us/graph
    /// </summary>
    public class SimpleGraphClient
    {
        private readonly string _token;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimpleGraphClient"/> class.
        /// </summary>
        /// <param name="token">The token issued to the user.</param>
        public SimpleGraphClient(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            _token = token;
        }

        /// <summary>
        /// Sends an email on the user's behalf using the Microsoft Graph API.
        /// </summary>
        /// <param name="toAddress">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="content">The content of the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendMailAsync(string toAddress, string subject, string content)
        {
            if (string.IsNullOrWhiteSpace(toAddress))
            {
                throw new ArgumentNullException(nameof(toAddress));
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                throw new ArgumentNullException(nameof(subject));
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentNullException(nameof(content));
            }

            var graphClient = GetAuthenticatedClient();
            var recipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = toAddress,
                        },
                    },
                };

            // Create the message.
            var email = new Message
            {
                Body = new ItemBody
                {
                    Content = content,
                    ContentType = BodyType.Text,
                },
                Subject = subject,
                ToRecipients = recipients,
            };

            // Send the message.
            await graphClient.Me.SendMail.PostAsync(new SendMailPostRequestBody { Message = email, SaveToSentItems = true });
        }

        /// <summary>
        /// Gets recent mail for the user using the Microsoft Graph API.
        /// </summary>
        /// <returns>An array of recent messages.</returns>
        public async Task<Message[]> GetRecentMailAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var messages = await graphClient.Me.MailFolders["inbox"].Messages.GetAsync();

            return messages.Value?.Take(5).ToArray();
        }

        /// <summary>
        /// Gets information about the user.
        /// </summary>
        /// <returns>The user information.</returns>
        public async Task<User> GetMeAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var me = await graphClient.Me.GetAsync();
            return me;
        }

        /// <summary>
        /// Gets the user's photo.
        /// </summary>
        /// <returns>The user's photo as a base64 string.</returns>
        public async Task<string> GetPhotoAsync()
        {
            var graphClient = GetAuthenticatedClient();
            var photo = await graphClient.Me.Photo.Content.GetAsync();
            if (photo != null)
            {
                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                var buffers = ms.ToArray();
                return $"data:image/png;base64,{Convert.ToBase64String(buffers)}";
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets an authenticated Microsoft Graph client using the token issued to the user.
        /// </summary>
        /// <returns>The authenticated GraphServiceClient.</returns>
        private GraphServiceClient GetAuthenticatedClient()
        {
            var tokenProvider = new SimpleAccessTokenProvider(_token);

            var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);

            return new GraphServiceClient(authProvider);
        }

        public class SimpleAccessTokenProvider : IAccessTokenProvider
        {
            private readonly string _accessToken;

            public SimpleAccessTokenProvider(string accessToken)
            {
                _accessToken = accessToken;
            }

            public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object> context = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_accessToken);
            }

            public AllowedHostsValidator AllowedHostsValidator => new AllowedHostsValidator();
        }
    }
}