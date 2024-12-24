namespace WebProjeDeneme1.Models.Uzmanlik
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class YapilabilenIslem
    {
        [Key]
        public int YapilabilenIslemlerId { get; set; }

        [ForeignKey("UzmanlikAlani")]
        public int UzmanlikAlaniId { get; set; }

        public UzmanlikAlani UzmanlikAlani { get; set; } // Navigation property

        [Required]
        [StringLength(255)]
        public string IslemAdi { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "İşlem ücreti negatif olamaz.")]
        [Column(TypeName = "decimal(10,2)")] // NUMERIC(10,2)
        public decimal IslemUcreti { get; set; }

        [Required]
        public TimeSpan IslemSuresi { get; set; } // TIME
    }
}