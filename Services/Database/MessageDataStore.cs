namespace DMDynamite.Services.Database
{
    public class MessageDataStore : IMessageDataStore
    {
        public Task<bool> AddItemAsync(Message item)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteItemAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteItemsAsync(IEnumerable<string> accounts)
        {
            throw new NotImplementedException();
        }

        public Task<Message> GetItemAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Message>> GetItemsAsync(bool forceRefresh = false)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Message>> GetItemsByRangeAsync(int range)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateItemAsync(Message item)
        {
            throw new NotImplementedException();
        }
    }
}
