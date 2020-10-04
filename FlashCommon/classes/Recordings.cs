using Google.Cloud.Firestore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        [JsonProperty]
        [FirestoreProperty]
        public string uid { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public string accountType { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public string displayName { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public string email { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public long currentTopicId { get; set; }
    }

    [FirestoreData]
    public class Deck
    {
        [JsonProperty]
        [FirestoreProperty]
        public int numPairs { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public int order { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public string name { get; set; }

        [JsonConverter(typeof(ToStringConverter))]
        [FirestoreProperty]
        public long id { get; set; }

        public List<PromptResponsePair> groups { get; set; }

        public void AddPair(PromptResponsePair pair)
        {
            groups.Insert(0, pair);
            numPairs = groups.Count;
            for (var i = 1; i <= groups.Count; i++) groups[i-1].order = i * 1024;
        }

        // method for cloning object 
        public Deck ShallowCopy()
        {
            return MemberwiseClone() as Deck;
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
        [JsonProperty]
        [FirestoreProperty]
        public string id { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public string name { get; set; }

    }

    [FirestoreData]
    public class Topic
    {
        [JsonProperty]
        [FirestoreProperty]
        public bool isLanguage { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public List<Deck> decks { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public string uid { get; set; }

        [JsonConverter(typeof(ToStringConverter))]
        [FirestoreProperty]
        public long id { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public bool listMode { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public Target target { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public Source source { get; set; }


        public Deck AddDeck()
        {
            var deck = new Deck()
            {
                groups = new List<PromptResponsePair>(),
                id = JSTime.Now,
                name = String.Empty,
                numPairs = 0,
                order = decks.Count + 1
            };
            CreatePromptResponsePair(deck);
            decks.Add(deck);
            return deck;
        }

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

        // Copies the Topic but sets the deck.groups to null
        // (Workaround for problem saving Topic in Azure.)
        public object NoGroupsCopy()
        {
            Topic copy = MemberwiseClone() as Topic;
            copy.decks = new List<Deck>();
            foreach(var deck in decks)
            {
                var d = deck.ShallowCopy();
                d.groups = null;
                copy.decks.Add(d);
            }
            return copy;
        }
    }

    [FirestoreData]
    public class Prompt
    {
        [JsonProperty]
        [FirestoreProperty]
        public string text { get; set; }

        [JsonConverter(typeof(ToStringConverter))]
        [FirestoreProperty]
        public long id { get; set; }

        [JsonProperty]
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
        [JsonProperty]
        [FirestoreProperty]
        public bool isActive { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public int interval { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public List<Prompt> prompts { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public int order { get; set; }

        [JsonConverter(typeof(ToStringConverter))]
        [FirestoreProperty]
        public long id { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public long nextDate { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public string uid { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public long topicId { get; set; }

        [JsonProperty]
        [FirestoreProperty]
        public long deckId { get; set; }
    }


    // In Azure Cosmos DB, the id field is mandatory and it must be a string.
    // This converts the existing long id to a string.
    public class ToStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JToken t = JToken.FromObject(value.ToString());
            t.WriteTo(writer);

        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override bool CanRead
        {
            get { 
                return false; 
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}
