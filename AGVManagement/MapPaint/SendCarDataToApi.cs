using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static AGVManagement.MainWindow;

namespace AGVManagement.MapPaint
{
    public class SendCarDataToApi
    {
        private async Task SendCarDataToApiInter(CarData data)
        {
            var httpClient = new HttpClient();
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://localhost:<port>/api/CarData/update", content);
            response.EnsureSuccessStatusCode();
        }
    }
}
