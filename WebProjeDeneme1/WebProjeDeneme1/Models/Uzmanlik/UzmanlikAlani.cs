namespace WebProjeDeneme1.Models.Uzmanlik
{
    using System.ComponentModel.DataAnnotations;

    public class UzmanlikAlani
    {
        [Key]
        public int UzmanlikAlaniId { get; set; }

        [Required]
        [StringLength(255)]
        public string UzmanlikAdi { get; set; }
    }
}
