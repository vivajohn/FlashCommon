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
        public List<PromptResponsePair> groups { get; set; }

        [FirestoreProperty]
        public int numPairs { get; set; }

        [FirestoreProperty]
        public int order { get; set; }

        [FirestoreProperty]
        public string name { get; set; }

        [FirestoreProperty]
        public long id { get; set; }
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
