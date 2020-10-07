using FlashCommon;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Net.Http;
using System.Reactive.Threading.Tasks;
using System.Text.Json;
using CustomExtensions;

namespace FlashCommon
{
    public class PythonService : IPython
    {
        private ReplaySubject<string> _currentUser = new ReplaySubject<string>(1);
        private Dictionary<string, User> users = new Dictionary<string, User>();
        private HttpClient client;
        private string baseUrl;

        public DBNames Name { get { return DBNames.Python; } }
        public IObservable<string> CurrentUserId { get { return _currentUser; } }


        public PythonService(HttpClient client, string serverUrl)
        {
            this.client = client;
            baseUrl = serverUrl;
        }

        // Set the main user of the app
        public void SetCurrentUserId(string uid)
        {
            _currentUser.OnNext(uid);
        }

        public IObservable<Unit> AddNewUser(User user)
        {
            var content = new StringContent(JsonSerializer.Serialize(user));
            return client.PostAsync($"{baseUrl}/saveuser/{user.uid}", content).AsUnit();
        }

        public IObservable<Unit> DeleteBlob(string uid, Prompt prompt)
        {
            return client.DeleteAsync($"{baseUrl}/deleteblob/{uid}/{prompt.id}").AsUnit();
        }

        public IObservable<Unit> DeletePair(PromptResponsePair pair)
        {
            return client.DeleteAsync($"{baseUrl}/deletepair/{pair.uid}/{pair.id}").AsUnit();
        }

        public IObservable<IList<PromptResponsePair>> GetCurrentPairs(string uid, long currentTopicId)
        {
            return GetHttp<IList<PromptResponsePair>>($"{baseUrl}/currentpairs/{uid}/{currentTopicId}");
        }

        public IObservable<Deck> GetPairs(string uid, Deck deck)
        {
            return GetHttp<IList<PromptResponsePair>>($"{baseUrl}/getpairs/{uid}/{deck.id}").Select(result =>
            {
                deck.groups = result;
                return deck;
            });
        }

        public IObservable<FirestoreBlob> GetRecording(string uid, Prompt prompt)
        {
            return GetHttp<FirestoreBlob>($"{baseUrl}/blob/{uid}/{prompt.id}");
        }

        public IObservable<IList<Topic>> GetTopics(string uid)
        {
            return GetHttp<IList<Topic>>($"{baseUrl}/gettopics/{uid}");
        }


        public IObservable<User> GetUserInfo(string uid)
        {
            if (users.ContainsKey(uid))
            {
                return Observable.Return(users[uid]);
            }
            return GetHttp<User>($"{baseUrl}/user/{uid}").Select(user =>
            {
                if (user != null)
                {
                    users[uid] = user;
                }
                return user;
            });
        }

        public IObservable<Unit> SavePromptPair(PromptResponsePair pair)
        {
            pair.version = 2;
            var content = new StringContent(JsonSerializer.Serialize(pair));
            return client.PostAsync($"{baseUrl}/savepromptpair/{pair.uid}/{pair.id}", content).AsUnit();
        }

        private class FakeBlob { public string type { get; set; } public string blob { get; set; } }
        public IObservable<Unit> SaveRecording(string uid, Prompt prompt, FirestoreBlob blob)
        {
            var content = new StringContent(JsonSerializer.Serialize(blob));
            return client.PostAsync($"{baseUrl}/saveblob/{uid}/{prompt.id}", content).AsUnit();
        }

        public IObservable<Unit> SaveTopic(Topic topic)
        {
            var content = new StringContent(JsonSerializer.Serialize(topic.NoGroupsCopy()));
            return client.PostAsync($"{baseUrl}/savetopic/{topic.uid}/{topic.id}", content).AsUnit();
        }

        private IObservable<T> GetHttp<T>(string url)// where T : class
        {
            // This feature is apparently available in .NET Core 5, which is in preview.
            // How you also have to install Visual Studio preview, so wait for next update...
            //var request = new HttpRequestMessage(HttpMethod.Get, url);
            //request.SetBrowserRequestCache(BrowserRequestCache.NoCache);
            return client.GetAsync(url).ToObservable().SelectMany(response =>
            {
                return response.Content.ReadAsStringAsync().ToObservable().Select(str =>
                {
                    if (String.IsNullOrEmpty(str))
                    {
                        return default(T);
                    }
                    var obj = JsonSerializer.Deserialize<T>(str);
                    return obj;
                });
            });
        }

    }
}
