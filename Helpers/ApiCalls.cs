using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using wpfGMTraceability.Models;

namespace wpfGMTraceability.Helpers
{
    public class ApiCalls
    {
        public static async Task<StationData> GetStationDataAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var json = await client.GetStringAsync(SettingsManager.APILoadBOMUrl); // o la URL que uses
                    var data = JsonConvert.DeserializeObject<StationData>(json);
                    return data;
                }
            }
            catch (Exception Ex)
            {
                Console.WriteLine(Ex.Message.ToString());
                throw;
            }
        }
        public static async Task<(string content, int statusCode)> GetFromApiAsync(string url)
        {
            int statusCode = -1;
            try
            {
                using (var client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    string content = await response.Content.ReadAsStringAsync();
                    statusCode = (int)response.StatusCode;
                    return (content, statusCode);
                }
            }
            catch (HttpRequestException ex)
            {
                // Puedes loggear el error aquí si tienes un panel de errores
                return (null, statusCode);
            }
        }
        public static async Task<(string content, int statusCode)> PostAPIConsumeAsync(string Json)
        {
            int statusCode = -1;
            using (var client = new HttpClient())
            {
                var content = new StringContent(Json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(SettingsManager.APIConsumeSerialUrl, content);
                string responseContent = await response.Content.ReadAsStringAsync();

                statusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                {
                    string respuesta = await response.Content.ReadAsStringAsync();                    
                    return (respuesta, statusCode);
                }
                else
                {
                    var json = JObject.Parse(responseContent);
                    return (json["detail"]?.ToString(), statusCode);
                }
            }
        }
        public static async Task<(string content, int statusCode)> PostAPIRequestBoxAsync(string Json)
        {
            int statusCode = -1;
            using (var client = new HttpClient())
            {
                var content = new StringContent(Json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PostAsync(SettingsManager.APIRequestBoxUrl, content);

                string responseContent = await response.Content.ReadAsStringAsync();

                statusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                {
                    string respuesta = await response.Content.ReadAsStringAsync();
                    return (respuesta, statusCode);
                }
                else
                {
                    var json = JObject.Parse(responseContent);
                    return (json["detail"]?.ToString(), statusCode);
                }
            }
        }
        public static async Task<(string content, int statusCode)> PostAPIPASSInsert(string Json)
        {
            int statusCode = -1;
            using (var client = new HttpClient())
            {
                var content = new StringContent(Json, Encoding.UTF8, "application/json");

                //
                HttpResponseMessage response = await client.PostAsync(@"http://10.13.0.41:8842/insertvcdata/", content);
                string responseContent = await response.Content.ReadAsStringAsync();

                statusCode = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                {
                    string respuesta = await response.Content.ReadAsStringAsync();
                    return (respuesta, statusCode);
                }
                else
                {
                    var json = JObject.Parse(responseContent);
                    return (json["detail"]?.ToString(), statusCode);
                }
            }
        }
    }
}
