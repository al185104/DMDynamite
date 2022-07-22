namespace DMDynamite.Models
{
    public class ProxySetup
    {
        [PrimaryKey]
        public Guid Id { get; set; }
        public string ProxyName { get; set; }
        public string ProxyHost { get; set; }
        public string ProxyPort { get; set; }
        public bool IsUsed { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
