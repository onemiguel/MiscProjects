using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace TestRestMethodWithAppId
{
    class Program
    {
        public class Options
        {
            [Option('i', "appid", Required = true, HelpText = "The Application Id to use")]
            public string AppId { get; set; }

            [Option('s', "secret", Required = true, HelpText = "Application Id secret string")]
            public string AppIdSecret { get; set; }

            [Option('u', "uri", Required = true, HelpText = "Target Rest URI")]
            public string TargetUri { get; set; }

            [Option('m', "method", Required = false, HelpText = "HTTP Method (i.e. get, post, etc) - Defaults to GET")]
            public string HttpMethod { get; set; } = "Get";

            [Option('b', "body", Required = false, HelpText = "Json string to send with a POST request (not used for other methods)")]
            public string Body { get; set; }

            [Option('t', "targetResource", Required = false, HelpText = "Target Resource - the resource to get the token for (defaults to the URI authority)")]
            public string TargetResource { get; set; }

            [Option('a', "authority", Required = false, HelpText = "Authority (defaults to https://login.windows.net/common)")]
            public string Authority { get; set; } = "https://login.windows.net/common";
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                   .WithParsed(o =>
                   {
                       CallRestApi(o);
                   });
        }

        private static async Task<AuthenticationResult> GetBearerTokenAuthHeaderAsync(string appId, string appIdSecret, string targetResource, string authority)
        {
            try
            {
                var clientCreds = new ClientCredential(appId, appIdSecret);
                var authenticationContext = new AuthenticationContext(authority, false);
                var r = await authenticationContext.AcquireTokenAsync(targetResource, clientCreds);
                WriteMessage($"Acquired Bearer Token for AppId {appId}", "Green");
                return r;
            } catch (Exception e)
            {
                WriteMessage($"Failed to get bearer token: {e.Message}", "Red");
                return null;
            }
        }

        private static void CallRestApi(Options options)
        {
            CallRestApiAsync(options).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static async Task CallRestApiAsync(Options options)
        {
            HttpClient httpClient = new HttpClient();
            HttpMethod httpMethod = new HttpMethod(options.HttpMethod);
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpMethod, options.TargetUri);
            
            if(string.IsNullOrWhiteSpace(options.TargetResource))
            {
                options.TargetResource = new Uri(options.TargetUri).Authority;
            }
            var bearerTokenResult = await GetBearerTokenAuthHeaderAsync(options.AppId, options.AppIdSecret, options.TargetResource, options.Authority);
            if (bearerTokenResult == null) return;
            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue(bearerTokenResult.AccessTokenType, bearerTokenResult.AccessToken);
            if (httpMethod == HttpMethod.Post)
            {
                httpRequestMessage.Content = new StringContent(options.Body, Encoding.UTF8, "application/json");
            }
            WriteMessage($"Sending request to {options.TargetUri}");
            try
            {
                var httpResponse = await httpClient.SendAsync(httpRequestMessage);

                string statusColor = "Green";
                if (false == httpResponse.IsSuccessStatusCode)
                {
                    statusColor = "Red";
                }
                        
                WriteMessage("\nResponse Headers");
                WriteMessage("================");
                WriteMessage($"Response Code: {(int)httpResponse.StatusCode} ({httpResponse.StatusCode})", statusColor);
                foreach (var header in httpResponse.Headers)
                {
                    WriteMessage($"{header.Key} = {string.Join(", ", header.Value)}", statusColor);
                }

                WriteMessage("\nContent Headers");
                WriteMessage("================");
                foreach (var header in httpResponse.Content?.Headers)
                {
                    WriteMessage($"{header.Key} = {string.Join(", ", header.Value)}", statusColor);
                }
                WriteMessage("\nResponse Content");
                WriteMessage("================");
                WriteMessage(await httpResponse.Content?.ReadAsStringAsync());

            } catch (Exception e)
            {
                string errorMessage;
                if (e.InnerException?.Message != null)
                {
                    errorMessage = e.InnerException.Message;
                } else
                {
                    errorMessage = e.Message;
                }
                WriteMessage($"Failed: {errorMessage}", "Red");
            }
        }

        private static void WriteMessage(string message, string color = null)
        {
            ConsoleColor currentConsoleColor = Console.ForegroundColor;
            
            if(Enum.TryParse(color, out ConsoleColor selectedColor))
            {
                Console.ForegroundColor = selectedColor;
            }
            Console.WriteLine(message);
            Console.ForegroundColor = currentConsoleColor;
        }
    }
}

