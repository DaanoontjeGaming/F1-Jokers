using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace F1Jokers.Models
{
    [Table("Puntenberekening Poule")]
    public class PuntenParameter
    {
        [Key]
        public int PuntenID { get; set; }
        public string Parameter { get; set; }
        public int Waarde { get; set; }
    }
}