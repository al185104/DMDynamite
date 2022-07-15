namespace DMDynamite.Models
{
    public class Activity
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public Guid SenderFK { get; set; }
        public Guid RecipientFK { get; set; }
        public Guid MessageFK { get; set; }
        public SendingOption SendingOption { get; set; }
        public string Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}
