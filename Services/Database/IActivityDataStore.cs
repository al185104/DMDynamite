namespace DMDynamite.Services.Database
{
    public interface IActivityDataStore : IDataStore<Activity>
    {
        Task<IEnumerable<Activity>> GetItemsByDateAsync(DateTime start, DateTime end, int range = 50);
    }
}
