namespace DMDynamite.ViewModels
{
    public partial class HomeViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly ILogger<HomeViewModel> _logger;
        private readonly ISenderDataStore _senderDataStore;
        private readonly IInstagramService _instagramService;
        private readonly IActivityDataStore _activityDataStore;
        private readonly IMessageDataStore _messageDataStore;
        private readonly IProxyDataStore _proxyDataStore;
        private readonly IRecipientDataStore _recipientDataStore;
        private Timer _timer;
        private int time_interval;
        #endregion

        #region Properties
        [ObservableProperty]
        string message;

        [ObservableProperty]
        int successCount;

        [ObservableProperty]
        int failureCount;

        [ObservableProperty]
        bool isSending;

        [ObservableProperty]
        string status;
        #endregion

        #region Constructor
        public HomeViewModel(
            ILogger<HomeViewModel> logger,
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

        //send
        #region Commands
        [ICommand]
        async Task Setup()
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("+Setup");
                Status = "Ready";
                MultipleHelper.LoadSessions();
                //get message
                Message = Preferences.Get(nameof(Message), string.Empty);
                time_interval = Preferences.Get("TimeInterval", 10);

                var activities = await _activityDataStore.GetItemsByDateAsync(DateTime.Today, DateTime.Now);

                if (activities != null && activities.Any())
                {
                    SuccessCount = activities.Count(i => i.IsSuccessful);
                    FailureCount = activities.Count(i => i.IsSuccessful == false);
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
                _logger.LogInformation("-Setup");
            }
        }

        [ICommand]
        async Task Send()
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("+Send");

                if (string.IsNullOrEmpty(message))
                    return;
                    
                Preferences.Set(nameof(Message), message);

                // create task list
                var tasks = new List<Task<IResult<string>>>();
                // get all senders
                var senders = await _senderDataStore.GetItemsAsync();
                var recipients = await _recipientDataStore.GetItemsByOffsetAsync(0, senders.Count());

                // create tasks
                for(int i = 0; i < senders.Count(); i++)
                {
                    if (!senders.ElementAt(i).HasIssue)
                    {
                        IInstaApi api = MultipleHelper.ApiList.FirstOrDefault(x => x.GetLoggedUser().LoggedInUser.UserName.ToLower().Equals(senders.ElementAt(i).Username.ToLower(), StringComparison.Ordinal));
                        if(api != null)
                            tasks.Add(_instagramService.SendMessage(api, message, recipients.ElementAt(i)));
                    }
                }

                // wait for task to be empty.
                while (tasks.Any())
                {
                    Task<IResult<string>> finishedTask = await Task.WhenAny(tasks);
                    tasks.Remove(finishedTask);
                    var result = await finishedTask;
                    if (result != null)
                    {
                        var accs = result.Value.Split(':');

                        var sender = senders.FirstOrDefault(i => i.Username.ToLower().Equals(accs[0].ToLower()));
                        var recipient = recipients.FirstOrDefault(i => i.Username.ToLower().Equals(accs[1].ToLower()));
                        
                        // if good: pop from recipients list
                        if (result.Succeeded)
                        {                            
                            var ret = await _recipientDataStore.DeleteItemAsync(recipient.Id);
                        }
                        else
                        {
                            if (!result.Info.Message.Contains("The SSL connection could not be established"))
                            {
                                //sender = senders.FirstOrDefault(i => i.Username.ToLower().Equals(result.Info.Message.Split('-')[0].ToLower()));
                                // if bad: remove problematic sender                                
                                if (result.Info.ResponseType == ResponseType.ChallengeRequired && !string.IsNullOrEmpty(result.Info.Message))
                                    sender.ChallengeURL = result.Info.Message;

                                sender.HasIssue = true;

                                var update = await _senderDataStore.UpdateItemAsync(sender);
                                if(!update)
                                    _logger.LogError($"DB Update sender FAILED[{sender.Username}]");

                                _logger.LogError($"sender FAILED[{sender.Username}] - {result.Info.Message}");
                            }
                        }

                        await _activityDataStore.AddItemAsync(new Activity
                        {
                            IsDeleted = false,
                            Message = message,
                            RecipientFK = recipient.Id,
                            SenderFK = sender.Id,
                            Status = string.Empty,
                            IsSuccessful = result.Succeeded
                        });
                    }
                }

                var activities = await _activityDataStore.GetItemsByDateAsync(DateTime.Today, DateTime.Now);

                if(activities != null && activities.Any())
                {
                    SuccessCount = activities.Count(i => i.IsSuccessful);
                    FailureCount = activities.Count(i => i.IsSuccessful == false);
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                IsBusy = false;
                _logger.LogInformation("-Send");
            }
        }

        [ICommand]
        async Task BackgroundSend()
        {
            try
            {
                _logger.LogInformation("+BackgroundSend");
                var startTimeSpan = TimeSpan.Zero;
                var rand = new Random();
                time_interval = rand.Next(13,15);
                var periodTimeSpan = TimeSpan.FromMinutes(time_interval);

                IsSending = true;
                _timer = new Timer((e) =>
                {
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var nextBlast = DateTime.Now.AddMinutes(time_interval).ToString("G");
                        Status = $"Sending.. next blast [{nextBlast}]";

                        await SendCommand.ExecuteAsync(null);
                    });
                }, null, startTimeSpan, periodTimeSpan);
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message);
                throw;
            }
            finally
            {
                _logger.LogInformation("-BackgroundSend");
            }
        }

        [ICommand]
        async Task BackgroundCancelSend()
        {
            try
            {
                Status = "Ready";
                _logger.LogInformation("+BackgroundCancelSend");
                IsSending = false;
                if (_timer != null)
                    _timer.Dispose();
            }
            finally
            {
                _logger.LogInformation("-BackgroundCancelSend");
            }
        }
        #endregion
    }
}
