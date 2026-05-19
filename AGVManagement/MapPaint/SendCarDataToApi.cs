using AGVManagement.Models;   
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AGVManagement.MapPaint
{
    /// <summary>将 CarData 推送到外部 API</summary>
    public class SendCarDataToApi
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>POST CarData 到指定接口</summary>
        public async Task SendAsync(CarData data, string apiUrl)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync(apiUrl, content);
            resp.EnsureSuccessStatusCode();
        }
    }
}