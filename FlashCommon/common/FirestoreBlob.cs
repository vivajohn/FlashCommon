﻿using Google.Cloud.Firestore;
using Google.Protobuf;

namespace FlashCommon
{
    // The Cloud Firestore blob is a bit different from the javascript blob.
    // This represents the data in a platform-free way.
    public class FirestoreBlob
    {
        public FirestoreBlob(string blobType, string data64) {
            this.blobType = blobType;
            this.data64 = data64;
        }

        public string blobType;
        public string data64;
    }

    [FirestoreData]
    public class BlobDto
    {
        [FirestoreProperty]
        public string type { get; set; }

        [FirestoreProperty]
        public Blob blob { get; set; }
    }

}
