namespace DMDynamite.Views;

public partial class SettingsPage : ContentPage
{
	private SettingsViewModel _vm;

	public SettingsPage(SettingsViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        _vm.LoadSettingsCommand.Execute(null);
    }
}