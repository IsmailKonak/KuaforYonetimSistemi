namespace WebProjeDeneme1.Models.Randevular
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class Randevu
    {
        [Key]
        public int RandevuId { get; set; }
        [ForeignKey("Salon")]
        public int SalonId { get; set; }
        public Salonlar.Salon Salon { get; set; }
        [ForeignKey("Personel")]
        public int PersonelId { get; set; }
        public Personeller.Personel Personel { get; set; }
        [ForeignKey("Uye")]
        public int UyeId { get; set; }
        public Kullanicilar.Uye Uye { get; set; }
        [ForeignKey("YapilabilenIslem")]
        public int IslemId { get; set; }
        public Uzmanlik.YapilabilenIslem Islem { get; set; }
        [Required]
        public DateTime Gun { get; set; }
        [Required]
        public TimeSpan Saat { get; set; }
        public bool Onaylandi { get; set; } = false; // Varsayılan değer atandı
    }
}
