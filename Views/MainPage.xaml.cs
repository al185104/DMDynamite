

namespace DMDynamite;

public partial class MainPage : ContentPage
{
    private MainViewModel _vm;

    public MainPage(MainViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _vm.SetupCommand.Execute(null);
    }
}

