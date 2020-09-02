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

namespace FlashCommon
{
    // Access the Cloud Firestore database
    public class FirebaseService : IFirebase
    {
        private static readonly string projectId = "flashdev-69399";

        private FirestoreDb db = FirestoreDb.Create(projectId);
        private ReplaySubject<User> rs = null;

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

        // Get the user's list of language pairs  (i.e. en-de). Usually there is only one.
        public IObservable<Topic[]> GetTopics(string uid)
        {
            var o = db.Collection("topics").WhereEqualTo("uid", uid).GetSnapshotAsync().ToObservable();
            return o.Select(snapshot => {
                var docs = snapshot.Documents.ToArray();
                var topics = Array.ConvertAll<DocumentSnapshot, Topic>(
                    docs, x => x.ConvertTo<Topic>());
                return topics;
            });
        }

        // Get the pairs for a deck (for the recording page)
        public IObservable<Deck> GetPairs(string uid, Deck deck)
        {
            var o = db.Collection("prpairs").WhereEqualTo("uid", uid).WhereEqualTo("deckId", deck.id).GetSnapshotAsync().ToObservable();
            return o.Select(snapshot => {
                var docs = snapshot.Documents.ToArray();
                deck.groups = Array.ConvertAll<DocumentSnapshot, PromptResponsePair>(
                   docs, x => x.ConvertTo<PromptResponsePair>()).ToList<PromptResponsePair>();
                return deck;
            });
        }

        // Gets the currently active pairs for the playback page
        public IObservable<List<PromptResponsePair>> GetCurrentPairs(string uid, long currentTopicId)
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

    }
}
