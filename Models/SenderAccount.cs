namespace DMDynamite.Models
{
    public class SenderAccount
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public bool HasIssue { get; set; }
        public string ChallengeURL { get; set; }
        public string Status { get; set; }
        public Guid ProxyFK { get; set; }

        public string Username { get; set; }
        public string Password { get; set; }
        public string ProfilePicture { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingsCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
