namespace DMDynamite.Helpers
{
    static class DirectoryHelper
    {
#if WINDOWS
        public static string AccountPathDirectory = "\\Accounts\\DMDynamite";
#else
        public static string AccountPathDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Accounts");
#endif
        public const string SessionExtension = ".bin";
        public static void CreateAccountDirectory()
        {
            if (!Directory.Exists(AccountPathDirectory))
                Directory.CreateDirectory(AccountPathDirectory);
        }
        public static string GetAccountPath(this string username) => $"{AccountPathDirectory}/{username}{SessionExtension}";
    }
}
