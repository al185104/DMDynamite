namespace DMDynamite.Models
{
    public class Activity
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public Guid SenderFK { get; set; }
        public Guid RecipientFK { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public bool IsSuccessful { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public bool IsDeleted { get; set; }
    }
}
