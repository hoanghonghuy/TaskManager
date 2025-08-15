using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

// phần này dùng AI viết 
namespace TaskManager.Web.Services
{
    /// <summary>
    /// Lớp biểu diễn thông tin công việc được phân tích từ trợ lý AI.
    /// </summary>
    public class ParsedTaskInfo
    {
        public string? Title { get; set; }
        public string? DueDate { get; set; }
        public string? Priority { get; set; }
        public string[]? Tags { get; set; }
    }

    /// <summary>
    /// Lớp dịch vụ để giao tiếp với API Gemini AI.
    /// </summary>
    public class AIService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        // Sử dụng mô hình mới cho yêu cầu JSON
        private const string GeminiApiEndpointTemplate = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-05-20:generateContent?key={0}";

        public AIService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Phân tích văn bản đầu vào để trích xuất thông tin công việc bằng Gemini AI.
        /// </summary>
        /// <param name="inputText">Văn bản mô tả công việc từ người dùng.</param>
        /// <returns>Đối tượng ParsedTaskInfo chứa thông tin đã phân tích, hoặc null nếu có lỗi.</returns>
        public async Task<ParsedTaskInfo?> ParseTaskFromTextAsync(string inputText)
        {
            var apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                System.Diagnostics.Debug.WriteLine("Gemini API Key is not configured in User Secrets.");
                return null;
            }

            var apiEndpoint = string.Format(GeminiApiEndpointTemplate, apiKey);

            // sử dụng JSON schema để đảm bảo định dạng đầu ra.

            var requestBody = new JObject
            {
                ["contents"] = new JArray
                {
                    new JObject
                    {
                        ["parts"] = new JArray
                        {
                            new JObject { ["text"] = @$"
                                Phân tích câu sau để trích xuất thông tin cho một công việc: ""{inputText}""
                                
                                Dữ liệu phản hồi phải là một đối tượng JSON duy nhất và hợp lệ.
                                Đối tượng JSON phải có các trường: ""title"" (string), ""dueDate"" (string, định dạng ""YYYY-MM-DD HH:mm:ss"" hoặc null), ""priority"" (string, ""High"", ""Medium"", ""Low"", hoặc ""None""), ""tags"" (mảng các chuỗi).
                                Sử dụng {DateTime.UtcNow:yyyy-MM-dd} làm ngày hiện tại cho các thuật ngữ tương đối như 'ngày mai'.

                                Ví dụ Đầu vào: ""Nhắc tôi nộp báo cáo dự án cho sếp vào 5 giờ chiều ngày mai, việc này gấp đấy""
                                Ví dụ Đầu ra:
                                {{
                                    ""title"": ""Nộp báo cáo dự án cho sếp"",
                                    ""dueDate"": ""{DateTime.UtcNow.AddDays(1):yyyy-MM-dd} 17:00:00"",
                                    ""priority"": ""High"",
                                    ""tags"": [""báo cáo"", ""dự án""]
                                }}
                            " }
                        }
                    }
                },
                ["generationConfig"] = new JObject
                {
                    ["responseMimeType"] = "application/json",
                    ["responseSchema"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["title"] = new JObject { ["type"] = "string" },
                            ["dueDate"] = new JObject { ["type"] = "string" },
                            ["priority"] = new JObject { ["type"] = "string" },
                            ["tags"] = new JObject { ["type"] = "array", ["items"] = new JObject { ["type"] = "string" } }
                        }
                    }
                }
            };

            var content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(apiEndpoint, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    System.Diagnostics.Debug.WriteLine($"Gemini API Error ({response.StatusCode}): {jsonResponse}");
                    return null;
                }

                var jObject = JObject.Parse(jsonResponse);
                var jsonResult = jObject.SelectToken("candidates[0].content.parts[0].text")?.ToString();

                if (string.IsNullOrEmpty(jsonResult))
                {
                    System.Diagnostics.Debug.WriteLine("Gemini API response did not contain the expected JSON text part.");
                    return null;
                }

                return JsonConvert.DeserializeObject<ParsedTaskInfo>(jsonResult);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception calling Gemini API: {ex.Message}");
                return null;
            }
        }
    }
}
