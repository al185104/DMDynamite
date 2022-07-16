namespace DMDynamite.Views;

public partial class ReportsPage : ContentPage
{
    private ReportsViewModel _vm;

    public ReportsPage(ReportsViewModel vm)
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