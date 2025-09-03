using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using wpfGMTraceability.Models;

namespace wpfGMTraceability.Helpers
{
    public class ApiCheckSerialService
    {
        private static readonly HttpClient client = new HttpClient();
        public static async Task<(string content, int statusCode)> GetFromApiAsync(string url)
        {
            int statusCode = -1;
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                string content = await response.Content.ReadAsStringAsync();
                statusCode = (int)response.StatusCode;

                return (content, statusCode);
            }
            catch (HttpRequestException ex)
            {
                // Puedes loggear el error aquí si tienes un panel de errores
                return (null, statusCode);
            }
        }
    }
}
