namespace DMDynamite.ViewModels
{
    public partial class ReportsViewModel : BaseViewModel
    {
        #region Private Fields
        private readonly ILogger<ReportsViewModel> _logger;
        private readonly ISenderDataStore _senderDataStore;
        private readonly IInstagramService _instagramService;
        private readonly IActivityDataStore _activityDataStore;
        private readonly IMessageDataStore _messageDataStore;
        private readonly IProxyDataStore _proxyDataStore;
        private readonly IRecipientDataStore _recipientDataStore;
        #endregion

        #region Properties
        public ObservableCollection<Activity> Activities { get; } = new();
        #endregion

        #region Constructor
        public ReportsViewModel(
            ILogger<ReportsViewModel> logger,
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
            try
            {
                _logger.LogInformation("+Setup");
                var activities = await _activityDataStore.GetItemsByDateAsync(DateTime.Today, DateTime.Now);

                if (activities.Any())
                {
                    Activities.Clear();
                    foreach(var activity in activities)
                    {
                        Activities.Add(new Activity
                        {
                            Id = activity.Id,
                            IsDeleted = activity.IsDeleted,
                            IsSuccessful = activity.IsSuccessful,
                            //Message = msg.Body.Replace("\r",string.Empty),
                            Message = activity.Message,
                            RecipientFK = activity.RecipientFK,
                            SenderFK = activity.SenderFK,
                            Status = activity.Status,
                            CreatedDate = activity.CreatedDate,
                            UpdatedDate = activity.UpdatedDate
                        });
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
                _logger.LogInformation("-Setup");
            }
        } 
        #endregion
    }
}
