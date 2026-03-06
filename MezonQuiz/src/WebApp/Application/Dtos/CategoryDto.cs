using System.ComponentModel.DataAnnotations;

namespace WebApp.Application.Dtos
{
    public class CategoryDto : SaveCategoryDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveCategoryDto
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? Slug { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }

    }
}
