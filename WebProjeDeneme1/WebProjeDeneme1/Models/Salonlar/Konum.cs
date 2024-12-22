namespace WebProjeDeneme1.Models.Salonlar
{
    using System.ComponentModel.DataAnnotations;

    public class Konum
    {
        [Key] // Primary key
        public int KonumId { get; set; }

        [Required] // NOT NULL constraint
        [StringLength(255)] // VARCHAR(255)
        public string KonumAdi { get; set; }
    }
}
