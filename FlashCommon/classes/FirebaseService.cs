using System;
using System.Collections.Generic;
using System.Linq;
using Google.Cloud.Firestore;
using Google.Protobuf;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Subjects;
using System.Reactive;
using CustomExtensions;
using System.Diagnostics;
using Google.Apis.Auth.OAuth2;
using System.Text.Json;
using Google.Cloud.Firestore.V1;

namespace FlashCommon
{
    // Access the Cloud Firestore database
    public class FirebaseService : IFirebase
    {
        private FirestoreDb db;
        private Dictionary<string, User> users = new Dictionary<string, User>();
        private ReplaySubject<string> _currentUser = new ReplaySubject<string>(1);

        public DBNames Name { get { return DBNames.Firebase; } }
        public IObservable<string> CurrentUserId { get { return _currentUser; } }

        public FirebaseService(string projectId)
        {
            db = FirestoreDb.Create(projectId);
        }

        // Set the main user of the app
        public void SetCurrentUserId(string uid)
        {
            _currentUser.OnNext(uid);
        }

        // A new user has logged in: create an entry in the Users collection
        public IObservable<Unit> AddNewUser(User user)
        {
            return db.Collection("users").Document(user.uid).SetAsync(user).AsUnit();
        }

        // Get the user's list of language pairs  (i.e. en-de). Usually there is only one.
        public IObservable<User> GetUserInfo(string uid)
        {
            if (users.ContainsKey(uid))
            {
                return Observable.Return(users[uid]);
            }
            var o = db.Collection("users").Document(uid).GetSnapshotAsync().ToObservable();
            return o.Select(snapshot => { 
                var user = snapshot.ConvertTo<User>();
                if (user != null)
                {
                    users[uid] = user;
                }
                return user;
            });
        }

        // Get the user's list of card decks (categories of prompt-response pairs).
        // (Usually there is only one Topic object.)
        public IObservable<IList<Topic>> GetTopics(string uid)
        {
            var o = db.Collection("topics").WhereEqualTo("uid", uid).GetSnapshotAsync().ToObservable();
            return o.Select(snapshot => {
                var docs = snapshot.Documents.ToArray();
                var topics = docs.Select(x => x.ConvertTo<Topic>());
                return new List<Topic>(topics);
            });
        }

        // Get the pairs for a deck (for the recording page)
        public IObservable<Deck> GetPairs(string uid, Deck deck)
        {
            var o = db.Collection("prpairs").WhereEqualTo("uid", uid).WhereEqualTo("deckId", deck.id).OrderBy("order").GetSnapshotAsync().ToObservable();
            return o.Select(snapshot => {
                var docs = snapshot.Documents.ToArray();
                deck.groups = Array.ConvertAll<DocumentSnapshot, PromptResponsePair>(
                   docs, x => x.ConvertTo<PromptResponsePair>()).ToList<PromptResponsePair>();
                return deck;
            });
        }

        // Gets the currently active pairs for the playback page
        public IObservable<IList<PromptResponsePair>> GetCurrentPairs(string uid, long currentTopicId)
        {
            var o = db.Collection("prpairs")
            .WhereEqualTo("uid", uid)
            .WhereEqualTo("topicId", currentTopicId)
            .WhereEqualTo("isActive", true)
            .OrderBy("nextDate")
            .Limit(20)
            .GetSnapshotAsync().ToObservable();

            return o.Select((snapshot) =>
            {
                var docs = snapshot.Documents.ToArray();
                var array = Array.ConvertAll(
                    docs, x => x.ConvertTo<PromptResponsePair>()).ToList();
                return array;
            });
        }

        // Get the audio data from the database for a prompt or response
        public IObservable<FirestoreBlob> GetRecording(string uid, Prompt prompt)
        {
            var key = $"{uid}_{prompt.id}";
            var o = db.Collection("blobs").Document(key).GetSnapshotAsync().ToObservable();

            return o.Select((snapshot) =>
            {
                // The returned data is a Firestore blob which has its own particular format
                var blobType = snapshot.GetValue<string>("type");
                var data = snapshot.GetValue<ByteString>("blob");
                return new FirestoreBlob(blobType, data.ToBase64());
            });
        }

        public IObservable<Unit> SaveRecording(string uid, Prompt prompt, FirestoreBlob blob)
        {
            // Firestore has its own blob type, which we must use
            var bytes = ByteString.FromBase64(blob.data64);
            var b = Blob.FromByteString(bytes);
            var data = new BlobDto() { type = blob.blobType, blob = b };
            var key = $"{uid}_{prompt.id}";
            return db.Collection("blobs").Document(key).SetAsync(data).AsUnit();
        }


        // Save the topic structure
        public IObservable<Unit> SaveTopic(Topic topic)
        {
            var key = $"{topic.uid}_{topic.id}";
            var dic = ToDictionary(topic);
            return db.Collection("topics").Document(key).SetAsync(dic).AsUnit();
        }

        // Save a prompt-response pair
        public IObservable<Unit> SavePromptPair(PromptResponsePair pair)
        {
            var key = $"{pair.uid}_{pair.id}";
            var dic = ToDictionary(pair);
            return db.Collection("prpairs").Document(key).SetAsync(dic).AsUnit();
        }

        // Save a list of prompt-response pairs
        public IObservable<Unit> SavePromptPairs(List<PromptResponsePair> pairs)
        {
            var batch = db.StartBatch();
            foreach(var pair in pairs)
            {
                var key = $"{pair.uid}_{pair.id}";
                var dic = ToDictionary(pair);
                batch.Set(db.Collection("prpairs").Document(key), dic);
            }
            return batch.CommitAsync().AsUnit();
        }

        // Delete a prompt-response pair
        public IObservable<Unit> DeletePair(PromptResponsePair pair)
        {
            return db.Collection("prpairs").Document($"{pair.uid}_{pair.id}").DeleteAsync().AsUnit();
        }

        // Delete a recording
        public IObservable<Unit> DeleteBlob(string uid, Prompt prompt)
        {
            return db.Collection("blobs").Document($"{uid}_{prompt.id}").DeleteAsync().AsUnit();
        }

        // Could not find method in Firestore sdk to serialize objects, so using this
        // Ref: https://github.com/googleapis/google-cloud-dotnet/issues/2444
        private Dictionary<string, object> ToDictionary<T>(T model)
        {
            return typeof(T).GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(FirestorePropertyAttribute)))
                .ToDictionary(
                    prop => prop.Name,
                    prop => prop.GetValue(model, null)
                );
        }
    }
}
