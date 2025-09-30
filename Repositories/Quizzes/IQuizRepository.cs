using Capstone.DTOs.Quizzes;
using Capstone.Model;

namespace Capstone.Repositories.Quizzes
{
    public interface IQuizRepository
    {
        public Task<bool> CreateQuiz(QuizModel quiz);
        public Task<bool> DeleteQuiz(int quizId);
        public Task<bool> UpdateQuiz(QuizModel quiz);
        public Task<bool> DeleteQuestion(int questionId);

        public Task<List<GetQuizQuestionsDTO>> GetQuizQuestions(int quizId);

        public Task<RightAnswerDTO> getCorrectAnswer(GetCorrectAnswer getCorrectAnswer);
        public Task<bool> checkAnswer(CheckAnswerDTO checkAnswerDTO);
    }
}
