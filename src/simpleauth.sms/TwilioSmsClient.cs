﻿namespace SimpleAuth.Sms
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Amazon;
    using Amazon.Runtime;
    using Amazon.SimpleNotificationService;
    using Amazon.SimpleNotificationService.Model;

    internal class AwsSmsClient : ISmsClient
    {
        private readonly string _sender;
        private readonly AmazonSimpleNotificationServiceClient _client;

        public AwsSmsClient(AWSCredentials credentials, RegionEndpoint region, string sender)
        {
            _client = new AmazonSimpleNotificationServiceClient(credentials, region);
            _sender = sender;
        }

        public async Task<bool> SendMessage(string toPhoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(toPhoneNumber))
            {
                throw new ArgumentException(nameof(toPhoneNumber));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(nameof(message));
            }

            var pubRequest = new PublishRequest
            {
                Message = message,
                PhoneNumber = toPhoneNumber,
                MessageAttributes =
                {
                    ["AWS.SNS.SMS.SenderID"] = new MessageAttributeValue {StringValue = _sender, DataType = "String"},
                    ["AWS.SNS.SMS.SMSType"] = new MessageAttributeValue {StringValue = "Transactional", DataType = "String"}
                }
            };
            var pubResponse = await _client.PublishAsync(pubRequest);

            return (int)pubResponse.HttpStatusCode < 400;
        }
    }

    internal class TwilioSmsClient : ISmsClient
    {
        private readonly HttpClient _client;
        private readonly TwilioSmsCredentials _credentials;
        private const string TwilioSmsEndpointFormat = "https://api.twilio.com/2010-04-01/Accounts/{0}/Messages.json";

        public TwilioSmsClient(HttpClient client, TwilioSmsCredentials credentials)
        {
            _client = client;
            _credentials = credentials;
        }

        public async Task<bool> SendMessage(string toPhoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(toPhoneNumber))
            {
                throw new ArgumentException(nameof(toPhoneNumber));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException(nameof(message));
            }

            var keyValues = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("To", toPhoneNumber),
                new KeyValuePair<string, string>("From", _credentials.FromNumber),
                new KeyValuePair<string, string>("Body", message)
            };
            var content = new FormUrlEncodedContent(keyValues);
            var postUrl = string.Format(CultureInfo.InvariantCulture, TwilioSmsEndpointFormat, _credentials.AccountSid);
            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = content,
                RequestUri = new Uri(postUrl)
            };
            httpRequest.Headers.Add("User-Agent", "twilio-csharp/5.13.4 (.NET Framework 4.5.1+)");
            httpRequest.Headers.Add("Accept", "application/json");
            httpRequest.Headers.Add("Accept-Encoding", "utf-8");
            httpRequest.Headers.Add("Authorization",
                "Basic " + CreateBasicAuthenticationHeader(_credentials.AccountSid, _credentials.AuthToken));
            var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);
            try
            {
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new SmsException(json, ex);
            }
        }

        private string CreateBasicAuthenticationHeader(string username, string password)
        {
            var credentials = username + ":" + password;
            var encoded = System.Text.Encoding.UTF8.GetBytes(credentials);
            return Convert.ToBase64String(encoded);
        }
    }
}
