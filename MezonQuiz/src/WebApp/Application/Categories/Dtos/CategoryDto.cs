using System.ComponentModel.DataAnnotations;

namespace WebApp.Application.Categories.Dtos
{
    public class CategoryDto : SaveCategoryDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SaveCategoryDto
    {
        public string Name { get; set; } = null!;
        public string? Slug { get; set; }
        public string? Icon { get; set; }
        public int SortOrder { get; set; }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new ArgumentException("Category name is required.", nameof(Name));
            }
            else if (SortOrder < 0)
            {
                throw new ArgumentException("Category sort order must be non-negative.", nameof(SortOrder));
            }
        }

    }
}
