namespace DMDynamite.Services.Database
{
    internal class SenderDataStore : ISenderDataStore
    {
        private SQLiteAsyncConnection db;

        private async Task Init()
        {
            if (db != null)
                return;

            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "SendersDB.db");
            db = new SQLiteAsyncConnection(databasePath);
            await db.CreateTableAsync<SenderAccount>();
        }

        public async Task<bool> AddItemAsync(SenderAccount item)
        {
            await Init();
            var obj = new SenderAccount
            {
                Id = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                JoinDate = item.JoinDate,
                ChallengeURL = item.ChallengeURL,
                HasIssue = item.HasIssue,
                Username = item.Username,
                Password = item.Password,
                ProfilePicture = item.ProfilePicture,
                ProxyFK = item.ProxyFK,
                FollowersCount = item.FollowersCount,
                FollowingsCount = item.FollowingsCount,
                Status = item.Status
            };

            var ret = await db.InsertAsync(obj);

            if (ret == 1)
                item.Id = obj.Id;

            return ret == 1;
        }

        public async Task<bool> DeleteItemAsync(Guid id)
        {
            await Init();
            var ret = await db.DeleteAsync<SenderAccount>(id);
            return ret == 1;
        }

        /// <summary>
        /// Delete all senders from db list
        /// </summary>
        /// <param name="objects">username list</param>
        /// <returns></returns>
        public async Task<bool> DeleteItemsAsync(IEnumerable<string> objects)
        {
            try
            {
                await Init();
                var table = await db.Table<SenderAccount>().ToListAsync();
                foreach (var obj in objects)
                {
                    var ret = table.FirstOrDefault(i => i.Username.ToLower().Equals(obj.ToLower()));
                    if (ret == null) continue;

                    var del = await db.DeleteAsync<SenderAccount>(ret.Id);
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

        public async Task<SenderAccount> GetItemAsync(Guid id)
        {
            await Init();
            var obj = await db.GetAsync<SenderAccount>(id);
            return obj;
        }

        public async Task<IEnumerable<SenderAccount>> GetItemsAsync(bool forceRefresh = false)
        {
            await Init();
            var objects = await db.Table<SenderAccount>().ToListAsync();
            return objects.OrderByDescending(i => i.CreatedDate);
        }

        public async Task<IEnumerable<SenderAccount>> GetItemsByRangeAsync(int range)
        {
            try
            {
                await Init();
                var objects = await db.Table<SenderAccount>().ToListAsync();

                if (objects.Count < range)
                    range = objects.Count;
                return objects.GetRange(0, range);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public async Task<bool> UpdateItemAsync(SenderAccount item)
        {
            await Init();
            var obj = new SenderAccount
            {
                Id = item.Id,
                CreatedDate = item.CreatedDate,
                Username = item.Username,
                Status = item.Status,
                ProxyFK = item.ProxyFK,
                ProfilePicture = item.ProfilePicture,
                Password = item.Password,
                HasIssue = item.HasIssue,
                ChallengeURL = item.ChallengeURL,
                FollowersCount = item.FollowersCount,
                FollowingsCount = item.FollowingsCount,
                JoinDate = item.JoinDate,
                UpdatedDate = DateTime.Now
            };
            var id = await db.UpdateAsync(obj);
            return id == 1;
        }

        public async Task<SenderAccount> GetItemByUsernameAsync(string name)
        {
            await Init();

            var objects = await db.Table<SenderAccount>().ToListAsync();

            var obj = await db.FindAsync<SenderAccount>(i => i.Username.ToLower().Equals(name.ToLower()));
            return obj;
        }
    }
}
