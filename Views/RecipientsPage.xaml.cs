namespace DMDynamite.Views;

public partial class RecipientsPage : ContentPage
{
    private RecipientsViewModel _vm;

    public RecipientsPage(RecipientsViewModel vm)
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