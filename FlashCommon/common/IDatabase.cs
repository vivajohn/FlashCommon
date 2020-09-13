using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace FlashCommon
{
    public interface IDatabase
    {
        string Name { get; }

        IObservable<User> GetUserInfo(string uid);

        IObservable<IList<Topic>> GetTopics(string uid);

        IObservable<Deck> GetPairs(string uid, Deck deck);

        IObservable<IList<PromptResponsePair>> GetCurrentPairs(string uid, long currentTopicId);

        IObservable<FirestoreBlob> GetRecording(string uid, Prompt prompt);

        IObservable<Unit> SaveTopic(Topic topic);

        IObservable<Unit> SavePromptPair(PromptResponsePair pair);

        IObservable<Unit> SavePromptPairs(List<PromptResponsePair> pairs);

        IObservable<Unit> DeletePair(PromptResponsePair pair);
    }
}
