using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MarketConnect.Data
{
    public class Province
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string Code { get; set; } = null!;

        public ICollection<District>? Districts { get; set; }
    }

    public class District
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Province))]
        public int ProvinceId { get; set; }
        public Province? Province { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string Code { get; set; } = null!;

        public ICollection<Ward>? Wards { get; set; }
        public ICollection<Market>? Markets { get; set; }
    }

    public class Ward
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(District))]
        public int DistrictId { get; set; }
        public District? District { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string Code { get; set; } = null!;

        public ICollection<Market>? Markets { get; set; }
    }
}
