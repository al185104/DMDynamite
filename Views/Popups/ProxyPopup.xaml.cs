namespace DMDynamite.Views.Popups;

public partial class ProxyPopup : Popup
{
	public ProxyPopup()
	{
		InitializeComponent();
	}

    public async Task ReturnCommand()
    {
        saveBtn_Clicked(null, null);
    }

    private void saveBtn_Clicked(object sender, EventArgs e)
    {
        if (
            string.IsNullOrEmpty(name.Text) ||
            string.IsNullOrEmpty(host.Text) ||
            string.IsNullOrEmpty(port.Text) || 
            string.IsNullOrEmpty(pass.Text) ||
            string.IsNullOrEmpty(uname.Text)
            )
            return;

        var proxy = new ProxySetup()
        {
            Id = Guid.NewGuid(),
            CreatedDate = DateTime.Now,
            ProxyName = name.Text,
            ProxyHost = host.Text,
            ProxyPort = port.Text,
            ProxyPassword = pass.Text,
            ProxyUsername = uname.Text
        };

        Close(proxy);
    }
}