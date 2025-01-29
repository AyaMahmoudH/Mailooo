using Mailo.Models;

namespace Mailoo.Models
{
    public class ProductVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int ColorId { get; set; }
        public Color Color { get; set; }

        public int SizeId { get; set; }
        public Size Size { get; set; }

        public int Quantity { get; set; }
        public ICollection<OrderProduct>? OrderProducts { get; set; }

    }
}
