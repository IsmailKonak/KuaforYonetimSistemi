namespace WebProjeDeneme1.Models.Salonlar
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Salon
    {
        [Key] // Primary key
        public int SalonId { get; set; }

        [ForeignKey("Konum")] // Foreign key
        public int KonumId { get; set; }

        public Konum Konum { get; set; } // Navigation property

        [Required]
        public TimeSpan BaslangicSaat { get; set; } // TIME

        [Required]
        public TimeSpan BitisSaat { get; set; } // TIME
    }
}
