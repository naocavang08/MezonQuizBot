using System.ComponentModel.DataAnnotations;

namespace WebApp.Application.ManageQuiz.Dtos
{
    public class QuizSettings
    {
        public bool ShuffleQuestions { get; set; } = false;
        public bool ShuffleOptions { get; set; } = false;
        public bool ShowCorrectAnswer { get; set; } = true;
    }
}