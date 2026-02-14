using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WepApp.Models
{
    [Table("ButtonPermissions")]
    public class ButtonPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string KullaniciTipi { get; set; }

        [Required]
        public string SayfaAdi { get; set; }

        [Required]
        public string ButonAksiyonu { get; set; }

        public bool IzınVar { get; set; } = true;

        [StringLength(200)]
        public string Aciklama { get; set; }

        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        public DateTime? UpdatedDate { get; set; } = DateTime.Now;
    }
}