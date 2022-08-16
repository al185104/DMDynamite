namespace DMDynamite.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        #region Properties
        [ObservableProperty]
        string subscriptionKey;

        [ObservableProperty]
        int timeInterval;

        [ObservableProperty]
        int followersCount;

        [ObservableProperty]
        bool stopOnError;

        [ObservableProperty]
        bool randomizeMessage;

        [ObservableProperty]
        bool randomizeProxy;

        [ObservableProperty]
        int maxPageToLoad = 1;
        #endregion

        #region Constructor
        public SettingsViewModel()
        {

        }
        #endregion

        #region Commands
        [ICommand]
        void SaveSetting()
        {
            Preferences.Set(nameof(SubscriptionKey), SubscriptionKey);
            Preferences.Set(nameof(TimeInterval), TimeInterval);
            Preferences.Set(nameof(FollowersCount), FollowersCount);
            Preferences.Set(nameof(StopOnError), StopOnError);
            Preferences.Set(nameof(RandomizeMessage), RandomizeMessage);
            Preferences.Set(nameof(RandomizeProxy), RandomizeProxy);
            Preferences.Set(nameof(MaxPageToLoad), MaxPageToLoad);
        }

        [ICommand]
        void LoadSettings()
        {
            SubscriptionKey = Preferences.Get(nameof(SubscriptionKey), string.Empty);
            TimeInterval = Preferences.Get(nameof(TimeInterval), 15);
            FollowersCount = Preferences.Get(nameof(FollowersCount), 1000);
            StopOnError = Preferences.Get(nameof(StopOnError), false);
            RandomizeMessage = Preferences.Get(nameof(RandomizeMessage), false);
            RandomizeProxy = Preferences.Get(nameof(RandomizeProxy), false);
            MaxPageToLoad = Preferences.Get(nameof(MaxPageToLoad), 1);
        }
        #endregion
    }
}
