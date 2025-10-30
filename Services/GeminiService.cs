using Capstone.DTOs;
using Capstone.DTOs.Quizzes;
using Capstone.ENUMs;
using Capstone.Repositories;
using DocumentFormat.OpenXml.Packaging;
using System.Text;
using System.Text.Json;

namespace Capstone.Services
{
    public class GeminiService : IGemeniService
    {
        private readonly ILogger<GeminiService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public GeminiService(ILogger<GeminiService> logger, HttpClient httpClient, IConfiguration configuration)
        {
            _logger = logger;
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<QuestionDTO>> ConvertJsonToQuestion(string json, int time, int score)
        {
            var ListQuestion = JsonSerializer.Deserialize<GeminiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return new List<QuestionDTO>();
        }

        public ConvertTextEnum.ComvertText ConvertToText(IFormFile pdfFile)
        {
            try
            {
                if (pdfFile == null || pdfFile.Length == 0)
                {
                    _logger.LogInformation("File ");
                    return ConvertTextEnum.ComvertText.NULL;
                }
                var fileExtension = Path.GetExtension(pdfFile.FileName).ToLower();
                using (var stream = pdfFile.OpenReadStream())
                {
                    if (fileExtension == ".pdf")
                    {
                        return ConvertTextEnum.ComvertText.PDF;
                    }
                    else if (fileExtension == ".docx")
                    {
                        return ConvertTextEnum.ComvertText.DOCX;
                    }
                    else
                    {
                        return ConvertTextEnum.ComvertText.NOTSUPPORT;
                    }
                }
            }
            catch (Exception ex)
            {
                return ConvertTextEnum.ComvertText.ERROR;
            }
        }

        public async Task<string> ExtractTextFromPdf(Stream pdfStream)
        {
            using (var memory = new MemoryStream())
            {
                await pdfStream.CopyToAsync(memory);
                memory.Position = 0;

                var textBuilder = new StringBuilder();
                using (var document = UglyToad.PdfPig.PdfDocument.Open(memory))
                {
                    foreach (var page in document.GetPages())
                    {
                        textBuilder.AppendLine(page.Text);
                    }
                }

                return textBuilder.ToString();
            }
        }

        public async Task<string> ExtractTextFromWord(Stream wordStream)
        {
            using (var memory = new MemoryStream())
            {
                await wordStream.CopyToAsync(memory);
                memory.Position = 0;

                using (var doc = WordprocessingDocument.Open(memory, false))
                {
                    var body = doc.MainDocumentPart.Document.Body;
                    return body.InnerText;
                }
            }
        }

        public async Task<string> GenerateQuestions(string prompt)
        {
            try
            {
                var APIKEY = _configuration.GetSection("Gemini").GetValue<string>("APIKey");
                var URL = _configuration.GetSection("Gemini").GetValue<string>("Url");
                var GeminiUrl = APIKEY + URL;

                var requestData = new
                {
                    contents = new[]
                    {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
                };
                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(GeminiUrl, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();
                if (jsonResponse == null)
                {
                    return null;
                }
                return jsonResponse;
            }
            catch (Exception ex) {
                return null;
            }
        }
    }
}
