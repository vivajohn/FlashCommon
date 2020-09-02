using Google.Protobuf;

namespace FlashCommon
{
    // The Cloud Firestore blob is a bit different from the javascript blob.
    // This represents the data in a platform-free way.
    public class FirestoreBlob
    {
        public FirestoreBlob(string blobType, ByteString data) {
            this.blobType = blobType;
            this.data = data;
        }

        public string blobType;
        public ByteString data;
    }
}
