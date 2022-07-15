namespace DMDynamite.Views;

public partial class AccountsPage : ContentPage
{
    private AccountsViewModel _vm;

    public AccountsPage(AccountsViewModel vm)
	{
		InitializeComponent();
		BindingContext = _vm = vm;
	}

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        _vm.LoadSendersCommand.Execute(null);
    }
}