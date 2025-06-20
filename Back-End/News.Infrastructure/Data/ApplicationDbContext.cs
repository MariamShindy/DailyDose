using News.Core.Entities.NewsCatcher;

namespace News.Infrastructure.Data
{
    public class ApplicationDbContext :IdentityDbContext<ApplicationUser>
	{
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
		{

		}
		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			var adminRoleId = Guid.NewGuid().ToString();
			var userRoleId = Guid.NewGuid().ToString();
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = adminRoleId,
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                },
                new IdentityRole
                {
                    Id = userRoleId,
                    Name = "User",
                    NormalizedName = "USER"
                }
            );
        }

		public DbSet<Comment> Comments { get; set; }
		public DbSet<UserFavoriteArticle> UserFavoriteArticles { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Survey> Surveys { get; set; }

    }
}
