using CustomExtensions;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace FlashCommon
{
    public class AzureService : AzureUtil, IAzure
    {
        // Azure requires an id field whereas in Firebase the id is the collection key
        private class AzureUser : User { public string id { get; set; } }

        private ReplaySubject<string> _currentUser = new ReplaySubject<string>(1);

        public DBNames Name { get { return DBNames.Azure; } }
        public IObservable<string> CurrentUserId { get { return _currentUser; } }

        // Set the main user of the app
        public void SetCurrentUserId(string uid)
        {
            _currentUser.OnNext(uid);
        }

        // A new user has logged in: create an entry in the Users collection
        public IObservable<Unit> AddNewUser(User user)
        {
            var azure = new AzureUser()
            {
                accountType = user.accountType,
                currentTopicId = user.currentTopicId,
                displayName = user.displayName,
                id = user.uid,
                uid = user.uid
            };
            return Save("users", azure);
        }

        // Get the user's list of language pairs  (i.e. en-de). Usually there is only one.
        public IObservable<User> GetUserInfo(string uid)
        {
            var query = $"SELECT * FROM c WHERE c.id = '{uid}'";
            return QuerySingle<User>("users", query);
        }

        // Get the user's list of card decks (categories of prompt-response pairs).
        // (Usually there is only one Topic object.)
        public IObservable<IList<Topic>> GetTopics(string uid)
        {
            var query = $"SELECT * FROM c WHERE c.uid = '{uid}' ";
            return QueryList<Topic>("topics", query);
        }

        // Get the pairs for a deck (for the recording page)
        public IObservable<Deck> GetPairs(string uid, Deck deck)
        {
            //var query = $"SELECT * FROM c WHERE c.uid = '{uid}' AND c.deckId = {deck.id} ORDER BY c.order";
            // TODO: ORDER BY not working here
            var query = $"SELECT * FROM c WHERE c.uid = '{uid}' AND c.deckId = {deck.id}";
            return QueryList<PromptResponsePair>("prpairs", query).Select(pairs => {
                deck.groups = pairs;
                return deck;
            });
        }

        // Gets the currently active pairs for the playback page
        public IObservable<IList<PromptResponsePair>> GetCurrentPairs(string uid, long currentTopicId)
        {
            //var query = $"SELECT * FROM c WHERE c.uid = '{uid}' AND c.topicId = {currentTopicId} AND c.isActive = true ORDER BY c.nextDate OFFSET 1 LIMIT 20";
            var query = $"SELECT * FROM c WHERE c.uid = '{uid}' AND c.topicId = {currentTopicId} AND c.isActive = true";
            return QueryList<PromptResponsePair>("prpairs", query);
        }

        // Get the audio data from the database for a prompt or response
        private class DbBlob { public string id { get; set; } public string type = "audio"; public string blob { get; set; } };
        public IObservable<FirestoreBlob> GetRecording(string uid, Prompt prompt)
        {
            var key = $"{uid}_{prompt.id}";
            var query = $"SELECT * FROM c WHERE c.id = '{key}'";
            return QuerySingle<DbBlob>("blobs", query).Select(b => 
            {
                return new FirestoreBlob(b.type, b.blob);
            });
        }

        public IObservable<Unit> SaveRecording(string uid, Prompt prompt, FirestoreBlob blob)
        {
            var key = $"{uid}_{prompt.id}";
            return Save("blobs", new DbBlob() { id = key, blob = blob.data64 });
        }

        // Save the topic structure
        public IObservable<Unit> SaveTopic(Topic topic)
        {
            // We don't want to save Deck.Groups. (This is done with a decorator in Firebase.)
            // TO DO: Is there some other way to ignore a property in Azure?
            return Save("topics", topic.NoGroupsCopy());
        }

        // Save a prompt-response pair
        public IObservable<Unit> SavePromptPair(PromptResponsePair pair)
        {
            return Save("prpairs", pair);
        }

        // No longer used
        // Save a list of prompt-response pairs
        //public IObservable<Unit> SavePromptPairs(List<PromptResponsePair> pairs)
        //{
        //    var c = Container("prpairs");
        //    List<Task> concurrentTasks = new List<Task>();
        //    foreach (var itemToInsert in pairs)
        //    {
        //        concurrentTasks.Add(c.UpsertItemAsync(itemToInsert));
        //    }
        //    return Task.WhenAll(concurrentTasks).AsUnit();
        //}

        // Delete a prompt-response pair
        public IObservable<Unit> DeletePair(PromptResponsePair pair)
        {
            var c = Container("prpairs");
            return c.DeleteItemAsync<PromptResponsePair>(pair.id.ToString(), new PartitionKey(pair.deckId)).AsUnit();
        }

        // Delete a recording
        public IObservable<Unit> DeleteBlob(string uid, Prompt prompt)
        {
            return Container("blobs").DeleteItemAsync<DbBlob>($"{uid}_{prompt.id}", new PartitionKey("audio")).AsUnit();
        }
    }
}
