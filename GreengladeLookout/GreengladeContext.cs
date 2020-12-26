using GreengladeLookout.Entities;
using Microsoft.EntityFrameworkCore;

#pragma warning disable CS8618
namespace GreengladeLookout
{
    public class GreengladeContext : DbContext
    {
        public GreengladeContext(DbContextOptions<GreengladeContext> options) : base(options)
        {
        }

        public DbSet<Guild> Guilds { get; set; }
    }
}
