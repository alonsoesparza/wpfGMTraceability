using Newtonsoft.Json;
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
            using (var client = new HttpClient()) {
                var json = await client.GetStringAsync(SettingsManager.APILoadBOMUrl); // o la URL que uses
                var data = JsonConvert.DeserializeObject<StationData>(json);
                return data;
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

                if (response.IsSuccessStatusCode)
                {
                    string respuesta = await response.Content.ReadAsStringAsync();
                    statusCode = (int)response.StatusCode;
                    return (respuesta, statusCode);
                }
                else
                {
                    return (null, statusCode);
                }
            }
        }
    }
}
