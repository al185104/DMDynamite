namespace DMDynamite.ViewModels
{
    public partial class RecipientsViewModel : BaseViewModel
    {
        #region Private Fields
        IInstaApi _api;
        private readonly ILogger<RecipientsViewModel> _logger;
        private readonly ISenderDataStore _senderDataStore;
        private readonly IInstagramService _instagramService;
        private readonly IActivityDataStore _activityDataStore;
        private readonly IMessageDataStore _messageDataStore;
        private readonly IProxyDataStore _proxyDataStore;
        private readonly IRecipientDataStore _recipientDataStore;
        #endregion

        #region Properties
        public ObservableCollection<RecipientAccount> Recipients { get; } = new();

        [ObservableProperty]
        int totalCount;

        [ObservableProperty]
        SendingOption sendingOption;

        [ObservableProperty]
        bool searchFromTop = true;

        [ObservableProperty]
        double latitude;

        [ObservableProperty]
        double longitude;
        #endregion

        #region Constructor
        public RecipientsViewModel(
            ILogger<RecipientsViewModel> logger,
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

        #region Commands
        [ICommand]
        async Task Setup()
        {
            await LoadRecipients();

            if(!MultipleHelper.ApiList.Any())
                MultipleHelper.LoadSessions();

            var api = MultipleHelper.ApiList.FirstOrDefault();
            if (api != null)
                _api = api;
        }

        [ICommand]
        private async Task LoadRecipients()
        {
            try
            {
                _logger.LogInformation("+LoadRecipients");
                var queue = await _recipientDataStore.GetItemsByOffsetAsync(0, 50);
                var count = await _recipientDataStore.GetTotalCount();

                if (queue.Any())
                {
                    MainThread.BeginInvokeOnMainThread(() => { 
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
                    foreach (var follower in followerList.Value.Distinct())
                    {
                        var instaAccount = new RecipientAccount
                        {
                            Pk = follower.Pk.ToString(),
                            Username = follower.UserName,
                            Fullname = follower.FullName,
                            ProfilePicture = follower.ProfilePicture,
                            IsVerified = follower.IsVerified,
                            IsPrivate = follower.IsPrivate,
                        };

                        var result = await _recipientDataStore.AddItemAsync(instaAccount);
                        if (!result)
                            _logger.LogError($"Something wrong in adding {instaAccount.Fullname} to recipients.");
                    }

                    await LoadRecipients();
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
        async Task ChangeSearch(SendingOption option)
        {
            IsBusy = true;
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

            IsBusy = false;
        }

        [ICommand]
        async Task DeleteAllRecipients()
        {
            try
            {
                _logger.LogInformation("+DeleteAllRecipients");
                var ret = await App.Current.MainPage.DisplayAlert("Delete all recipients",$"Are you sure you want to delete all {Recipients.Count} recipients?","Delete", "Back");
                if (ret)
                {
                    var result = await _recipientDataStore.DeleteAllItemsAsync();
                    if (result < 0)
                        _logger.LogError("Something went wrong in deleting entries.");
                    else
                        await LoadRecipients();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-DeleteAllRecipients");
            }
        }
        #endregion
    }
}
