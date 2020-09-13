using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace FlashCommon
{
    // Code for initializing the database
    public class AzureAdmin: AzureUtil
    {

        public IObservable<Unit> Init()
        {
            return CreateUsers().SelectMany(_ => {
                return CreateTopics();
            });
        }

        private IObservable<Unit> CreateUsers()
        {
            var name = "users";
            return DeleteContainer(name).SelectMany(result =>
            {
                Debug.WriteLine("Deleted Users");
                var props = new ContainerProperties(name, "/accountType");
                return Catch(db.CreateContainerAsync(props)).SelectMany(x => {
                    var user = new {
                        accountType = "pro",
                        currentTopicId = 1570041849700,
                        displayName = "Guest",
                        id = "WTatLnFyjJceDeS4QGMLo3PjFjm1"
                    };
                    return Container(name).UpsertItemAsync(user).ToObservable().Select(x => {
                        return Unit.Default;
                    });
                });
            });
        }

        private IObservable<Unit> CreateTopics()
        {
            var name = "topics";
            return DeleteContainer(name).SelectMany(result =>
            {
                Debug.WriteLine("Deleted Topics");
                var props = new ContainerProperties(name, "/target/id");
                return Catch(db.CreateContainerAsync(props)).SelectMany(x => {
                    var topic = new Topic()
                    {
                        decks = new List<Deck>() {
                            new Deck() {
                                groups = null,
                                id = 1599073825980,
                                name = "General",
                                numPairs = 0,
                                order = 1,
                            }
                        },
                        id = 1570041849700,
                        isLanguage = true,
                        listMode = false,
                        source = new Source() { id = "en", name = "en" },
                        target = new Target() { id = "de", name = "de" },
                        uid = "WTatLnFyjJceDeS4QGMLo3PjFjm1",
                    };
                    return Container(name).UpsertItemAsync(topic).ToObservable().Select(_ => {
                        return Unit.Default;
                    });
                });
            });

        }

        private IObservable<Unit> DeleteContainer(string name)
        {
            return Catch(Container(name).DeleteContainerAsync()).Select(x => {
                return Unit.Default;
            });
        }

        protected IObservable<T> Catch<T>(Task<T> t) where T : class
        {
            var x = t.ToObservable().Catch((Exception e) => {
                Debug.WriteLine(e.Message);
                return Observable.Return<T>(null);
            });
            return x;
        }

    }
}
