using System.Threading.Tasks;

namespace LearningWords.DAL
{
	public abstract class RepositoryBase
	{
		protected readonly WordsDbContext dbContext;

		public RepositoryBase(WordsDbContext dbContext)
		{
			this.dbContext = dbContext;
		}

		public async Task<int> SaveChangesAsync()
		{
			return await dbContext.SaveChangesAsync();
		}
	}
}
