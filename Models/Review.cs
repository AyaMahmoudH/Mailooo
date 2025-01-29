using Mailo.Models;

namespace Mailoo.Models
{
    public class Review
    {
        public int Id { get; set; } 
        public int ProductId { get; set; } 

        public string UserName { get; set; } 
        public string Content { get; set; } 
        public int Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now; 

        public Product Product { get; set; }
    }
}
