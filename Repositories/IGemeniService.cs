using Capstone.DTOs;
using Capstone.DTOs.Quizzes;
using Capstone.ENUMs;
using Capstone.Model;
using System.Threading.Tasks;

namespace Capstone.Repositories
{
    public interface IGemeniService
    {
        public ConvertTextEnum.ComvertText ConvertToText(IFormFile pdfFile);
        public Task<string> ExtractTextFromPdf(Stream pdfStream);
        public Task<string> ExtractTextFromWord(Stream wordStream);
        public Task<string> GenerateQuestions(string text);
        public Task<List<QuestionDTO>> ConvertJsonToQuestion(string json, int time, int score);
    }
}
