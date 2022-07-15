using CommunityToolkit.Maui.Views;

namespace DMDynamite.Views.Popups;

public partial class LoginPopup : Popup
{
	public LoginPopup(IEnumerable<ProxySetup> proxies)
	{
		InitializeComponent();

		proxy.ItemsSource = proxies.ToList();
        proxy.SelectedIndex = 0;
	}

    private void submitBtn_Clicked(object sender, EventArgs e)
    {
        if (
            string.IsNullOrEmpty(username.Text) ||
            string.IsNullOrEmpty(password.Text) 
            )
            return;

        if(proxy.SelectedItem != null)
        {
            var pr = proxy.SelectedItem as ProxySetup;

            var acc = new SenderAccount
            {
                Username = username.Text,
                Password = password.Text,
                ProxyFK = pr.Id
            };

            Close(acc);
        }

        Close(null);
    }
}