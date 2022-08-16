namespace DMDynamite.ViewModels
{
    public partial class AccountsViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly ILogger<AccountsViewModel> _logger;
        private readonly ISenderDataStore _senderDataStore;
        private readonly IInstagramService _instagramService;
        private readonly IActivityDataStore _activityDataStore;
        private readonly IMessageDataStore _messageDataStore;
        private readonly IProxyDataStore _proxyDataStore;
        private readonly IRecipientDataStore _recipientDataStore;
        #endregion

        #region Properties
        public ObservableCollection<SenderAccount> Senders { get; } = new(); 
        #endregion

        #region Constructor
        public AccountsViewModel(
            ILogger<AccountsViewModel> logger,
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
        }
        #endregion

        #region Private Methods
        [ICommand]
        async Task LoadSenders()
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("+LoadSenders");
                MultipleHelper.LoadSessions();

                Senders.Clear();

                // reload accounts
                foreach (var api in MultipleHelper.ApiList)
                {
                    // account from session
                    var acc = api.GetLoggedUser().LoggedInUser;

                    // get account from db
                    var account = await _senderDataStore.GetItemByUsernameAsync(acc.UserName);
                    if (account != null)
                        Senders.Add(account);
                    else
                    {
                        Console.WriteLine("hmm");
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
                IsBusy = false;
                _logger.LogInformation("-LoadSenders");
            }
        }

        [ICommand]
        async Task ShowLoginPopup()
        {
            try
            {
                _logger.LogInformation("+ShowLoginPopup");
                // setting if random here
                var p = await _proxyDataStore.GetItemsAsync();
                var proxies = await _proxyDataStore.GetItemRandomAsync();
                var acc = await App.Current.MainPage.ShowPopupAsync(new LoginPopup(proxies));

                if (acc != null)
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
                        }

                        var proxy = proxies.ElementAt(0);
                        proxy.IsUsed = true;

                        await _proxyDataStore.UpdateItemAsync(proxy);
                        await _senderDataStore.AddItemAsync(account);
                        await LoadSendersCommand.ExecuteAsync(null);
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
        async Task ShowProxyPopup()
        {
            try
            {
                _logger.LogInformation("+ShowProxyPopup");
                var _proxy = await App.Current.MainPage.ShowPopupAsync(new ProxyPopup());

                if (_proxy != null)
                {
                    if(_proxy is List<ProxySetup>)
                    {
                        var proxyList = _proxy as List<ProxySetup>;
                        foreach (var proxy in proxyList)
                            await _proxyDataStore.AddItemAsync(proxy);
                    }
                    else
                        await _proxyDataStore.AddItemAsync(_proxy as ProxySetup);

                    await App.Current.MainPage.DisplayAlert("Proxy added", "Successfully added new proxy/ies", "Okay");
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
        #endregion


        #region Private Method
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

        [ICommand]
        async Task SelectSender(object obj)
        {
            try
            {
                if (obj == null) return;

                var account = obj as SenderAccount;
                _logger.LogInformation("+SelectSender");
                var ret = await App.Current.MainPage.DisplayAlert("Use this account", $"Are you sure you want to use {account.Username} for search?", "Yes", "Back");
                if (ret)
                {
                    // save sender account username
                    Preferences.Set("SenderName", account.Username);
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-SelectSender");
            }
        }

        [ICommand]
        async Task FixAccount(object obj)
        {
            try
            {
                _logger.LogInformation("+FixAccount");

                if (obj == null) return;

                var account = obj as SenderAccount;

                //var _api = MultipleHelper.ApiList.FirstOrDefault(i => i.GetLoggedUser().LoggedInUser.UserName.ToLower().Equals(account.Username.ToLower()));
                //var info = await _api.WebProcessor.GetAccountInfoAsync();
                //if(info.Succeeded)
                //{
                //    account.JoinDate = (DateTime)info.Value.JoinedDate;
                //}

                if (!string.IsNullOrEmpty(account.ChallengeURL))
                {
                    await Clipboard.SetTextAsync($"{account.Username}\t{account.Password}");
                    await App.Current.MainPage.ShowPopupAsync(new ChallengePopup(account.ChallengeURL));

                    account.HasIssue = false;
                    account.ChallengeURL = string.Empty;
                    var upd = await _senderDataStore.UpdateItemAsync(account);
                    if (upd)
                        await LoadSendersCommand.ExecuteAsync(null);
                }
                else
                {
                    account.HasIssue = false;
                    var upd = await _senderDataStore.UpdateItemAsync(account);
                    if (upd)
                        await LoadSendersCommand.ExecuteAsync(null);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-FixAccount");
            }

        }

        [ICommand]
        async Task RefreshAccounts()
        {
            try
            {
                _logger.LogInformation("+RefreshAccounts");
                await LoadSendersCommand.ExecuteAsync(null);
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-RefreshAccounts");
            }
        }

        [ICommand]
        async Task DeleteSender(object obj)
        {
            try
            {
                _logger.LogInformation("+DeleteSender");
                if (obj == null) return;

                var account = obj as SenderAccount;

                var _api = MultipleHelper.ApiList.FirstOrDefault(i => i.GetLoggedUser().LoggedInUser.UserName.ToLower().Equals(account.Username.ToLower()));
                var ret = await _api.LogoutAsync();
                if (ret.Succeeded)
                {
                    var file = account.Username.GetAccountPath();

                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        var del = await _senderDataStore.DeleteItemAsync(account.Id);
                        await LoadSendersCommand.ExecuteAsync(null);
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
                _logger.LogInformation("-DeleteSender");
            }
        }
        #endregion
    }
}
