namespace DMDynamite.Services.Database
{
    public interface ISenderDataStore : IDataStore<SenderAccount>
    {
        Task<SenderAccount> GetItemByUsernameAsync(string name);
    }
}
