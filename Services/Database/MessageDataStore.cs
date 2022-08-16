namespace DMDynamite.Services.Database
{
    public class MessageDataStore : IMessageDataStore
    {
        private SQLiteAsyncConnection db;

        public Message CurrentMessage { get; set; }

        private async Task Init()
        {
            if (db != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "MessageDB.db");
            db = new SQLiteAsyncConnection(dbPath);
            await db.CreateTableAsync<Message>();

        }
        public async Task<bool> AddItemAsync(Message item)
        {
            await Init();
            var obj = new Message
            {
                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,
                Id = Guid.NewGuid(),
                Body = item.Body,
                Subject = item.Subject
            };

            var ret = await db.InsertAsync(obj);

            if (ret == 1)
                item.Id = obj.Id;

            return ret == 1;
        }

        public async Task<bool> DeleteItemAsync(Guid id)
        {
            await Init();
            var ret = await db.DeleteAsync<Message>(id);

            if(id == CurrentMessage.Id)
            {
                var items = await GetItemRandomAsync();
                if (!items.Any())
                    CurrentMessage = null;
            }

            return ret == 1;
        }

        public async Task<bool> DeleteItemsAsync(IEnumerable<string> objects)
        {
            try
            {
                await Init();
                var table = await db.Table<Message>().ToListAsync();
                foreach (var obj in objects)
                {
                    var ret = table.FirstOrDefault(i => i.Subject.ToString().ToLower().Equals(obj.ToLower()));
                    if (ret == null) continue;

                    var del = await db.DeleteAsync<Message>(ret.Id);
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

        public async Task<Message> GetItemAsync(Guid id)
        {
            await Init();
            var obj = await db.GetAsync<Message>(id);
            CurrentMessage = obj;
            return obj;
        }

        public async Task<IEnumerable<Message>> GetItemsAsync(bool forceRefresh = false)
        {
            await Init();
            var objects = await db.Table<Message>().ToListAsync();
            return objects;
        }

        public async Task<IEnumerable<Message>> GetItemsByRangeAsync(int range)
        {
            await Init();
            try
            {
                await Init();
                var objects = await db.Table<Message>().ToListAsync();

                if (objects.Count < range)
                    range = objects.Count;

                return objects.GetRange(0, range);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public async Task<bool> UpdateItemAsync(Message item)
        {
            await Init();
            var obj = new Message
            {
                Id = item.Id,
                CreatedDate = item.CreatedDate,
                UpdatedDate = DateTime.Now,
                Body = item.Body,
                Subject = item.Subject
            };
            var id = await db.UpdateAsync(obj);
            return id == 1;
        }

        public async Task<IEnumerable<Message>> GetItemRandomAsync()
        {
            await Init();

            var objects = await db.Table<Message>().ToListAsync();
            var rand = new Random();

            List<Message> list = new();
            list.Add(objects.ElementAt(rand.Next(0,objects.Count - 1)));

            if(list.Count > 0)
                CurrentMessage = list.ElementAt(0);

            return list;
        }
    }
}
