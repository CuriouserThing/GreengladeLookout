using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GreengladeLookout.Entities
{
    public class Guild
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public string? CommandPrefix { get; set; }

        public string? Locale { get; set; }
    }
}
