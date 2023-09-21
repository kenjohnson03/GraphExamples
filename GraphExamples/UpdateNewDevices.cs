using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Xml.Linq;

namespace GraphExamples
{
    public class UpdateNewDevices
    {
        private static ILogger _logger;
        private static string _guidRegex = "^([0-9A-Fa-f]{8}[-]?[0-9A-Fa-f]{4}[-]?[0-9A-Fa-f]{4}[-]?[0-9A-Fa-f]{4}[-]?[0-9A-Fa-f]{12})$";

        [FunctionName("UpdateNewDevices")]
        public async Task Run([TimerTrigger("0 5 * * * *")] TimerInfo myTimer, ILogger log, IConfiguration configuration)
        {
            MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
            string methodName = method.Name;
            string className = method.ReflectedType.Name;
            string fullMethodName = className + "." + methodName;

            _logger = log;

            _logger.LogInformation($"C# Timer trigger function {fullMethodName} executed at: {DateTime.Now}");
            var AUUpdates = Environment.GetEnvironmentVariable("AUUpdates", EnvironmentVariableTarget.Process);

            if (String.IsNullOrEmpty(AUUpdates))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{fullMethodName} Error: Missing required environment variables. Please check the following environment variables are set:");
                sb.Append(String.IsNullOrEmpty(AUUpdates) ? "AUUpdates\n" : "");
                
                _logger.LogError(sb.ToString());
                return;
            }

            List<GraphExamples.Models.Graph.ManagedDevice> devices = null;
            try
            {
                devices = await GetNewDeviceManagementObjectsAsync((DateTime.UtcNow.AddHours(-2)));
            }            
            catch (Exception ex)
            {
                _logger.LogError($"{fullMethodName} Error retrieving devices: \n{ex.Message}");
                return;
            }

            if (devices == null)
            {
                _logger.LogError($"{fullMethodName} Error: Failed to get new devices, exiting");
                return;
            }

            string[] entries = AUUpdates.Split(';');
            foreach (string entry in entries)
            {
                string[] entryParts = entry.Split('=');
                if (entryParts.Length != 2)
                {
                    _logger.LogError($"{fullMethodName} Error: Invalid AUUpdates entry: {entry}");
                    continue;
                }

                string deviceName = entryParts[0];
                string auId = entryParts[1];
                List<GraphExamples.Models.Graph.ManagedDevice> filteredDevices = devices.FindAll(x => x.deviceName.Contains(deviceName));
                foreach(GraphExamples.Models.Graph.ManagedDevice device in filteredDevices)
                {
                    try
                    {
                        await AddDeviceToAzureAdministrativeUnit(device.azureADDeviceId, auId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"{fullMethodName} Error adding Device Id {device.azureADDeviceId} to Administrative Unit: {auId}\nError : {ex.Message}");
                    }
                    
                }
            }
        }

        private static async Task<List<GraphExamples.Models.Graph.ManagedDevice>> GetNewDeviceManagementObjectsAsync(DateTime dateTime)
        {
            MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
            string methodName = method.Name;
            string className = method.ReflectedType.Name;
            string fullMethodName = className + "." + methodName;

            var TargetCloud = Environment.GetEnvironmentVariable("AzureEnvironment", EnvironmentVariableTarget.Process);

            string tokenUri = "";

            if (TargetCloud == "AzureUSDoD")
            {
                tokenUri = "https://dod-graph.microsoft.us";
            }
            else if (TargetCloud == "AzureUSGovernment")
            {
                tokenUri = "https://graph.microsoft.us";
            }
            else
            {
                tokenUri = "https://graph.microsoft.com";
            }

            var graphAccessToken = await GetAccessTokenAsync(tokenUri);
            if (graphAccessToken == null)
            {
                _logger.LogError($"{fullMethodName} Error: Failed to get access token");
                return null;
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
            List<GraphExamples.Models.Graph.ManagedDevice> devices = new List<GraphExamples.Models.Graph.ManagedDevice>();

            try
            {
                string uri = $"{tokenUri}/v1.0/deviceManagement/managedDevices?$filter=enrolledDateTime ge {dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")}&$select=id,name,manufacturer,model,serialNumber,azureADDeviceId";
                while (!string.IsNullOrEmpty(uri))
                {
                    var graphResponse = await httpClient.GetAsync(uri);
                    graphResponse.EnsureSuccessStatusCode();
                    var graphContent = await graphResponse.Content.ReadAsStringAsync();
                    GraphExamples.Models.Graph.ManagedDevices deviceResponse = JsonConvert.DeserializeObject<GraphExamples.Models.Graph.ManagedDevices>(graphContent);

                    devices.AddRange(deviceResponse.value);
                    uri = deviceResponse.odataNextLink;
                }

                _logger.LogInformation($"{fullMethodName} Intune device information\nFound {devices.Count} new devices since {dateTime} UTC");
            }
            catch (Exception ex)
            {
                _logger.LogError($"{fullMethodName} Error: {ex.Message}");
            }

            return devices;
        }

        private static async Task AddDeviceToAzureADGroup(string deviceId, string groupId)
        {
            MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
            string methodName = method.Name;
            string className = method.ReflectedType.Name;
            string fullMethodName = className + "." + methodName;

            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(groupId))
            {
                _logger.LogError($"{fullMethodName} Error: DeviceId or GroupId is null or empty. DeviceId: {deviceId} GroupId: {groupId}");
                return;
            }
            if (Regex.IsMatch(deviceId, _guidRegex) == false)
            {
                _logger.LogError($"{fullMethodName} Error: DeviceId is not a valid GUID. DeviceId: {deviceId}");
                return;
            }
            if (Regex.IsMatch(groupId, _guidRegex) == false)
            {
                _logger.LogError($"{fullMethodName} Error: GroupId is not a valid GUID. GroupId: {groupId}");
                return;
            }

            var TargetCloud = Environment.GetEnvironmentVariable("AzureEnvironment", EnvironmentVariableTarget.Process);

            string tokenUri = "";
            if (TargetCloud == "AzureUSDoD")
            {
                tokenUri = "https://dod-graph.microsoft.us";
            }
            else if (TargetCloud == "AzureUSGovernment")
            {
                tokenUri = "https://graph.microsoft.us";
            }
            else
            {
                tokenUri = "https://graph.microsoft.com";
            }


            try
            {

                var graphAccessToken = await GetAccessTokenAsync(tokenUri);
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);

                string deviceADRequestUri = $"{tokenUri}/v1.0/devices?$filter=deviceId eq '{deviceId}'&$select=id";
                var deviceADRequest = new HttpRequestMessage(HttpMethod.Get, deviceADRequestUri);
                deviceADRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
                HttpResponseMessage deviceADResponse = await httpClient.SendAsync(deviceADRequest);
                deviceADResponse.EnsureSuccessStatusCode();
                string deviceADResponseContent = await deviceADResponse.Content.ReadAsStringAsync();
                Regex regex = new Regex(@"\b[A-Fa-f0-9]{8}(?:-[A-Fa-f0-9]{4}){3}-[A-Fa-f0-9]{12}\b");
                string deviceAzureAdObjectId = "";
                Match deviceMatch = regex.Match(deviceADResponseContent);
                if (deviceMatch.Success)
                {
                    deviceAzureAdObjectId = deviceMatch.Value;
                }
                else
                {
                    _logger.LogError($"{fullMethodName} Error: Could not find Azure AD Object Id for device {deviceId}");
                    return;
                }

                var groupRequest = new HttpRequestMessage(HttpMethod.Post, $"{tokenUri}/v1.0/groups/{groupId}/members/$ref");
                groupRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);

                var values = new Dictionary<string, string>{
                    { "@odata.id", $"{tokenUri}/v1.0/directoryObjects/{deviceAzureAdObjectId}" }
                };

                var json = JsonConvert.SerializeObject(values, Formatting.Indented);

                var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");

                _logger.LogInformation($"{fullMethodName} Adding Device:{deviceAzureAdObjectId} to Group:{groupId}");

                stringContent.Headers.Remove("Content-Type");
                stringContent.Headers.Add("Content-Type", "application/json");
                groupRequest.Content = stringContent;

                var groupResponse = await httpClient.SendAsync(groupRequest);
                if (groupResponse.StatusCode == System.Net.HttpStatusCode.OK || groupResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    _logger.LogInformation($"{fullMethodName} Device:{deviceAzureAdObjectId} added to Group:{groupId}");
                }
                else if (groupResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    string groupResponseContent = await groupResponse.Content.ReadAsStringAsync();
                    if (groupResponseContent.Contains("One or more added object references already exist for the following modified properties: 'members'."))
                    {
                        _logger.LogInformation($"{fullMethodName} Device:{deviceAzureAdObjectId} already added to Group:{groupId}");
                    }
                    else
                    {
                        _logger.LogError($"{fullMethodName} Error: Device:{deviceAzureAdObjectId} not added to Group:{groupId}\n{await groupResponse.Content.ReadAsStringAsync()}");
                    }
                }
                else
                {
                    _logger.LogError($"{fullMethodName} Error: Device:{deviceId} not added to Group:{groupId}\n{await groupResponse.Content.ReadAsStringAsync()}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{fullMethodName} Error: {ex.Message}");
            }
        }

        private static async Task AddDeviceToAzureAdministrativeUnit(string deviceId, string auId)
        {
            MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
            string methodName = method.Name;
            string className = method.ReflectedType.Name;
            string fullMethodName = className + "." + methodName;

            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(auId))
            {
                _logger.LogError($"{fullMethodName} Error: DeviceId or AU Id is null or empty. DeviceId: {deviceId} AU Id: {auId}");
                return;
            }
            if (Regex.IsMatch(deviceId, _guidRegex) == false)
            {
                _logger.LogError($"{fullMethodName} Error: DeviceId is not a valid GUID. DeviceId: {deviceId}");
                return;
            }
            if (Regex.IsMatch(auId, _guidRegex) == false)
            {
                _logger.LogError($"{fullMethodName} Error: AU Id is not a valid GUID. AU Id: {auId}");
                return;
            }

            var TargetCloud = Environment.GetEnvironmentVariable("AzureEnvironment", EnvironmentVariableTarget.Process);

            string tokenUri = "";
            if (TargetCloud == "AzureUSDoD")
            {
                tokenUri = "https://dod-graph.microsoft.us";
            }
            else if (TargetCloud == "AzureUSGovernment")
            {
                tokenUri = "https://graph.microsoft.us";
            }
            else
            {
                tokenUri = "https://graph.microsoft.com";
            }

            var graphAccessToken = await GetAccessTokenAsync(tokenUri);
            if (graphAccessToken == null)
            {
                _logger.LogError($"{fullMethodName} Error: Failed to get Graph Access Token");
                return;
            }

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);

            string deviceADRequestUri = $"{tokenUri}/v1.0/devices?$filter=deviceId eq '{deviceId}'&$select=id";
            var deviceADRequest = new HttpRequestMessage(HttpMethod.Get, deviceADRequestUri);
            deviceADRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
            HttpResponseMessage deviceADResponse = await httpClient.SendAsync(deviceADRequest);
            deviceADResponse.EnsureSuccessStatusCode();
            string deviceADResponseContent = await deviceADResponse.Content.ReadAsStringAsync();
            Regex regex = new Regex(@"\b[A-Fa-f0-9]{8}(?:-[A-Fa-f0-9]{4}){3}-[A-Fa-f0-9]{12}\b");
            string deviceAzureAdObjectId = "";
            Match deviceMatch = regex.Match(deviceADResponseContent);
            if (deviceMatch.Success)
            {
                deviceAzureAdObjectId = deviceMatch.Value;
            }
            else
            {
                _logger.LogError($"{fullMethodName} Error: Could not find Azure AD Object Id for device {deviceId}");
                return;
            }

            var groupRequest = new HttpRequestMessage(HttpMethod.Post, $"{tokenUri}/v1.0/directory/administrativeUnits/{auId}/members/$ref");
            groupRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);

            var values = new Dictionary<string, string>{
                { "@odata.id", $"{tokenUri}/v1.0/devices/{deviceAzureAdObjectId}" }
            };

            var json = JsonConvert.SerializeObject(values, Formatting.Indented);

            var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");

            _logger.LogInformation($"Adding Device:{deviceAzureAdObjectId} to Administrative Unit:{auId}");

            stringContent.Headers.Remove("Content-Type");
            stringContent.Headers.Add("Content-Type", "application/json");
            groupRequest.Content = stringContent;

            var groupResponse = await httpClient.SendAsync(groupRequest);
            string response = await groupResponse.Content.ReadAsStringAsync();
            if (groupResponse.StatusCode == System.Net.HttpStatusCode.OK || groupResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                _logger.LogInformation($"Device:{deviceId} added to Administrative Unit:{auId}");
            }
            else if (groupResponse.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                if (response.Contains("A conflicting object with one or more of the specified property values is present in the directory"))
                {
                    _logger.LogInformation($"Device:{deviceId} already exists in Administrative Unit:{auId}");
                }
                else
                {
                    _logger.LogError($"Error: Device:{deviceId} not added to Administrative Unit:{auId}\n{response}");
                }
            }
            else
            {
                _logger.LogError($"Error: Device:{deviceId} not added to Administrative Unit:{auId}\n{response}");
            }
            response = String.IsNullOrEmpty(response) ? "Success" : response;
            _logger.LogInformation($"Administrative Unit Add Response: {response}");
        }


        private static async Task<String> GetAccessTokenAsync(string uri)
        {
            MethodBase method = System.Reflection.MethodBase.GetCurrentMethod();
            string methodName = method.Name;
            string className = method.ReflectedType.Name;
            string fullMethodName = className + "." + methodName;

            var AppSecret = Environment.GetEnvironmentVariable("AzureApp:ClientSecret", EnvironmentVariableTarget.Process);
            var AppId = Environment.GetEnvironmentVariable("AzureAd:ClientId", EnvironmentVariableTarget.Process);
            var TenantId = Environment.GetEnvironmentVariable("AzureAd:TenantId", EnvironmentVariableTarget.Process);
            var TargetCloud = Environment.GetEnvironmentVariable("AzureEnvironment", EnvironmentVariableTarget.Process);

            if (String.IsNullOrEmpty(AppSecret) || String.IsNullOrEmpty(AppId) || String.IsNullOrEmpty(TenantId) || String.IsNullOrEmpty(TargetCloud))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"{fullMethodName} Error: Missing required environment variables. Please check the following environment variables are set:");
                sb.Append(String.IsNullOrEmpty(AppSecret) ? "AzureApp:ClientSecret\n" : "");
                sb.Append(String.IsNullOrEmpty(AppId) ? "AzureAd:ClientId\n" : "");
                sb.Append(String.IsNullOrEmpty(TenantId) ? "AzureAd:TenantId\n" : "");
                sb.Append(String.IsNullOrEmpty(TargetCloud) ? "AzureEnvironment\n" : "");
                _logger.LogError(sb.ToString());
                return null;
            }

            string tokenUri = "";

            if (TargetCloud == "AzureUSGovernment" || TargetCloud == "AzureUSDoD")
            {
                tokenUri = $"https://login.microsoftonline.us/{TenantId}/oauth2/token";
            }
            else
            {
                tokenUri = $"https://login.microsoftonline.com/{TenantId}/oauth2/token";
            }

            // Get token for Log Analytics

            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUri);
            tokenRequest.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", AppId),
                new KeyValuePair<string, string>("client_secret", AppSecret),
                new KeyValuePair<string, string>("resource", uri)
            });

            try
            {
                var httpClient = new HttpClient();
                var tokenResponse = await httpClient.SendAsync(tokenRequest);
                var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenData = JsonConvert.DeserializeObject<dynamic>(tokenContent);
                return tokenData.access_token;
            }
            catch (Exception ex)
            {
                _logger.LogError($"{fullMethodName} Error: getting access token for URI {tokenUri}: {ex.Message}");
                return null;
            }
        }
    }
}

