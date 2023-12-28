using IdentityService.DAL.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.DAL.Context
{
    public class IdentityContext : IdentityDbContext<LwIdentityUser>
    {
        public IdentityContext(DbContextOptions options) : base(options) { }
    }
}
