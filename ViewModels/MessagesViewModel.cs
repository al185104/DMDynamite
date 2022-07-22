namespace DMDynamite.ViewModels
{
    public partial class MessagesViewModel : BaseViewModel
    {
        #region Private Fields
        IInstaApi _api;
        private readonly ILogger<MessagesViewModel> _logger;
        private readonly ISenderDataStore _senderDataStore;
        private readonly IInstagramService _instagramService;
        private readonly IActivityDataStore _activityDataStore;
        private readonly IMessageDataStore _messageDataStore;
        private readonly IProxyDataStore _proxyDataStore;
        private readonly IRecipientDataStore _recipientDataStore;
        #endregion

        #region Properties
        public ObservableCollection<Message> Messages { get; } = new();
        public ObservableCollection<object> SelectedMessages { get; } = new();
        #endregion

        #region Constructor
        public MessagesViewModel(
            ILogger<MessagesViewModel> logger,
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
                _logger.LogInformation("+Setup Messages");
                var msgs = await _messageDataStore.GetItemsAsync();

                IsBusy = true;
                if (msgs != null && msgs.Any())
                {
                    Messages.Clear();
                    foreach (var msg in msgs)
                        Messages.Add(msg);
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
                _logger.LogInformation("-Setup Messages");
            }
        }

        [ICommand]
        async Task DeleteMessage()
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("+DeleteMessage");

                if(SelectedMessages.Count > 0)
                {
                    foreach(var obj in SelectedMessages)
                    {
                        if(obj != null)
                        {
                            var msg = obj as Message;
                            var ret = await _messageDataStore.DeleteItemAsync(msg.Id);
                            if (ret)
                                Messages.Remove(msg);
                        }
                    }
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
                _logger.LogInformation("-DeleteMessage");
            }
        }

        [ICommand]
        async Task AddMessage()
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("+AddMessage");

                var obj = await App.Current.MainPage.ShowPopupAsync(new NewMessagePopup());

                if (obj != null)
                {
                    var msg = obj as Message;
                    var ret = await _messageDataStore.AddItemAsync(msg);
                    if (ret)
                        Messages.Add(msg);
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
                _logger.LogInformation("-AddMessage");
            }
        }
        #endregion
    }
}
