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
        if (!string.IsNullOrEmpty(formattedProxy.Text) &&
            !string.IsNullOrEmpty(name.Text) && 
            !string.IsNullOrEmpty(startNumber.Text))
        {
            List<ProxySetup> proxies = new List<ProxySetup>();
            //skanincbusiness:968zrqD0TiInf673_country-UnitedStates_session-GnOkjnl3:100.24.154.229:31112
            var proxyList = formattedProxy.Text.Split("\r");

            for (int i = 0; i < proxyList.Length; i++)
            {
                if (!string.IsNullOrEmpty(proxyList[i]))
                {
                    var prx = proxyList[i];

                    var info = prx.Split(':');

                    int num;
                    if (int.TryParse(startNumber.Text, out num))
                    {
                        proxies.Add(new ProxySetup
                        {
                            Id = Guid.NewGuid(),
                            CreatedDate = DateTime.Now,
                            ProxyName = $"{name.Text}{(num + i).ToString("00")}",
                            ProxyHost = info[2],
                            ProxyPort = info[3],
                            ProxyPassword = info[1],
                            ProxyUsername = info[0]
                        });
                    } 
                }
            }
            Close(proxies);
        }
        else
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

    private void downloadBtn_Clicked(object sender, EventArgs e)
    {
        List<ProxySetup> proxies = new List<ProxySetup>();
        //skanincbusiness:968zrqD0TiInf673_country-UnitedStates_session-GnOkjnl3:100.24.154.229:31112
        var proxyList = formattedProxy.Text.Split(Environment.NewLine);

        for(int i = 0; i < proxyList.Length; i++)
        {
            var proxy = proxyList[i];

            var info = proxy.Split(':');

            int num;
            if (int.TryParse(startNumber.Text, out num))
            {
                proxies.Add(new ProxySetup
                {
                    Id = Guid.NewGuid(),
                    CreatedDate = DateTime.Now,
                    ProxyName = $"{name.Text}{(num + i).ToString("##")}",
                    ProxyHost = info[2],
                    ProxyPort = info[3],
                    ProxyPassword = info[1],
                    ProxyUsername = info[0]
                });
            }
        }
    }
}