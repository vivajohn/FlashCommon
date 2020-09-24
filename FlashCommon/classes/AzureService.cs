using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using System;
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
        public string Name { get { return "Azure"; } }

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
            var query = $"SELECT * FROM c WHERE c.uid = '{uid}' AND c.topicId = {currentTopicId} AND c.isActive = true ORDER BY c.nextDate OFFSET 1 LIMIT 20";
            return QueryList<PromptResponsePair>("prpairs", query);
        }

        // Get the audio data from the database for a prompt or response
        public IObservable<FirestoreBlob> GetRecording(string uid, Prompt prompt)
        {
            throw new NotImplementedException();
        }

        // Save the topic structure
        public IObservable<Unit> SaveTopic(Topic topic)
        {
            return Save("topics", topic);
        }

        // Save a prompt-response pair
        public IObservable<Unit> SavePromptPair(PromptResponsePair pair)
        {
            return Save("prpairs", pair);
        }

        // Save a list of prompt-response pairs
        public IObservable<Unit> SavePromptPairs(List<PromptResponsePair> pairs)
        {
            var c = Container("prpairs");
            List<Task> concurrentTasks = new List<Task>();
            foreach (var itemToInsert in pairs)
            {
                concurrentTasks.Add(c.UpsertItemAsync(itemToInsert));
            }
            return UnitTask(Task.WhenAll(concurrentTasks));
        }

        // Delete a prompt-response pair
        public IObservable<Unit> DeletePair(PromptResponsePair pair)
        {
            var c = Container("prpairs");
            var key = pair.id.ToString();
            return UnitTask(c.DeleteItemAsync<PromptResponsePair>(key, new PartitionKey(pair.deckId)));
        }
    }
}
