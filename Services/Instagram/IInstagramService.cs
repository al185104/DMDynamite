namespace DMDynamite.Services.InstagramService
{
    public interface IInstagramService
    {
        Task<IResult<InstaLoginResult>> LoginAccount(IInstaApi api, string username, string password);
        Task<IResult<InstaLoginTwoFactorResult>> TwoFactorAuthentication(IInstaApi api, string username, string code);
        void SaveSession(IInstaApi api, string username);
        Task<IResult<string>> SendMessage(IInstaApi api, string message, RecipientAccount recipients);
        Task<IResult<InstaUserShortList>> SearchByHashTagAsync(IInstaApi api, string search, PaginationParameters pagination, int followersCountReq, bool searchTop = false);
        Task<IResult<InstaUserShortList>> SearchByLocationTagAsync(IInstaApi api, double latitude, double longitude, string search, PaginationParameters pagination, int followersCountReq, bool searchTop = false);
    }
}
