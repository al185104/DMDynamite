namespace DMDynamite.Models
{
    public class RecipientAccount
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string Pk { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string ProfilePicture { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPrivate { get; set; }
        public int FollowersCount { get; set; }
        public string Status { get; set; }
        public SendingOption SendingOption { get; set; }
        public string SearchText { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
