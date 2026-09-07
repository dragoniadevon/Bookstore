using System.ComponentModel.DataAnnotations;

namespace BookApi.Entity
{
    public class Book : BaseEntity
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

    }
}
