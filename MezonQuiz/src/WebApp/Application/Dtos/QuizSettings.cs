using System.ComponentModel.DataAnnotations;

namespace WebApp.Application.Dtos
{
    public class QuizSettings
    {
        public bool ShuffleQuestions { get; set; } = false;
        public bool ShuffleOptions { get; set; } = false;
        public bool ShowCorrectAnswer { get; set; } = true;
        [Range(1, 5, ErrorMessage = "Số lần làm bài phải từ 1 đến 5")]
        public int MaxAttempts { get; set; } = 1;

        public bool IsValid()
        {
            return MaxAttempts is >= 1 and <= 5;
        }
    }
}