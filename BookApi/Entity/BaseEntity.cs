namespace BookApi.Entity
{
    public class BaseEntity
    {
        public int Id { get; set; }
        public DateTime DateTimeCreate { get; set; }
        public DateTime? DateTimeModified { get; set; }
    }
}
