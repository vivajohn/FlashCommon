using System.ComponentModel;

namespace FlashCommon
{
    public enum DBNames
    {
        [Description("Azure Cosmos")]
        Azure,

        [Description("Google Cloud Firestore")]
        Firebase,

        [Description("Python REST API")]
        Python
    }
}

