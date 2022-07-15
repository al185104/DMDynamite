namespace DMDynamite.Models
{
    public class InstagramAccount
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string Pk { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string ProfilePicture { get; set; }
        public bool IsVerified { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime DateAdded { get; set; }
        public string Status { get; set; }
    }
}
