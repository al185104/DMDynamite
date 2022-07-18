namespace DMDynamite.Services.Database
{
    public interface IProxyDataStore : IDataStore<ProxySetup>
    {
        Task<IEnumerable<ProxySetup>> GetItemRandomAsync();
    }
}
