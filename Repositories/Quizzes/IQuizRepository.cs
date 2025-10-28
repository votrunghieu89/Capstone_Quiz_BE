using Capstone.DTOs.Quizzes;
using Capstone.Model;

namespace Capstone.Repositories.Quizzes
{
    public interface IQuizRepository
    {
        public Task<bool> CreateQuiz(QuizCreateDTo quiz, string IpAddress);
        public Task<string> DeleteQuiz(int quizId, int accountId, string IpAddress);
        public Task<QuizUpdateDTO> UpdateQuiz(QuizUpdateDTO quiz, string IpAddress, int accountId);
        public Task<bool> DeleteQuestion(int questionId);
        public Task<List<getQuizQuestionWithoutAnswerDTO>> GetAllQuestionEachQuiz(int quizId);
        public Task<RightAnswerDTO> getCorrectAnswer(GetCorrectAnswer getCorrectAnswer);
        public Task<bool> checkAnswer(CheckAnswerDTO checkAnswerDTO);
        public Task<ViewDetailDTO> getDetailOfAQuiz(int quizId); // detail quiz ở folder của giáo viên
        public Task<QuizDetailHPDTO> getDetailOfQuizHP(int quizId); // detail quiz ở homepage
        public Task<string> getOrlAvatarURL(int quizId);
        public Task<List<ViewAllQuizDTO>> getAllQuizzes(int page, int PageSize);
    }
}
