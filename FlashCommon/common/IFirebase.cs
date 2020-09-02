using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlashCommon
{
    public interface IFirebase
    {
        IObservable<User> GetUserInfo(string uid);

        IObservable<Topic[]> GetTopics(string uid);

        IObservable<Deck> GetPairs(string uid, Deck deck);

        IObservable<List<PromptResponsePair>> GetCurrentPairs(string uid, long currentTopicId);

        IObservable<FirestoreBlob> GetRecording(string uid, Prompt prompt);
    }
}
