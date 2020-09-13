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
    public class AzureUtil
    {
        protected Database db;

        protected AzureUtil()
        {
            var client = new CosmosClientBuilder(Cosmos.Endpoint, Cosmos.AuthKey)
                    .WithSerializerOptions(new CosmosSerializationOptions
                    {
                        PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                    })
                    .WithBulkExecution(true)
                    .Build();

            db = client.GetDatabase("AudioFlash");
        }

        // Get the container with the given name
        protected Container Container(string name)
        {
            return db.GetContainer(name);
        }

        // Save an object to the database
        protected IObservable<Unit> Save<T>(string name, T item)
        {
            return UnitTask(Container(name).UpsertItemAsync(item));
        }

        // Convert a result to Unit.Default (we aren't reading in these cases so
        // we only need to return an empty observable).
        protected IObservable<Unit> UnitTask(Task t)
        {
            return t.ToObservable().Select(x => Unit.Default);
        }


        // Run a query that returns only one result
        protected IObservable<T> QuerySingle<T>(string containerName, string query)
        {
            return QueryAsync<T>(containerName, query).Select(list => {
                return list.LastOrDefault();
            });
        }

        // Run a query that returns a List
        protected IObservable<List<T>> QueryList<T>(string containerName, string query)
        {
            return QueryAsync<T>(containerName, query);
        }

        // Run the passed query
        protected AsyncSubject<List<T>> QueryAsync<T>(string name, string query)
        {
            var s = new AsyncSubject<List<T>>();
            var list = new List<T>();
            var iterator = Container(name).GetItemQueryIterator<T>(query);
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
