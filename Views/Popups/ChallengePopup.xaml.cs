namespace DMDynamite.Views.Popups;

public partial class ChallengePopup : Popup
{
    //private Uri currentUri;
	public ChallengePopup(string url)
	{
		InitializeComponent();
        webview.Source = url;
        //currentUri = new Uri(url);
        //HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("http://www.stackoverflow.com");
        
        //WebProxy myProxy = new WebProxy("208.52.92.160:80");
        //myRequest.Proxy = myProxy;

        //HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();

        //webview.Source = myResponse.GetResponseStream();

        //webBrowser1.Navigating += new WebBrowserNavigatingEventHandler(webBrowser1_Navigating);


        //webview.NavigateWithHttpRequestMessage = url;
    }

    private void btnBack_Clicked(object sender, EventArgs e) => Close(true);
}