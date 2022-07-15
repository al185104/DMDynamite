namespace DMDynamite.Services.Database
{
    public interface IRecipientDataStore : IDataStore<RecipientAccount>
    {
        Task<int> DeleteAllItemsAsync();
        Task<IEnumerable<RecipientAccount>> GetItemsByOffsetAsync(int start, int range);
        Task<int> GetTotalCount();
    }
}
