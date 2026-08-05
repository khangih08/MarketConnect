using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class ProductMarket
    {
        [ForeignKey(nameof(Market))]
        public int MarketId { get; set; }

        public Market? Market { get; set; }

        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
