using Capstone.DTOs.Gemini;
using Capstone.DTOs.Quizzes;
using Capstone.ENUMs;
using Capstone.Repositories;
using DocumentFormat.OpenXml.Packaging;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

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

            // ✅ Đảm bảo HttpClient gửi và nhận UTF-8
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<QuestionDTO>> ConvertJsonToQuestion(string json, int time, int score)
        {
            _logger.LogInformation("ConvertJsonToQuestion: Start - Time={Time}, Score={Score}", time, score);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("ConvertJsonToQuestion: JSON string is null or empty");
                return new List<QuestionDTO>();
            }
         
            List<QuestionDTO> questionDTOs = new List<QuestionDTO>();
            try
            {
                Console.WriteLine("Step1");
                Console.WriteLine(json);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                };
                Console.WriteLine("Step2");
                var ListQuestion = JsonSerializer.Deserialize<GeminiResponse>(json, options);
                if (ListQuestion == null)
                {
                    _logger.LogWarning("ConvertJsonToQuestion: Failed to deserialize JSON to GeminiResponse");
                    return new List<QuestionDTO>();
                }
                Console.WriteLine(ListQuestion.ToString());
                Console.WriteLine("Step3");
                foreach (var item in ListQuestion.questionResponses) {
                    Console.WriteLine("Step3");
                }
                foreach (var questionJson in ListQuestion.questionResponses)
                {
                    QuestionDTO questionDTO = new QuestionDTO();
                    questionDTO.QuestionContent = questionJson.questionContent;
                    questionDTO.Score = score;
                    questionDTO.Time = time;
                    questionDTO.QuestionType = "MCQ";
                    questionDTO.Options = new List<OptionDTO>();
                    Console.WriteLine("Ste4");
                    foreach (var option in questionJson.options)
                    {
                        OptionDTO optionDTO = new OptionDTO();
                        optionDTO.OptionContent = option.OptionContent;
                        optionDTO.IsCorrect = option.IsCorrect;
                        questionDTO.Options.Add(optionDTO);
                    }
                    questionDTOs.Add(questionDTO);
                    Console.WriteLine("Step5");
                }
                Console.WriteLine("Step6");
                if (questionDTOs.Count > 0)
                {
                    _logger.LogInformation("ConvertJsonToQuestion: Success - Generated {Count} questions", questionDTOs.Count);
                    return questionDTOs;
                }
                else
                {
                    _logger.LogWarning("ConvertJsonToQuestion: No questions generated from JSON");
                    return new List<QuestionDTO>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Err");
                _logger.LogError(ex, "ConvertJsonToQuestion: Error deserializing or processing JSON");
                return new List<QuestionDTO>();
            }
        }

        public ConvertTextEnum.ComvertText ConvertToText(IFormFile pdfFile)
        {
            _logger.LogInformation("ConvertToText: Start - FileName={FileName}", pdfFile?.FileName);
            try
            {
                if (pdfFile == null || pdfFile.Length == 0)
                {
                    _logger.LogWarning("ConvertToText: File is null or empty");
                    return ConvertTextEnum.ComvertText.NULL;
                }
                var fileExtension = Path.GetExtension(pdfFile.FileName).ToLower();
                _logger.LogInformation("ConvertToText: File extension detected - Extension={Extension}", fileExtension);
                
                using (var stream = pdfFile.OpenReadStream())
                {
                    if (fileExtension == ".pdf")
                    {
                        _logger.LogInformation("ConvertToText: File type is PDF");
                        return ConvertTextEnum.ComvertText.PDF;
                    }
                    else if (fileExtension == ".docx")
                    {
                        _logger.LogInformation("ConvertToText: File type is DOCX");
                        return ConvertTextEnum.ComvertText.DOCX;
                    }
                    else
                    {
                        _logger.LogWarning("ConvertToText: File type not supported - Extension={Extension}", fileExtension);
                        return ConvertTextEnum.ComvertText.NOTSUPPORT;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConvertToText: Error processing file - FileName={FileName}", pdfFile?.FileName);
                return ConvertTextEnum.ComvertText.ERROR;
            }
        }

        public async Task<string> ExtractTextFromPdf(Stream pdfStream)
        {
            _logger.LogInformation("ExtractTextFromPdf: Start");
            try
            {
                using (var memory = new MemoryStream())
                {
                    await pdfStream.CopyToAsync(memory);
                    memory.Position = 0;

                    var textBuilder = new StringBuilder();
                    using (var document = UglyToad.PdfPig.PdfDocument.Open(memory))
                    {
                        _logger.LogInformation("ExtractTextFromPdf: Processing {PageCount} pages", document.NumberOfPages);
                        foreach (var page in document.GetPages())
                        {
                            textBuilder.AppendLine(page.Text);
                        }
                    }

                    var extractedText = textBuilder.ToString();
                    _logger.LogInformation("ExtractTextFromPdf: Success - Extracted {Length} characters", extractedText.Length);
                    return extractedText;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExtractTextFromPdf: Error extracting text from PDF");
                return null;
            }
        }

        public async Task<string> ExtractTextFromWord(Stream wordStream)
        {
            _logger.LogInformation("ExtractTextFromWord: Start");
            try
            {
                using (var memory = new MemoryStream())
                {
                    await wordStream.CopyToAsync(memory);
                    memory.Position = 0;

                    using (var doc = WordprocessingDocument.Open(memory, false))
                    {
                        var body = doc.MainDocumentPart.Document.Body;
                        var extractedText = body.InnerText;
                        _logger.LogInformation("ExtractTextFromWord: Success - Extracted {Length} characters", extractedText.Length);
                        return extractedText;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExtractTextFromWord: Error extracting text from Word document");
                return null;
            }
        }

        public async Task<string> GenerateQuestions(string prompt)
        {
            _logger.LogInformation("GenerateQuestions: Start - Prompt length={Length}", prompt?.Length ?? 0);
            try
            {
                var APIKEY = _configuration.GetSection("Gemini").GetValue<string>("APIKey");
                var URL = _configuration.GetSection("Gemini").GetValue<string>("Url");
                var GeminiUrl = $"{URL}{APIKEY}";

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

                // ✅ Serialize với Unicode encoder để không lỗi tiếng Việt
                var jsonBody = JsonSerializer.Serialize(requestData, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
                });

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                int maxRetries = 10;
                int delayMs = 2000;
                HttpResponseMessage response = null;

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    _logger.LogInformation("GenerateQuestions: Attempt {Attempt}", attempt);

                    try
                    {
                        response = await _httpClient.PostAsync(GeminiUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            // ✅ đọc UTF-8 đúng encoding
                            var jsonResponse = await response.Content.ReadAsStringAsync();
                            _logger.LogInformation("GenerateQuestions: Success - Response length={Length}", jsonResponse.Length);

                            using var doc = JsonDocument.Parse(jsonResponse);
                            var rawText = doc.RootElement
                                .GetProperty("candidates")[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString();

                            if (string.IsNullOrWhiteSpace(rawText))
                                return null;

                            // Làm sạch JSON output
                            string cleanJson = rawText
                                .Replace("```json", "")
                                .Replace("```", "")
                                .Trim();

                            _logger.LogInformation("GenerateQuestions: Clean JSON length={Length}", cleanJson.Length);
                            return cleanJson;
                        }

                        if (response.StatusCode == HttpStatusCode.TooManyRequests ||
                            response.StatusCode == HttpStatusCode.ServiceUnavailable)
                        {
                            _logger.LogWarning("Gemini overloaded, retrying in {Delay}ms", delayMs);
                            await Task.Delay(delayMs);
                            delayMs *= 2;
                            continue;
                        }

                        var errorBody = await response.Content.ReadAsStringAsync();
                        _logger.LogError("GenerateQuestions: API returned {StatusCode} - {Body}", response.StatusCode, errorBody);
                        return null;
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(ex, "GenerateQuestions: Network error, attempt {Attempt}", attempt);
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                    }
                }

                _logger.LogError("GenerateQuestions: Failed after {MaxRetries} attempts", maxRetries);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateQuestions: Unexpected error");
                return null;
            }
        }
    }
}
