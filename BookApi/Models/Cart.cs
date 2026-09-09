using Bookstore.Api.Models.Base;

namespace Bookstore.Api.Models
{
    public class Cart : BaseEntity
    {
        public IEnumerable<Book>? Books { get; set; }


    
    }
}