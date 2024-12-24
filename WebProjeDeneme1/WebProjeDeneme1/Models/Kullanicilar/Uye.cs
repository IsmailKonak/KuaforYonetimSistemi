namespace WebProjeDeneme1.Models.Kullanicilar
{
    using System.ComponentModel.DataAnnotations;

    public class Uye
    {
        [Key]
        public int UyeId { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [StringLength(255, MinimumLength = 6)]
        public string Password { get; set; }
        public bool AdminMi { get; set; } = false; // Varsayılan değer atandı
    }
}
