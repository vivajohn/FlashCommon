using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using System.Diagnostics;
using Google.Protobuf;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Google.Api;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Google.Cloud.Firestore.Converters;
using System.Reactive;

namespace FlashCommon
{
    // Access the Cloud Firestore database
    public class FirebaseService : IFirebase
    {
        private static readonly string projectId = "flashdev-69399";

        private FirestoreDb db = FirestoreDb.Create(projectId);
        private ReplaySubject<User> rs = null;

        public string Name { get { return "Firebase"; } }

        // Get the user's list of language pairs  (i.e. en-de). Usually there is only one.
        public IObservable<User> GetUserInfo(string uid)
        {
            if (rs == null) 
            {
                rs = new ReplaySubject<User>(1);
                var o = db.Collection("users").Document(uid).GetSnapshotAsync().ToObservable();
                o.Subscribe(snapshot => { 
                    var user = snapshot.ConvertTo<User>();
                    rs.OnNext(user);
                });
            }
            return rs;
        }

        // Get the user's list of card decks (categories of prompt-response pairs).
        // (Usually there is only one Topic object.)
        public IObservable<IList<Topic>> GetTopics(string uid)
        {
            var o = db.Collection("topics").WhereEqualTo("uid", uid).GetSnapshotAsync().ToObservable();
            return o.Select(snapshot => {
                var docs = snapshot.Documents.ToArray();
                var topics = docs.Select(x => x.ConvertTo<Topic>());
                //var topics = Array.ConvertAll<DocumentSnapshot, Topic>(
                //    docs, x => x.ConvertTo<Topic>());
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
                return new FirestoreBlob(blobType, data);
            });
        }

        // Save the topic structure
        public IObservable<Unit> SaveTopic(Topic topic)
        {
            var key = $"{topic.uid}_{topic.id}";
            var dic = ToDictionary(topic);
            //var o = db.Collection("topics").Document(key).SetAsync(dic).ToObservable();
            //return o.Select(x => Unit.Default);
            return UnitTask(db.Collection("topics").Document(key).SetAsync(dic));
        }

        // Save a prompt-response pair
        public IObservable<Unit> SavePromptPair(PromptResponsePair pair)
        {
            var key = $"{pair.uid}_{pair.id}";
            var dic = ToDictionary(pair);
            //var o = db.Collection("prpairs").Document(key).SetAsync(dic).ToObservable();
            //return o.Select(x => Unit.Default);
            return UnitTask(db.Collection("prpairs").Document(key).SetAsync(dic));
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
            //return batch.CommitAsync().ToObservable().Select(x => Unit.Default);
            return UnitTask(batch.CommitAsync());
        }

        // Delete a prompt-response pair
        public IObservable<Unit> DeletePair(PromptResponsePair pair)
        {
            throw new NotImplementedException();
        }

        private IObservable<Unit> UnitTask(Task t)
        {
            return t.ToObservable().Select(x => Unit.Default);
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
