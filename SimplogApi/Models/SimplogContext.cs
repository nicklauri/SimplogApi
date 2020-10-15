using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SimplogApi.Models
{
    public class SimplogContext : DbContext
    {
        public SimplogContext(DbContextOptions<SimplogContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
    }
}
