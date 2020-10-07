using System;
using System.Collections.Generic;
using System.Reactive;

namespace FlashCommon
{
    public interface IDatabase
    {
        DBNames Name { get; }

        IObservable<string> CurrentUserId { get; }
        void SetCurrentUserId(string user);

        IObservable<User> GetUserInfo(string uid);

        IObservable<Unit> AddNewUser(User user);

        IObservable<IList<Topic>> GetTopics(string uid);

        IObservable<Deck> GetPairs(string uid, Deck deck);

        IObservable<IList<PromptResponsePair>> GetCurrentPairs(string uid, long currentTopicId);

        IObservable<FirestoreBlob> GetRecording(string uid, Prompt prompt);

        IObservable<Unit> SaveRecording(string uid, Prompt prompt, FirestoreBlob blob);

        IObservable<Unit> SaveTopic(Topic topic);

        IObservable<Unit> SavePromptPair(PromptResponsePair pair);

        // Currently unused
        //IObservable<Unit> SavePromptPairs(List<PromptResponsePair> pairs);

        IObservable<Unit> DeletePair(PromptResponsePair pair);

        IObservable<Unit> DeleteBlob(string uid, Prompt prompt);
    }
}
