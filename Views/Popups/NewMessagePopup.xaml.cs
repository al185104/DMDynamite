namespace DMDynamite.Views.Popups;

public partial class NewMessagePopup : Popup
{
	public NewMessagePopup()
	{
		InitializeComponent();
	}

    private void btnBack_Clicked(object sender, EventArgs e)
    {
        if (
            string.IsNullOrEmpty(body.Text) ||
            string.IsNullOrEmpty(subject.Text)
            )
            return;

        var msg = new Message
        {
            Body = body.Text,
            Subject = subject.Text
        };

        Close(msg);
    }
}