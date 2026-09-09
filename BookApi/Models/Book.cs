using Bookstore.Api.Models.Base;
using System.ComponentModel.DataAnnotations;

namespace Bookstore.Api.Models
{
    public class Book : BaseEntity
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

    }
}
