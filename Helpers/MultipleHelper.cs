namespace DMDynamite.Helpers
{
    public static class MultipleHelper
    {
        public static ObservableCollection<string> LoggedInUsers = new ObservableCollection<string>();
        public static InstaApiList ApiList { get; private set; } = new InstaApiList();

        public static void LoadSessions()
        {
            ApiList = new InstaApiList();
            LoggedInUsers = new ObservableCollection<string>();
            if (Directory.Exists(DirectoryHelper.AccountPathDirectory))
            {
                var files = Directory.GetFiles(DirectoryHelper.AccountPathDirectory);
                if (files?.Length > 0)
                {
                    foreach (var path in files)
                    {
                        if (Path.GetExtension(path).ToLower() == DirectoryHelper.SessionExtension)
                        {
                            // load session!
                            var api = BuildApi();
                            var sessionHandler = new FileSessionHandler { FilePath = path, InstaApi = api };

                            api.SessionHandler = sessionHandler;

                            LoadSession(api);
                            if (api.IsUserAuthenticated)
                            {
                                LoggedInUsers.Add(api.GetLoggedUser().LoggedInUser.UserName.ToLower());
                                ApiList.Add(api);
                            }
                        }
                    }
                }
            }
            else
                Directory.CreateDirectory(DirectoryHelper.AccountPathDirectory);
        }

        public static void LoadSession(IInstaApi api)
        {
            if (File.Exists(api.SessionHandler.FilePath))
            {
                using (var fs = File.OpenRead(api.SessionHandler.FilePath))
                {
                    LoadStateDataStream(api, fs);
                }
            }
        }

        private static void LoadStateDataStream(IInstaApi api, Stream stream)
        {
            var json = new StreamReader(stream).ReadToEnd();
            var data = JsonConvert.DeserializeObject<StateData>(json);
            api.LoadStateDataFromObject(data);
        }

        public static IInstaApi BuildApi(string username = null, string password = null, string host = null, string port= null, string proxyUsername = null, string proxyPassword = null)
        {
            if(string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) || string.IsNullOrEmpty(proxyUsername) || string.IsNullOrEmpty(proxyPassword))
            {
                var fakeUserData = UserSessionData.ForUsername(username ?? "FAKEUSER").WithPassword(password ?? "FAKEPASS");
                return InstaApiBuilder.CreateBuilder()
                                 .SetUser(fakeUserData)
                                 .SetRequestDelay(RequestDelay.FromSeconds(0, 1))
                                 .Build();
            }
            else
            {
                var proxy = new WebProxy()
                {
                    Address = new Uri($"http://{host}:{port}"), //i.e: http://1.2.3.4.5:8080
                    BypassProxyOnLocal = false,
                    UseDefaultCredentials = false,

                    // *** These creds are given to the proxy server, not the web server ***
                    Credentials = new NetworkCredential(
                    userName: proxyUsername,
                    password: proxyPassword)
                };

                // Now create a client handler which uses that proxy
                var httpClientHandler = new HttpClientHandler()
                {
                    Proxy = proxy,
                };

                var fakeUserData = UserSessionData.ForUsername(username ?? "FAKEUSER").WithPassword(password ?? "FAKEPASS");
                return InstaApiBuilder.CreateBuilder()
                                 .SetUser(fakeUserData)
                                 .SetRequestDelay(RequestDelay.FromSeconds(0, 1))                                
                                 .UseHttpClientHandler(httpClientHandler)
                                 .Build();
            }
        }
    }
}
