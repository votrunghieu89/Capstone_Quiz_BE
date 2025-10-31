using Capstone.DTOs.Gemini;
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
            _logger.LogInformation("ConvertJsonToQuestion: Start - Time={Time}, Score={Score}", time, score);

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("ConvertJsonToQuestion: JSON string is null or empty");
                return new List<QuestionDTO>();
            }
                    
            List<QuestionDTO> questionDTOs = new List<QuestionDTO>();
            try
            {
                var ListQuestion = JsonSerializer.Deserialize<GeminiResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (ListQuestion == null)
                {
                    _logger.LogWarning("ConvertJsonToQuestion: Failed to deserialize JSON to GeminiResponse");
                    return new List<QuestionDTO>();
                }
                foreach (var questionJson in ListQuestion.questionResponses)
                {
                    QuestionDTO questionDTO = new QuestionDTO();
                    questionDTO.QuestionContent = questionJson.QuestionContent;
                    questionDTO.Score = score;
                    questionDTO.Time = time;
                    questionDTO.QuestionType = "MCQ";
                    questionDTO.Options = new List<OptionDTO>();
                    foreach (var option in questionJson.Options)
                    {
                        OptionDTO optionDTO = new OptionDTO();
                        optionDTO.OptionContent = option.OptionContent;
                        optionDTO.IsCorrect = option.IsCorrect;
                        questionDTO.Options.Add(optionDTO);
                    }
                    questionDTOs.Add(questionDTO);
                }
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

                _logger.LogInformation("GenerateQuestions: Sending request to Gemini API");
                var response = await _httpClient.PostAsync(GeminiUrl, content);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                if (jsonResponse == null)
                {
                    _logger.LogWarning("GenerateQuestions: Received null response from Gemini API");
                    return null;
                }

                _logger.LogInformation("GenerateQuestions: Success - Response length={Length}", jsonResponse.Length);
                return jsonResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateQuestions: Error calling Gemini API");
                return null;
            }
        }
    }
}
