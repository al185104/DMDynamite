namespace DMDynamite.Services.Database
{
    internal class RecipientDataStore : IRecipientDataStore
    {
        private SQLiteAsyncConnection dbRecipients;

        private async Task Init()
        {
            if (dbRecipients != null)
                return;

            var databaseAccountsPath = Path.Combine(FileSystem.AppDataDirectory, "RecipientsDB.db");
            dbRecipients = new SQLiteAsyncConnection(databaseAccountsPath);
            await dbRecipients.CreateTableAsync<RecipientAccount>();
        }

        public async Task<bool> AddItemAsync(RecipientAccount item)
        {
            await Init();

            var accounts = await dbRecipients.Table<RecipientAccount>().ToListAsync();

            var account = new RecipientAccount
            {
                Fullname = item.Fullname,
                IsPrivate = item.IsPrivate,
                IsVerified = item.IsVerified,
                Pk = item.Pk,
                ProfilePicture = item.ProfilePicture,
                Username = item.Username,
                Status = item.Status,
                FollowersCount = item.FollowersCount,
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                Id = Guid.NewGuid()
            };

            var ret = await dbRecipients.InsertAsync(account);

            if (ret == 1)
                item.Id = account.Id;

            return ret == 1;
        }

        public async Task<int> DeleteAllItemsAsync()
        {
            return await dbRecipients.DeleteAllAsync<RecipientAccount>();
        }

        public async Task<bool> DeleteItemAsync(Guid id)
        {
            await Init();
            var ret = await dbRecipients.DeleteAsync<RecipientAccount>(id);
            return ret == 1;
        }

        /// <summary>
        /// Delete all usernames from the db list
        /// </summary>
        /// <param name="accounts">username list</param>
        /// <returns></returns>
        public async Task<bool> DeleteItemsAsync(IEnumerable<string> accounts)
        {
            try
            {
                await Init();
                var table = await dbRecipients.Table<RecipientAccount>().ToListAsync();
                foreach (var account in accounts)
                {
                    var ret = table.FirstOrDefault(i => i.Username.ToLower().Equals(account.ToLower()));
                    if (ret == null) continue;

                    var del = await dbRecipients.DeleteAsync<RecipientAccount>(ret.Id);
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

        public async Task<RecipientAccount> GetItemAsync(Guid id)
        {
            await Init();
            var account = await dbRecipients.GetAsync<RecipientAccount>(id);
            return account;
        }

        public async Task<IEnumerable<RecipientAccount>> GetItemsAsync(bool forceRefresh = false)
        {
            await Init();
            var accounts = await dbRecipients.Table<RecipientAccount>().ToListAsync();
            return accounts;
        }

        public async Task<IEnumerable<RecipientAccount>> GetItemsByOffsetAsync(int start, int range)
        {
            await Init();
            string query = $"select * from RecipientAccount limit {range} offset {start}";
            var accounts = await dbRecipients.QueryAsync<RecipientAccount>(query);
            return accounts;
        }

        public async Task<IEnumerable<RecipientAccount>> GetItemsByRangeAsync(int range)
        {
            try
            {
                await Init();
                var accounts = await dbRecipients.Table<RecipientAccount>().ToListAsync();

                if (accounts.Count < range)
                    range = accounts.Count;
                return accounts.GetRange(0, range);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public async Task<int> GetTotalCount()
        {
            await Init();
            var accounts = await dbRecipients.Table<RecipientAccount>().ToListAsync();
            return accounts.Count;
        }

        public async Task<bool> UpdateItemAsync(RecipientAccount item)
        {
            await Init();
            var account = new RecipientAccount
            {
                Id = item.Id,
                CreatedDate = item.CreatedDate,
                Fullname = item.Fullname,
                IsPrivate = item.IsPrivate,
                IsVerified = item.IsVerified,
                Pk = item.Pk,
                ProfilePicture = item.ProfilePicture,
                Username = item.Username,
                FollowersCount = item.FollowersCount,
                Status = item.Status,
                UpdatedDate = DateTime.Now
            };
            var id = await dbRecipients.UpdateAsync(account);
            return id == 1;
        }
    }
}
