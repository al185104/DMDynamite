namespace DMDynamite.ViewModels
{
    public partial class MainViewModel : BaseViewModel
    {
        #region Private Fields
        IInstaApi _api;
        private readonly ILogger<MainViewModel> _logger;
        private readonly IInstagramService _instagramService;
        private readonly IActivityDataStore _activityDataStore;
        private readonly IMessageDataStore _messageDataStore;
        private readonly IProxyDataStore _proxyDataStore;
        private readonly IRecipientDataStore _recipientDataStore;
        private readonly ISenderDataStore _senderDataStore;
        #endregion

        #region Constructors
        public MainViewModel(
            ILogger<MainViewModel> logger,
            IInstagramService instagramService,
            IActivityDataStore activityDataStore,
            IMessageDataStore messageDataStore,
            IProxyDataStore proxyDataStore,
            IRecipientDataStore recipientDataStore,
            ISenderDataStore senderDataStore)
        {
            _logger = logger;
            _instagramService = instagramService;
            _activityDataStore = activityDataStore;
            _messageDataStore = messageDataStore;
            _proxyDataStore = proxyDataStore;
            _recipientDataStore = recipientDataStore;
            _senderDataStore = senderDataStore;

            _logger.IsEnabled(LogLevel.Information);
        }
        #endregion

        #region Properties
        public ObservableCollection<SenderAccount> Senders { get; } = new();
        public ObservableCollection<RecipientAccount> Recipients { get; } = new();
        public ObservableCollection<Activity> Activities { get; } = new();
        public ObservableCollection<InstaUserShort> Followers { get; } = new();

        [ObservableProperty]
        bool searchFromTop = true;

        [ObservableProperty]
        double latitude;

        [ObservableProperty]
        double longitude;

        [ObservableProperty]
        bool pauseOnError;

        [ObservableProperty]
        int followersCount;

        [ObservableProperty]
        int timeInterval;

        [ObservableProperty]
        int blastPerAccount;

        [ObservableProperty]
        int totalCount;

        [ObservableProperty]
        int successCount; // TODO

        [ObservableProperty]
        int failureCount; // TODO

        [ObservableProperty]
        string subscriptionKey;

        [ObservableProperty]
        SendingOption sendingOption;

        [ObservableProperty]
        InstaUserInfo myAccount;
        #endregion

        #region Commands
        [ICommand]
        async Task ShowProxyPopup()
        {
            try
            {
                _logger.LogInformation("+ShowProxyPopup");
                var _proxy = await App.Current.MainPage.ShowPopupAsync(new ProxyPopup());

                if(_proxy != null)
                {
                    await _proxyDataStore.AddItemAsync(_proxy as ProxySetup);

                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-ShowProxyPopup");
            }
        }

        [ICommand]
        async Task ShowLoginPopup()
        {
            try
            {
                _logger.LogInformation("+ShowLoginPopup");
                var proxies = await _proxyDataStore.GetItemsAsync();

                var acc = await App.Current.MainPage.ShowPopupAsync(new LoginPopup(proxies));

                if(acc != null)
                {
                    var account = acc as SenderAccount;
                    // login account
                    var ret = await Login(account);
                    // refresh list
                    if (ret)
                    {
                        var _api = MultipleHelper.ApiList.FirstOrDefault(i => i.GetLoggedUser().LoggedInUser.UserName.ToLower().Equals(account.Username.ToLower()));
                        var obj = await _api.UserProcessor.GetUserInfoByUsernameAsync(_api.GetLoggedUser().LoggedInUser.UserName);
                        if (obj.Succeeded)
                        {
                            account.ProfilePicture = obj.Value.ProfilePicUrl;
                            account.FollowersCount = (int)obj.Value.FollowerCount;
                            account.FollowingsCount = (int)obj.Value.FollowingCount;
                            await _senderDataStore.AddItemAsync(account);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-ShowLoginPopup");
            }
        }

        [ICommand]
        async Task Setup()
        {
            IsBusy = true;
            await Task.Delay(100);
            _logger.LogInformation("+Setup");

            // Get settings
            LoadSettings();
            // Load Senders
            LoadSenders();

            List<Task> tasks = new();

            // Load Recipients
            tasks.Add(LoadRecipients());
            // Get ActivityReport
            tasks.Add(LoadActivityReport());
            // Get MyAccount
            await Task.WhenAll(tasks);
            await LoadFirstAccount();

            _logger.LogInformation("-Setup");
            IsBusy = false;
        }

        [ICommand]
        async Task ChangeSearch(SendingOption option)
        {
            switch (option)
            {
                case SendingOption.Hashtag:
                    SendingOption = option;
                    break;
                case SendingOption.Location:
                    _logger.LogInformation("+GetLocation");
                    string address = await App.Current.MainPage.DisplayPromptAsync("Input address", "state the name of the location/city/state", "Set", "Back", "Times Square New York", keyboard: Keyboard.Default);

                    if (!string.IsNullOrEmpty(address))
                    {
                        IEnumerable<Location> locations = await Geocoding.Default.GetLocationsAsync(address);

                        Location location = locations?.FirstOrDefault();

                        if (location != null)
                        {
                            Latitude = location.Latitude;
                            Longitude = location.Longitude;
                            SendingOption = option;
                        }
                    }
                    _logger.LogInformation("-GetLocation");
                    break;
                default:
                    SendingOption = option;
                    break;
            }
        }

        [ICommand]
        public async Task Search(string search)
        {
            IResult<InstaUserShortList> followerList;
            try
            {
                IsBusy = true;
                _logger.LogInformation("+Search");

                if (string.IsNullOrEmpty(search)) return;

                if (_api == null)
                {
                    await App.Current.MainPage.DisplayAlert("No account", "You currently don't have any selected accounts to search.", "Back");
                    return;
                }

                if (sendingOption == SendingOption.Location)
                    followerList = await _instagramService.SearchByLocationTagAsync(_api, latitude, longitude, search, PaginationParameters.MaxPagesToLoad(5), 1000, searchFromTop); // true is search top results
                else if (sendingOption == SendingOption.Hashtag)
                    followerList = await _instagramService.SearchByHashTagAsync(_api, search, PaginationParameters.MaxPagesToLoad(2), 1000);
                else
                    followerList = await _api.UserProcessor.GetUserFollowersAsync(search, PaginationParameters.MaxPagesToLoad(2));

                if (followerList.Succeeded)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Followers.Clear();
                        foreach (var follower in followerList.Value.Distinct())
                            Followers.Add(follower);
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                IsBusy = false;
                _logger.LogInformation("-Search");
            }
        }

        [ICommand]
        async void AddAllToQueue()
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("+AddAllToQueue");

                var ret = await App.Current.MainPage.DisplayAlert("Add all as recipients?", $"Are you sure you want to add all {Followers.Count} accounts as recipients?", "Yes", "No");
                if (ret)
                {
                    if (Followers.Any())
                    {
                        var _temp = new List<InstaUserShort>(Followers);
                        foreach (var follower in _temp)
                        {
                            var instaAccount = new RecipientAccount
                            {
                                Pk = follower.Pk.ToString(),
                                Username = follower.UserName,
                                Fullname = follower.FullName,
                                IsPrivate = follower.IsPrivate,
                                IsVerified = follower.IsVerified,
                                ProfilePicture = follower.ProfilePicture
                            };

                            var result = await _recipientDataStore.AddItemAsync(instaAccount);
                            if (result)
                                Followers.Remove(follower);
                        }
                    }

                    var queue = await _recipientDataStore.GetItemsByOffsetAsync(0, 50);
                    var count = await _recipientDataStore.GetTotalCount();
                    if (queue.Any())
                    {
                        TotalCount = count;
                        Recipients.Clear();
                        foreach (var q in queue)
                            Recipients.Add(q);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {

                _logger.LogInformation("-AddAllToQueue");
                IsBusy = false;
            }
        }
        #endregion

        #region Private Methods
        private async Task<bool> Login(SenderAccount acc)
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("+Login");

                var _proxy = await _proxyDataStore.GetItemAsync(acc.ProxyFK);

                IInstaApi api = MultipleHelper.ApiList.FirstOrDefault(x => x.GetLoggedUser().LoggedInUser.UserName.Equals(acc.Username.ToLower()));
                if (api == null)
                    api = MultipleHelper.BuildApi(acc.Username.ToLower(), acc.Password, _proxy.ProxyHost, _proxy.ProxyPort, _proxy.ProxyUsername, _proxy.ProxyPassword);

                if (!api.IsUserAuthenticated)
                {
                    var loginResult = await _instagramService.LoginAccount(api, acc.Username.ToLower(), acc.Password);

                    if (!loginResult.Succeeded)
                    {
                        if (loginResult.Value == InstaLoginResult.ChallengeRequired)
                        {
                            var challenge = await api.GetChallengeRequireVerifyMethodAsync();
                            if (challenge.Succeeded)
                            {
                                if (challenge.Value.SubmitPhoneRequired)
                                {
                                    var challengeResponse = await App.Current.MainPage.DisplayPromptAsync($"Phone challenge~{acc.Username}", "Please type a valid phone number(with country code)", "Okay", "Cancel", "+989123456789", 13, Keyboard.Numeric, "");
                                    if (!string.IsNullOrEmpty(challengeResponse))
                                    {
                                        var submitPhone = await api.SubmitPhoneNumberForChallengeRequireAsync(challengeResponse);
                                        if (submitPhone.Succeeded)
                                        {
                                            // VERIFY
                                            var verificationResponse = await App.Current.MainPage.DisplayPromptAsync($"Verification~{acc.Username}", "Please enter verification code", "Okay", "Cancel", "code", 6, Keyboard.Numeric, "");
                                            if (!string.IsNullOrEmpty(verificationResponse))
                                            {
                                                var code = verificationResponse.Replace(" ", "");
                                                var regex = new Regex(@"^-*[0-9,\.]+$");
                                                if (!regex.IsMatch(code)) { await App.Current.MainPage.DisplayAlert("ERR", "Verification code must be numeric!", "Back"); return false; }
                                                if (code.Length != 6) { await App.Current.MainPage.DisplayAlert("ERR", "Verification code must be 6 digits!", "Back"); return false; }

                                                var verifyLogin = await api.VerifyCodeForChallengeRequireAsync(code);
                                                if (verifyLogin.Succeeded)
                                                {
                                                    _instagramService.SaveSession(api, acc.Username.ToLower());
                                                    //await App.Current.MainPage.DisplayAlert("Login success", $"{acc.Username} is now added as sender", "Okay");
                                                    return true;
                                                }
                                                else
                                                {
                                                    if (verifyLogin.Value == InstaLoginResult.TwoFactorRequired)
                                                    {
                                                        var twoFResponse = await App.Current.MainPage.DisplayPromptAsync($"2F Auth~{acc.Username}", "Input two factor authentication", "Okay", null, "code", 12, Keyboard.Default, "");
                                                        if (!string.IsNullOrEmpty(twoFResponse))
                                                        {
                                                            var twoFac = await _instagramService.TwoFactorAuthentication(api, acc.Username, twoFResponse);
                                                            if (twoFac.Succeeded)
                                                            {
                                                                _instagramService.SaveSession(api, acc.Username.ToLower());
                                                                //await App.Current.MainPage.DisplayAlert("Login success", $"{acc.Username} is now added as sender", "Okay");
                                                                return true;
                                                            }
                                                        }
                                                    }
                                                    else
                                                        await App.Current.MainPage.DisplayAlert("ERR Verify Login", verifyLogin.Info.Message, "Back");
                                                }
                                            }
                                        }
                                        else
                                            await App.Current.MainPage.DisplayAlert("ERR Submit Phone", submitPhone.Info.Message, "Back");
                                    }
                                }
                                else if (challenge.Value.StepData != null)
                                {
                                    if (!string.IsNullOrEmpty(challenge.Value.StepData.Email))
                                    {
                                        var email = await api.RequestVerifyCodeToEmailForChallengeRequireAsync();
                                        if (email.Succeeded)
                                        {
                                            // VERIFY
                                            var verificationResponse = await App.Current.MainPage.DisplayPromptAsync($"Verification~{acc.Username}", "Please enter verification code", "Okay", "Cancel", "code", 6, Keyboard.Numeric, "");
                                            if (!string.IsNullOrEmpty(verificationResponse))
                                            {
                                                var code = verificationResponse.Replace(" ", "");
                                                var regex = new Regex(@"^-*[0-9,\.]+$");
                                                if (!regex.IsMatch(code)) { await App.Current.MainPage.DisplayAlert("ERR", "Verification code must be numeric!", "Back"); return false; }
                                                if (code.Length != 6) { await App.Current.MainPage.DisplayAlert("ERR", "Verification code must be 6 digits!", "Back"); return false; }

                                                var verifyLogin = await api.VerifyCodeForChallengeRequireAsync(code);
                                                if (verifyLogin.Succeeded)
                                                {
                                                    _instagramService.SaveSession(api, acc.Username.ToLower());
                                                    //await App.Current.MainPage.DisplayAlert("Login success", $"{acc.Username} is now added as sender", "Okay");
                                                    return true;
                                                }
                                                else
                                                {
                                                    if (verifyLogin.Value == InstaLoginResult.TwoFactorRequired)
                                                    {
                                                        var twoFResponse = await App.Current.MainPage.DisplayPromptAsync($"2F Auth~{acc.Username}", "Input two factor authentication", "Okay", null, "code", 12, Keyboard.Default, "");
                                                        if (!string.IsNullOrEmpty(twoFResponse))
                                                        {
                                                            var twoF = await _instagramService.TwoFactorAuthentication(api, acc.Username, twoFResponse);
                                                            if (twoF.Succeeded)
                                                            {
                                                                _instagramService.SaveSession(api, acc.Username.ToLower());
                                                                //await App.Current.MainPage.DisplayAlert("Login success", $"{acc.Username} is now added as sender", "Okay");
                                                                return true;
                                                            }
                                                        }
                                                    }
                                                    else
                                                        await App.Current.MainPage.DisplayAlert("ERR Verify Login", verifyLogin.Info.Message, "Back");
                                                }
                                            }
                                        }
                                        else
                                            await App.Current.MainPage.DisplayAlert("ERR", email.Info.Message, "Back");
                                    }
                                    else if (!string.IsNullOrEmpty(challenge.Value.StepData.PhoneNumber))
                                    {
                                        var phoneNumber = await api.RequestVerifyCodeToSMSForChallengeRequireAsync();
                                        if (phoneNumber.Succeeded)
                                        {
                                            // VERIFY
                                            var verificationResponse = await App.Current.MainPage.DisplayPromptAsync($"Verification~{acc.Username}", "Please enter verification code", "Okay", "Cancel", "code", 6, Keyboard.Numeric, "");
                                            if (!string.IsNullOrEmpty(verificationResponse))
                                            {
                                                var code = verificationResponse.Replace(" ", "");
                                                var regex = new Regex(@"^-*[0-9,\.]+$");
                                                if (!regex.IsMatch(code)) { await App.Current.MainPage.DisplayAlert("ERR", "Verification code must be numeric!", "Back"); return false; }
                                                if (code.Length != 6) { await App.Current.MainPage.DisplayAlert("ERR", "Verification code must be 6 digits!", "Back"); return false; }

                                                var verifyLogin = await api.VerifyCodeForChallengeRequireAsync(code);
                                                if (verifyLogin.Succeeded)
                                                {
                                                    _instagramService.SaveSession(api, acc.Username.ToLower());
                                                    //await App.Current.MainPage.DisplayAlert("Login success", $"{acc.Username} is now added as sender", "Okay");
                                                    return true;
                                                }
                                                else
                                                {
                                                    if (verifyLogin.Value == InstaLoginResult.TwoFactorRequired)
                                                    {
                                                        var twoFResponse = await App.Current.MainPage.DisplayPromptAsync($"2F Auth~{acc.Username}", "Input two factor authentication", "Okay", null, "code", 12, Keyboard.Default, "");
                                                        if (!string.IsNullOrEmpty(twoFResponse))
                                                        {
                                                            var twoF = await _instagramService.TwoFactorAuthentication(api, acc.Username, twoFResponse);
                                                            if (twoF.Succeeded)
                                                            {
                                                                _instagramService.SaveSession(api, acc.Username.ToLower());
                                                                //await App.Current.MainPage.DisplayAlert("Login success", $"{acc.Username} is now added as sender", "Okay");
                                                                return true;
                                                            }
                                                        }
                                                    }
                                                    else
                                                        await App.Current.MainPage.DisplayAlert("ERR Verify Login", verifyLogin.Info.Message, "Back");
                                                }
                                            }
                                        }
                                        else
                                            await App.Current.MainPage.DisplayAlert("ERR", phoneNumber.Info.Message, "Back");
                                    }
                                }
                            }
                            else
                                await App.Current.MainPage.DisplayAlert("ERR Challenge", challenge.Info.Message, "Back");
                        }
                        else if (loginResult.Value == InstaLoginResult.BadPassword)
                            await App.Current.MainPage.DisplayAlert("Bad Password", "Cannot find the account, please try again.", "Back");
                        else
                            await App.Current.MainPage.DisplayAlert("Something went wrong", loginResult.Info.Message, "Back");
                    }
                    else
                        return true;
                }
                else
                    return true;

                return false;
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                IsBusy = false;
                _logger.LogInformation("+Login");
            }
        }

        private async Task LoadActivityReport()
        {
            try
            {
                _logger.LogInformation("+LoadActivityReport");
                var activities = await _activityDataStore.GetItemsAsync();
                if(activities != null)
                {
                    Activities.Clear();
                    foreach(var activity in activities)
                        Activities.Add(activity);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-LoadActivityReport");
            }
        }

        private void LoadSenders()
        {
            try
            {
                _logger.LogInformation("+LoadSenders");
                MultipleHelper.LoadSessions();

                Senders.Clear();

                // reload accounts
                foreach (var api in MultipleHelper.ApiList)
                {
                    var account = api.GetLoggedUser().LoggedInUser;

                    // get proxy in db
                    Senders.Add(new SenderAccount
                    {
                        Username = account.UserName,
                        ProfilePicture = account.ProfilePicture,
                        Password = api.GetLoggedUser().Password,
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-LoadSenders");
            }
        }

        private async Task LoadRecipients()
        {
            try
            {
                _logger.LogInformation("+LoadRecipients");
                var queue = await _recipientDataStore.GetItemsByOffsetAsync(0, 50);
                var count = await _recipientDataStore.GetTotalCount();

                if (queue.Any())
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TotalCount = count;
                        Recipients.Clear();
                        foreach (var q in queue)
                            Recipients.Add(q);
                    });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation($"-LoadRecipients {Recipients.Count}");
            }
        }

        private async Task LoadFirstAccount()
        {
            try
            {
                _logger.LogInformation("+LoadFirstAccount");
                
                var _api = MultipleHelper.ApiList.FirstOrDefault();
                if(_api != null)
                {
                    var account = await _api.UserProcessor.GetUserInfoByUsernameAsync(_api.GetLoggedUser().LoggedInUser.UserName);
                    if (account.Succeeded)
                    {
                        MyAccount = account.Value;
                        this._api = _api;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-LoadFirstAccount");
            }
        }

        private void LoadSettings()
        {
            try
            {
                _logger.LogInformation("+LoadSettings");
                // getting settings
                PauseOnError = Preferences.Get(nameof(PauseOnError), false);
                FollowersCount = Preferences.Get(nameof(FollowersCount), 0);
                TimeInterval = Preferences.Get(nameof(TimeInterval), 15);
                BlastPerAccount = Preferences.Get(nameof(BlastPerAccount), 1);
                SubscriptionKey = Preferences.Get(nameof(SubscriptionKey), string.Empty);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-LoadSettings");
            }
        } 
        #endregion
    }
}
