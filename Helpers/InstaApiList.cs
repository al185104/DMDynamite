namespace DMDynamite.Helpers
{
    public class InstaApiList : List<IInstaApi>
    {
        public void SaveSessions()
        {
            DirectoryHelper.CreateAccountDirectory();
            if (this == null)
                return;
            if (this.Any())
            {
                foreach (var instaApi in this)
                {
                    var state = instaApi.GetStateDataAsStream();
                    var path = Path.Combine(DirectoryHelper.AccountPathDirectory, $"{instaApi.GetLoggedUser().UserName}{DirectoryHelper.SessionExtension}");
                    using (var fileStream = File.Create(path))
                    {
                        state.Seek(0, SeekOrigin.Begin);
                        state.CopyTo(fileStream);
                    }
                }
            }
        }
    }
}
