using System.Collections;

namespace BookApi.Entity
{
    public class Cart : BaseEntity
    {
        public IEnumerable<Book> Books { get; set; }


    
    }
}
