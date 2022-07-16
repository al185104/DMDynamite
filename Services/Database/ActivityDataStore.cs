namespace DMDynamite.Services.Database
{
    internal class ActivityDataStore : IActivityDataStore
    {
        private SQLiteAsyncConnection dbActivity;

        private async Task Init()
        {
            if (dbActivity != null)
                return;

            var databaseAccountsPath = Path.Combine(FileSystem.AppDataDirectory, "ActivitiesDB.db");
            dbActivity = new SQLiteAsyncConnection(databaseAccountsPath);
            await dbActivity.CreateTableAsync<Activity>();
        }

        public async Task<bool> AddItemAsync(Activity item)
        {
            await Init();
            var activity = new Activity
            {
                Id = Guid.NewGuid(),
                CreatedDate = DateTime.Now,
                IsDeleted = false,
                Message = item.Message,
                RecipientFK = item.RecipientFK,
                SenderFK = item.SenderFK,
                Status = item.Status,
                IsSuccessful = item.IsSuccessful,
                UpdatedDate = DateTime.Now
            };

            var ret = await dbActivity.InsertAsync(activity);

            if (ret == 1)
                item.Id = activity.Id;

            return ret == 1;
        }

        public async Task<bool> DeleteItemAsync(Guid id)
        {
            await Init();
            var ret = await dbActivity.DeleteAsync<Activity>(id);
            return ret == 1;
        }

        public async Task<bool> DeleteItemsAsync(IEnumerable<string> activities)
        {
            try
            {
                await Init();
                var table = await dbActivity.Table<Activity>().ToListAsync();
                foreach (var activity in activities)
                {
                    var ret = table.FirstOrDefault(i => i.Id.ToString().ToLower().Equals(activity.ToLower()));
                    if (ret == null) continue;

                    var del = await dbActivity.DeleteAsync<Activity>(ret.Id);
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

        public async Task<Activity> GetItemAsync(Guid id)
        {
            await Init();
            var activity = await dbActivity.GetAsync<Activity>(id);
            return activity;
        }

        public async Task<IEnumerable<Activity>> GetItemsAsync(bool forceRefresh = false)
        {
            await Init();
            var activity = await dbActivity.Table<Activity>().ToListAsync();
            return activity;
        }

        public async Task<IEnumerable<Activity>> GetItemsByDateAsync(DateTime start, DateTime end, int range = 50)
        {
            await Init();

            string query = $"SELECT * FROM [Activity] WHERE ( [UpdatedDate] BETWEEN {start.Ticks} AND {end.Ticks})";

            var activities = await dbActivity.QueryAsync<Activity>(query);
            return activities;
        }

        public async Task<IEnumerable<Activity>> GetItemsByRangeAsync(int range)
        {
            await Init();
            try
            {
                await Init();
                var activities = await dbActivity.Table<Activity>().ToListAsync();

                if (activities.Count < range)
                    range = activities.Count;
                return activities.GetRange(0, range);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public async Task<bool> UpdateItemAsync(Activity item)
        {
            await Init();
            var account = new Activity
            {
                Id = item.Id,
                CreatedDate = item.CreatedDate,
                IsDeleted = false,
                Message = item.Message,
                RecipientFK = item.RecipientFK,
                SenderFK = item.SenderFK,
                Status = item.Status,
                IsSuccessful = item.IsSuccessful,
                UpdatedDate = DateTime.Now
            };
            var id = await dbActivity.UpdateAsync(account);
            return id == 1;
        }
    }
}
