using Capstone.DTOs.Quizzes;
using Capstone.Model;

namespace Capstone.Repositories.Quizzes
{
    public interface IQuizRepository
    {
        public Task<bool> CreateQuiz(QuizCreateDTo quiz);
        public Task<bool> DeleteQuiz(int quizId);
        public Task<QuizUpdateDTO> UpdateQuiz(QuizUpdateDTO quiz);
        public Task<bool> DeleteQuestion(int questionId);

        public Task<List<getQuizQuestionWithoutAnswerDTO>> GetQuizQuestions(int quizId);

        public Task<RightAnswerDTO> getCorrectAnswer(GetCorrectAnswer getCorrectAnswer);
        public Task<bool> checkAnswer(CheckAnswerDTO checkAnswerDTO);
        public Task<ViewDetailDTO> getDetailOfAQuizforTeacher(int quizId);
        public Task<string> getOrlAvatarURL(int quizId);

        public Task<List<ViewAllQuizDTO>> getAllQuizzes(int page, int PageSize);
    }
}
