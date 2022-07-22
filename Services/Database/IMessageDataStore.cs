namespace DMDynamite.Services.Database
{
    public interface IMessageDataStore : IDataStore<Message>
    {
        Task<IEnumerable<Message>> GetItemRandomAsync();
    }
}
