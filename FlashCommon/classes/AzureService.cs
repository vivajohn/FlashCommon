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
    public class AzureService : IDatabase
    {
        private Database db;
        public string Name { get { return "Azure"; } }

        public AzureService() 
        {
            var client = new CosmosClientBuilder(@"https://vivajohn2.documents.azure.com:443/", @"M9xOG2vaPNm9jOBmhTz7F0fKi3FXVh0xYeJ32lxnIVbhvWp67Lz5PX9H64cgUrbfwFp9MbSssFzTAgk7geBtiA==")
                                .WithSerializerOptions(new CosmosSerializationOptions
                                {
                                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                                })
                                .WithBulkExecution(true)
                                .Build();

            db = client.GetDatabase("AudioFlash");
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

        // Get the container with the given name
        private Container Container(string name)
        {
            return db.GetContainer(name);
        }

        // Save an object to the database
        private IObservable<Unit> Save<T>(string name, T item)
        {
            return UnitTask(Container(name).UpsertItemAsync(item));
        }

        // Convert a result to Unit.Default (we aren't reading in these cases so
        // we only need to return an empty observable).
        private IObservable<Unit> UnitTask(Task t)
        {
            return t.ToObservable().Select(x => Unit.Default);
        }


        // Run a query that returns only one result
        private IObservable<T> QuerySingle<T>(string containerName, string query)
        {
            return QueryAsync<T>(containerName, query).Select(list => {
                return list.LastOrDefault();
            });
        }

        // Run a query that returns a List
        private IObservable<List<T>> QueryList<T>(string containerName, string query)
        {
            return QueryAsync<T>(containerName, query);
        }

        // Run the passed query
        private AsyncSubject<List<T>> QueryAsync<T>(string name, string query)
        {
            var s = new AsyncSubject<List<T>>();
            var list = new List<T>();
            var iterator = db.GetContainer(name).GetItemQueryIterator<T>(query);
            iterator.ReadNextAsync().ToObservable().Subscribe(item => { 
                foreach (var x in item.Resource)
                {
                    list.Add(x);
                }
                if (!iterator.HasMoreResults)
                {
                    s.OnNext(list);
                    s.OnCompleted();
                }
            });
            return s;
        }
    }
}
