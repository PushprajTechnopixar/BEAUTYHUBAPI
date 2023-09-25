using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeautyHubAPI.Firebase
{
    public class MobileMessagingClient : IMobileMessagingClient
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;
        private readonly FirebaseMessaging messaging;

        public MobileMessagingClient(IConfiguration config, ILogger<MobileMessagingClient> logger)
        {
            _config = config;
            _logger = logger;

            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential
                .FromFile(_config["FirebaseNotification:PrivateKeyFile"])
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging")
            });
            messaging = FirebaseMessaging.GetMessaging(app);
        }

        public async Task<string> SendNotificationAsync(string token, string title, string body)
        {
            try
            {
                Message notification;
                // if (phoneType == 1)
                // {
                notification = CreateMessage(title, body, token);
                // }
                // else if(phoneType == 1)
                // {
                //     notification = CreateAPNSMessage(title, body, token);
                // }
                // else
                // {
                //     throw new NotImplementedException();
                // }

                var result = await messaging.SendAsync(notification);
                // _logger.LogInformation("Notification result", result, token, title, body);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        internal static Message CreateMessage(string title, string body, string token)
        {
            var message = new Message
            {
                Token = token,
                // Android = new Config()
                // {
                //     TimeToLive = TimeSpan.FromHours(8),
                //     Priority = Priority.Normal,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body,
                    // Icon = "stock_ticker_update",
                    // Color = "#11b0a7",
                },
                // },
                // Topic = "industry-tech",
            };
            return message;
        }

        internal static Message CreateAPNSMessage(string title, string body, string token)
        {
            var message = new Message
            {
                Token = token,
                Apns = new ApnsConfig()
                {
                    Headers = new Dictionary<string, string>()
                    {
                        { "apns-priority", "10" },
                    },
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            Title = title,
                            Body = body
                        },
                        // Badge = 42,
                    },
                },
                // Topic = "industry-tech",
            };
            return message;
        }
    }
}
