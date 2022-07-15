namespace DMDynamite.Services.InstagramService
{
    public class InstagramService : IInstagramService
    {
        public InstagramService()
        {
            DirectoryHelper.CreateAccountDirectory();
        }

        public async Task<IResult<InstaLoginResult>> LoginAccount(IInstaApi api, string username, string password)
        {
            var sessionHandler = new FileSessionHandler { FilePath = username.GetAccountPath(), InstaApi = api };
            api.SessionHandler = sessionHandler;

            var loginResult = await api.LoginAsync();
            if (loginResult.Succeeded)
            {
                await api.SendRequestsAfterLoginAsync();
                SaveSession(api, username);
            }
            return loginResult;
        }

        public void SaveSession(IInstaApi api, string username)
        {            
            if (api == null)
                return;
            if (!api.IsUserAuthenticated)
                return;

            if (!MultipleHelper.LoggedInUsers.Contains(username))
            {
                MultipleHelper.LoggedInUsers.Add(username);
                MultipleHelper.ApiList.Add(api);
            }

            using (var state = GetStreamData(api))
            {
                using (var fileStream = File.Create(api.SessionHandler.FilePath))
                {
                    state.Seek(0, SeekOrigin.Begin);
                    state.CopyTo(fileStream);
                }
            }
        }

        private Stream GetStreamData(IInstaApi api)
        {
            var o = api.SessionHandler.InstaApi.GetStateDataAsObject();
            var stream = new MemoryStream();
            var json = JsonConvert.SerializeObject(o);
            var bytes = Encoding.UTF8.GetBytes(json);
            stream = new MemoryStream(bytes);
            return stream;
        }

        public T DeserializeFromStream<T>(Stream stream)
        {
            var json = new StreamReader(stream).ReadToEnd();
            return DeserializeFromString<T>(json);
        }

        private T DeserializeFromString<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<IResult<InstaUserShortList>> SearchByHashTagAsync(IInstaApi api, string search, PaginationParameters pagination, int followersCountReq, bool searchTop = false)
        {
            IResult<InstaSectionMedia> hashtags;
            var accounts = new InstaUserShortList();
            string tempNextMaxId = string.Empty;

            do
            {
                if (!string.IsNullOrEmpty(tempNextMaxId))
                    pagination.NextMaxId = tempNextMaxId;

                if (searchTop)
                    hashtags = await api.HashtagProcessor.GetTopHashtagMediaListAsync(search, pagination);
                else
                    hashtags = await api.HashtagProcessor.GetRecentHashtagMediaListAsync(search, pagination);

                if (hashtags.Succeeded)
                {
                    var followers = hashtags.Value.Medias.Select(i => i.User).ToList();
                    foreach(var follower in followers)
                        accounts.Add(follower);

                    tempNextMaxId = hashtags.Value.NextMaxId;
                }
            }
            while (hashtags.Succeeded && hashtags.Value.MoreAvailable && !string.IsNullOrEmpty(hashtags.Value.NextMaxId) && accounts.Count < 630);
            
            if (accounts.Any())
            {
                var ret = Result.Success(accounts);
                ret.Value.NextMaxId = hashtags.Value.MoreAvailable ? hashtags.Value.NextMaxId : string.Empty;
                return ret;
            }
            return Result.Fail(new ArgumentException("hashtags unsuccessful"), default(InstaUserShortList), ResponseType.WrongRequest);
        }

        public async Task<IResult<InstaUserShortList>> SearchByLocationTagAsync(IInstaApi api, double latitude, double longitude, string search, PaginationParameters pagination, int followersCountReq, bool searchTop = false)
        {
            var accounts = new InstaUserShortList();
            IResult<InstaSectionMedia> locationStories;
            string tempNextMaxId = string.Empty;

            do
            {
                if(!string.IsNullOrEmpty(tempNextMaxId))
                    pagination.NextMaxId = tempNextMaxId;

                var results = await api.LocationProcessor.SearchLocationAsync(latitude, longitude, search);
                var firstLocation = results.Value?.FirstOrDefault();
                if (firstLocation == null)
                    return Result.Fail(new ArgumentException("first location unsuccessful"), default(InstaUserShortList), ResponseType.WrongRequest);

                if (searchTop)
                    locationStories = await api.LocationProcessor.GetTopLocationFeedsAsync(long.Parse(firstLocation.ExternalId), pagination);
                else
                    locationStories = await api.LocationProcessor.GetRecentLocationFeedsAsync(long.Parse(firstLocation.ExternalId), pagination);

                if (locationStories.Succeeded)
                {
                    foreach (var follower in locationStories.Value.Medias.Distinct().Where(i=>i.User.IsVerified == false))
                        accounts.Add(follower.User);

                    tempNextMaxId = locationStories.Value.NextMaxId;
                }
                await Task.Delay(1000);
            }
            while (locationStories.Succeeded && locationStories.Value.MoreAvailable && !string.IsNullOrEmpty(locationStories.Value.NextMaxId) && accounts.Count < 630); //630

            if (accounts.Any())
            {
                var ret = Result.Success(accounts);
                ret.Value.NextMaxId = locationStories.Value.MoreAvailable ? locationStories.Value.NextMaxId : string.Empty;
                return ret;
            }
            return Result.Fail(new ArgumentException("location unsuccessful"), default(InstaUserShortList), ResponseType.WrongRequest);
        }

        public async Task<IResult<string>> SendMessage(IInstaApi api, string message, InstagramAccount recipient)
        {
            string err = string.Empty;
            var sender = api.GetLoggedUser().LoggedInUser.UserName;
            var msg = message.Contains("{name}") ? message.Replace("{name}", recipient.Fullname) : message;

            var randDelay = new Random();
            await Task.Delay(randDelay.Next(500, 3000));

            var ret = await api.MessagingProcessor.SendDirectTextAsync(recipient.Pk, null, msg);
            if (ret.Succeeded)
            {
                return Result.Success(recipient.Username);
            }
            else
            {
                if (!string.IsNullOrEmpty(ret.Info.Message))
                {
                    if (ret.Info.NeedsChallenge || ret.Info.Message.Contains("feedback_required"))
                    {
                        var challengeData = await api.GetLoggedInChallengeDataInfoAsync();
                        if (challengeData.Info.ResponseType == ResponseType.OK)
                        {
                            await Task.Delay(100);
                            var accept = await api.AcceptChallengeAsync();
                            if (accept.Succeeded) // resend
                            {
                                var ret2 = await api.MessagingProcessor.SendDirectTextAsync(recipient.Pk, null, msg);
                                if (ret2.Succeeded)
                                    return Result.Success($"{sender} successfully sent a message to {recipient}");
                                else
                                    err = ret2.Info.Message;
                            }
                            else if(!string.IsNullOrEmpty(accept.Info.Message) && accept.Info.Message.Equals("Create a password at least 6 characters long."))
                            {
                                if (!string.IsNullOrEmpty(ret?.Info?.Challenge?.Url))
                                    return Result.Fail<string>($"{sender}- failed to send a message to {recipient} : {accept.Info.Message}", ResponseType.ChallengeRequired, ret?.Info?.Challenge?.Url);
                            }
                            else
                                err = accept.Info.Message;
                        }
                        else if(challengeData.Info.ResponseType == ResponseType.ChallengeRequired)
                        {
                            if (!string.IsNullOrEmpty(challengeData.Info.Challenge.Url))
                                return Result.Fail<string>($"{sender}- failed to send a message to {recipient} : {err}", ResponseType.ChallengeRequired, challengeData.Info.Challenge.Url);
                        }
                    }
                    else
                        err = ret.Info.Message;
                }
            }
            return Result.Fail<string>($"{sender}- failed to send a message to {recipient} : {err}");
        }

        public async Task<IResult<InstaLoginTwoFactorResult>> TwoFactorAuthentication(IInstaApi api, string username, string code)
        {
            var twoFactorLogin = await api.TwoFactorLoginAsync(code);
            if (twoFactorLogin.Succeeded)
                SaveSession(api, username);
            return twoFactorLogin;
        }
    }
}
