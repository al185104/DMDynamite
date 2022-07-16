namespace DMDynamite.Views;

public partial class HomePage : ContentPage
{
    private HomeViewModel _vm;

    public HomePage(HomeViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
       _vm.SetupCommand.Execute(null);
    }
}