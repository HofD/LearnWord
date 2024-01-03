using LearnWord.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearnWord.DAL.Configurations
{
    public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
    {
        public void Configure(EntityTypeBuilder<Collection> builder)
        {
            builder.Property(x => x.Name).HasMaxLength(512);
        }
    }
}
