    using Bookstore.Api.Models.Base;

    namespace Bookstore.Api.Models
    {
        public class User : BaseEntity
        {
            // User name
            public string Name { get; set; } = string.Empty;

            // User email
            public string Email { get; set; } = string.Empty;

            // User password
            public string Password { get; set; } = string.Empty;

            // User cart
            public Cart? Cart { get; set; }

            // User orders
            public IEnumerable<Order>? Orders { get; set; }

        }
    }
