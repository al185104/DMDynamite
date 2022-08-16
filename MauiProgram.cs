namespace DMDynamite;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
			{
                fonts.AddFont("Feather.ttf", "Icons");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("GothamBook.ttf", "Regular");
                fonts.AddFont("GothamBook-Bold.otf", "RegularBold");
                fonts.AddFont("Capuche.otf", "Header");
            })
            .ConfigureEssentials(essentials =>
            {
                essentials.UseMapServiceToken("rTKSR1YQSsN5VN6iGQAF~UTO-c8uisgsH-U1LFZqttA~AotqbvMmnAPS46bGHAoKI9cWl-VJASyYLJ7A1HmN-KxMV7R-qsdlCR7AApKImaTZ");
            });

        var service = builder.Services;

        service.AddLogging(configure => {
            var _loggingProvider = new LoggerFileProvider("C:\\Logs\\DMDynamite.log");
            _loggingProvider.CreateLogger("Information");
            configure.AddProvider(_loggingProvider);
        });

        service.AddSingleton<PopupSizeConstants>();

        #region Pages
        service.AddSingleton<MainPage>();
        service.AddSingleton<AccountsPage>();
        service.AddSingleton<RecipientsPage>();
        service.AddSingleton<HomePage>();
        service.AddSingleton<ReportsPage>();
        service.AddSingleton<MessagesPage>();
        service.AddSingleton<SettingsPage>();
        #endregion

        #region ViewModels
        service.AddSingleton<MainViewModel>();
        service.AddSingleton<AccountsViewModel>();
        service.AddSingleton<RecipientsViewModel>();
        service.AddSingleton<HomeViewModel>();
        service.AddSingleton<ReportsViewModel>();
        service.AddSingleton<MessagesViewModel>();
        service.AddSingleton<SettingsViewModel>();
        #endregion

        #region Popup
        service.AddTransient<LoginPopup>();
        service.AddTransient<ProxyPopup>();
        service.AddTransient<ChallengePopup>();
        service.AddTransient<NewMessagePopup>();
        #endregion

        #region Essentials Services
        service.AddSingleton<IDeviceInfo>(DeviceInfo.Current);
        service.AddSingleton<IDeviceDisplay>(DeviceDisplay.Current);
        service.AddSingleton<IConnectivity>(Connectivity.Current);
        service.AddSingleton<IGeolocation>(Geolocation.Default);
        service.AddSingleton<IMap>(Map.Default);
        service.AddSingleton<IInstagramService, InstagramService>();
        service.AddSingleton<IActivityDataStore, ActivityDataStore>();
        service.AddSingleton<IMessageDataStore, MessageDataStore>();
        service.AddSingleton<IRecipientDataStore, RecipientDataStore>();
        service.AddSingleton<ISenderDataStore, SenderDataStore>();
        service.AddSingleton<IProxyDataStore, ProxyDataStore>();
        #endregion

        return builder.Build();
	}
}
