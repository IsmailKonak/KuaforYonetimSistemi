using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Threading;
using System.Text.Json.Serialization;

namespace WebApplication.Controllers
{
    public class AIController : Controller
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _ailabApiKey = "B6UJLpnuX2Zf5GqH0DpP7aPWITj7hTdoDigxymzMSYkFYOCkr1w3vq4X3aVR65Km"; // Your AILabTools API key
        private readonly string _apiEndpoint = "https://www.ailabapi.com/api/portrait/effects/hairstyle-editor-pro";
        private readonly string _queryResultEndpoint = "https://www.ailabapi.com/api/common/query-async-task-result";

        public AIController(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestEt(string sacModeli, string sacRengi, IFormFile file, int image_size = 1)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.ErrorMessage = "Lütfen bir fotoğraf yükleyin.";
                return View("Index");
            }

            if (string.IsNullOrEmpty(sacModeli))
            {
                ViewBag.ErrorMessage = "Lütfen bir saç modeli seçin.";
                return View("Index");
            }

            string taskId = await SubmitImageForProcessing(sacModeli, sacRengi, file, image_size);

            if (string.IsNullOrEmpty(taskId))
            {
                ViewBag.ErrorMessage = "API isteği sırasında bir hata oluştu.";
                return View("Index");
            }

            string[] imageLinks = await PollForTaskResult(taskId);
            Console.WriteLine("imageLinks: " + imageLinks);

            if (imageLinks == null || imageLinks.Length == 0)
            {
                ViewBag.ErrorMessage = "Sonuçlar alınırken bir hata oluştu.";
                return View("Index");
            }

            ViewBag.SonucFotograflar = imageLinks;

            // Store the original image in TempData for display
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            TempData["YuklenenFotografBase64"] = Convert.ToBase64String(memoryStream.ToArray());

            return View("Index");
        }

        private async Task<string> SubmitImageForProcessing(string sacModeli, string sacRengi, IFormFile file, int image_size)
        {
            using var client = _clientFactory.CreateClient();
            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent("async"), "task_type");
            formData.Add(new StringContent("1"), "auto");
            formData.Add(new StringContent(sacModeli), "hair_style");
            if (!string.IsNullOrEmpty(sacRengi))
            {
                formData.Add(new StringContent(sacRengi), "color");
            }
            formData.Add(new StringContent(image_size.ToString()), "image_size");

            using var streamContent = new StreamContent(file.OpenReadStream());
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            formData.Add(streamContent, "image", file.FileName);

            client.DefaultRequestHeaders.Add("ailabapi-api-key", _ailabApiKey);

            HttpResponseMessage response = null;
            try
            {
                response = await client.PostAsync(_apiEndpoint, formData);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<InitialApiResponse>(responseContent);

                return jsonResponse?.TaskId;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"API İstek Hatası: {ex.Message}");
                if (response != null)
                {
                    Console.WriteLine($"Yanıt İçeriği: {await response.Content.ReadAsStringAsync()}");
                }
                return null;
            }
        }

        private async Task<string[]> PollForTaskResult(string taskId)
        {
            using var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("ailabapi-api-key", _ailabApiKey);

            int attempts = 0;
            while (attempts < 6) // Try for 30 seconds (6 attempts * 5 seconds)
            {
                attempts++;
                var queryUrl = $"{_queryResultEndpoint}?task_id={taskId}";
                var response = await client.GetAsync(queryUrl);
                Console.WriteLine(response);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("response.IsSuccessStatusCode");
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(responseContent);

                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("task_status", out JsonElement taskStatusElement) && taskStatusElement.GetInt32() == 2)
                        {
                            if (root.TryGetProperty("data", out JsonElement dataElement) && dataElement.TryGetProperty("images", out JsonElement imagesElement))
                            {
                                var imageLinks = imagesElement.EnumerateArray().Select(image => image.GetString()).ToArray();
                                return imageLinks;
                            }
                        }
                    }
                }

                await Task.Delay(5000); // Wait for 5 seconds
            }

            return null; // Task failed or timed out
        }

    }

    public class InitialApiResponse
    {
        [JsonPropertyName("task_type")]
        public string TaskType { get; set; }

        [JsonPropertyName("task_id")]
        public string TaskId { get; set; }
    }

    public class TaskResultResponse
    {
        [JsonPropertyName("error_code")]
        public int ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("task_status")]
        public int TaskStatus { get; set; }

        [JsonPropertyName("data")]
        public TaskData Data { get; set; }
    }

    public class TaskData
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("image_list")]
        public string[] ImageList { get; set; }
    }
}