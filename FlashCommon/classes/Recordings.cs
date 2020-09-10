using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// All the classes for Firestore data
namespace FlashCommon
{

    [FirestoreData]
    public class User
    {
        // Note: not all properties included here

        [FirestoreProperty]
        public string accountType { get; set; }

        [FirestoreProperty]
        public string displayName { get; set; }

        [FirestoreProperty]
        public long currentTopicId { get; set; }
    }

    [FirestoreData]
    public class Deck
    {
        [FirestoreProperty]
        public int numPairs { get; set; }

        [FirestoreProperty]
        public int order { get; set; }

        [FirestoreProperty]
        public string name { get; set; }

        [FirestoreProperty]
        public long id { get; set; }

        public List<PromptResponsePair> groups { get; set; }

        public void AddPair(PromptResponsePair pair)
        {
            groups.Insert(0, pair);
            numPairs = groups.Count;
            for (var i = 1; i <= groups.Count; i++) groups[i-1].order = i * 1024;
        }
    }

    [FirestoreData]
    public class Target
    {
        [FirestoreProperty]
        public string name { get; set; }

        [FirestoreProperty]
        public string id { get; set; }
    }

    [FirestoreData]
    public class Source
    {
        [FirestoreProperty]
        public string id { get; set; }

        [FirestoreProperty]
        public string name { get; set; }

    }

    [FirestoreData]
    public class Topic
    {
        [FirestoreProperty]
        public bool isLanguage { get; set; }

        [FirestoreProperty]
        public List<Deck> decks { get; set; }

        [FirestoreProperty]
        public string uid { get; set; }

        [FirestoreProperty]
        public long id { get; set; }

        [FirestoreProperty]
        public bool listMode { get; set; }

        [FirestoreProperty]
        public Target target { get; set; }

        [FirestoreProperty]
        public Source source { get; set; }


        public PromptResponsePair CreatePromptResponsePair(Deck deck)
        {
            var pair = new PromptResponsePair()
            {
                deckId = deck.id,
                id = JSTime.Now,
                interval = 0,
                isActive = false,
                order = 1024,
                topicId = id,
                uid = uid,
            };
            pair.nextDate = pair.id;

            pair.prompts = new List<Prompt>(2);
            pair.prompts.Add(new Prompt() { topicNameId = source.id });
            pair.prompts.Add(new Prompt() { topicNameId = target.id });

            deck.AddPair(pair);

            return pair;
        }
    }

    [FirestoreData]
    public class Prompt
    {
        [FirestoreProperty]
        public string text { get; set; }

        [FirestoreProperty]
        public long id { get; set; }

        [FirestoreProperty]
        public string topicNameId { get; set; }

        public static Prompt CreatePrompt()
        {
            var prompt = new Prompt();
            return prompt;
        }
    }

    [FirestoreData]
    public class PromptResponsePair
    {
        [FirestoreProperty]
        public bool isActive { get; set; }

        [FirestoreProperty]
        public int interval { get; set; }

        [FirestoreProperty]
        public List<Prompt> prompts { get; set; }

        [FirestoreProperty]
        public int order { get; set; }

        [FirestoreProperty]
        public long id { get; set; }

        [FirestoreProperty]
        public long nextDate { get; set; }

        [FirestoreProperty]
        public string uid { get; set; }

        [FirestoreProperty]
        public long topicId { get; set; }

        [FirestoreProperty]
        public long deckId { get; set; }
    }


}
