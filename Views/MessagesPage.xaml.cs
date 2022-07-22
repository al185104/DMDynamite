namespace DMDynamite.Views;

public partial class MessagesPage : ContentPage
{
    private MessagesViewModel _vm;

    public MessagesPage(MessagesViewModel vm)
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