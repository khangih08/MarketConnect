using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MarketConnect.Data
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        public ICollection<Product>? Products { get; set; }
    }
}
