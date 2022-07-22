namespace DMDynamite.Services.Database
{
    public class ProxyDataStore : IProxyDataStore
    {
        private SQLiteAsyncConnection db;

        private async Task Init()
        {
            if (db != null)
                return;

            var databaseAccountsPath = Path.Combine(FileSystem.AppDataDirectory, "ProxyDB.db");
            db = new SQLiteAsyncConnection(databaseAccountsPath);
            await db.CreateTableAsync<ProxySetup>();
        }

        public async Task<bool> AddItemAsync(ProxySetup item)
        {
            await Init();
            var obj = new ProxySetup
            {
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                Id = Guid.NewGuid(),
                IsUsed = false,
                ProxyName = item.ProxyName,
                ProxyHost = item.ProxyHost,
                ProxyPort = item.ProxyPort,
                ProxyPassword = item.ProxyPassword,
                ProxyUsername = item.ProxyUsername
            };

            var ret = await db.InsertAsync(obj);

            if (ret == 1)
                item.Id = obj.Id;

            return ret == 1;
        }

        public async Task<bool> DeleteItemAsync(Guid id)
        {
            await Init();
            var ret = await db.DeleteAsync<ProxySetup>(id);
            return ret == 1;
        }

        /// <summary>
        /// Delete the list of proxy names from db.
        /// </summary>
        /// <param name="objects">list of proxy names</param>
        /// <returns></returns>
        public async Task<bool> DeleteItemsAsync(IEnumerable<string> objects)
        {
            try
            {
                await Init();
                var table = await db.Table<ProxySetup>().ToListAsync();
                foreach (var obj in objects)
                {
                    var ret = table.FirstOrDefault(i => i.ProxyName.ToString().ToLower().Equals(obj.ToLower()));
                    if (ret == null) continue;

                    var del = await db.DeleteAsync<ProxySetup>(ret.Id);
                    if (del == 0)
                    {
                        Console.WriteLine("Wrong!!");
                    }
                }
                return true;
            }
            catch (Exception)
            {
                //TODO
                return false;
            }
        }

        public async Task<ProxySetup> GetItemAsync(Guid id)
        {
            await Init();
            var obj = await db.GetAsync<ProxySetup>(id);
            return obj;
        }

        public async Task<IEnumerable<ProxySetup>> GetItemsAsync(bool forceRefresh = false)
        {
            await Init();
            var objects = await db.Table<ProxySetup>().ToListAsync();
            return objects;
        }

        public async Task<IEnumerable<ProxySetup>> GetItemsByRangeAsync(int range)
        {
            await Init();
            try
            {
                await Init();
                var objects = await db.Table<ProxySetup>().ToListAsync();

                if (objects.Count < range)
                    range = objects.Count;
                return objects.GetRange(0, range);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public async Task<bool> UpdateItemAsync(ProxySetup item)
        {
            await Init();
            var obj = new ProxySetup
            {
                Id = item.Id,
                CreatedDate = item.CreatedDate,
                ProxyHost = item.ProxyHost,
                ProxyName = item.ProxyName,
                ProxyPort = item.ProxyPort,
                ProxyUsername = item.ProxyUsername,
                ProxyPassword = item.ProxyPassword,
                IsUsed = item.IsUsed,
                UpdatedDate = DateTime.Now
            };
            var id = await db.UpdateAsync(obj);
            return id == 1;
        }

        public async Task<IEnumerable<ProxySetup>> GetItemRandomAsync()
        {
            await Init();

            var objects = await db.Table<ProxySetup>().ToListAsync();
            
            List<ProxySetup> list = new();
            var proxy = objects.FirstOrDefault(i => i.IsUsed == false);
            list.Add(proxy);
            return list;
            //string query = $"SELECT * FROM [ProxySetup] WHERE IsUsed = false ORDER BY RANDOM() LIMIT 1";
            //var proxies = await db.QueryAsync<ProxySetup>(query);
            //return proxies;
        }
    }
}