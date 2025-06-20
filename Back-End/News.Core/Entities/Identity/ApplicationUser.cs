using News.Core.Entities.NewsCategory;

namespace News.Core.Entities.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePicUrl { get; set; }
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Survey> Surveys { get; set; } = new List<Survey>();
        public DateTime? DeletionRequestedAt { get; set; }
        public bool IsPendingDeletion { get; set; }
    }
}
