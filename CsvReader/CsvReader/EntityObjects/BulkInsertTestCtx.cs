using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvReader.EntityObjects
{
    public  class BulkInsertTestCtx : DbContext
    {
        public BulkInsertTestCtx()
            : base("BulkInsertTest")
        {
            Database.SetInitializer<BulkInsertTestCtx>(new BulkInsertTestInitializer());
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }

        public virtual DbSet<Address> Addresses { get; set; }
    }
}
