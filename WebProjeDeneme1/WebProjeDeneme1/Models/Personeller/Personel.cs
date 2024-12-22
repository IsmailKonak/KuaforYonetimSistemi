namespace WebProjeDeneme1.Models.Personeller
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Personel
    {
        [Key]
        public int PersonelId { get; set; }

        [ForeignKey("Salon")]
        public int SalonId { get; set; }

        public Salonlar.Salon Salon { get; set; } // Navigation property

        [Required]
        public List<int> UzmanlikAlanlari { get; set; } // Uzmanlık alanları

        [Required]
        public TimeSpan BaslangicSaat { get; set; }

        [Required]
        public TimeSpan BitisSaat { get; set; }
    }
}
