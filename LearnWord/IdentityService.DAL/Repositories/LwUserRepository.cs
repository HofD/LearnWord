using IdentityService.DAL.Context;
using IdentityService.DAL.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityService.DAL.Repositories
{
    public class LwUserRepository
    {
        protected readonly IdentityContext dbContext;

        public LwUserRepository(IdentityContext dbContext)
        {
            this.dbContext = dbContext;
        }

        
    }
}
