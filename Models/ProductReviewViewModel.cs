namespace Mailoo.Models
{
    public class ProductReviewViewModel
    {
        public int ProductId { get; set; } 
        public string UserName { get; set; } 
        public string Content { get; set; } 
        public int Rating { get; set; } 
        public List<Review> Reviews { get; set; }
    }
}
