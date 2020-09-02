using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FlashCommon
{
    // Manages everything related to playing back the list of
    // currently active prompt-response pairs.
    public class PlaybackMgr
    {
        private string uid;
        private IFirebase fb = new FirebaseService();
        public List<PromptResponsePair> pairs;
        public int currentIndex = 0;
        
        public PlaybackMgr(string uid) {
            this.uid = uid;
            LoadPairs();
        }

        // The currently playing prompt-response pair
        public PromptResponsePair CurrentPair { get { return pairs[currentIndex]; } }

        // The audio data for the current prompt-response pair
        private ReplaySubject<FirestoreBlob[]> _recordings = new ReplaySubject<FirestoreBlob[]>(1);
        public IObservable<FirestoreBlob[]> Recordings { get { return _recordings; } }

        // With the current prompt, update its next playback time and its time interval.
        // Move on to the next prompt.
        public void NextCard(bool success)
        {
            // TO DO: replace this with a C# version
            //var pair = pairs[currentIndex];
            //var nextInfo = await JSRuntime.InvokeAsync<long[]>("flash.nextDate", pair.interval, success);
            //pair.interval = (int)nextInfo[0];
            //pair.nextDate = nextInfo[1];

            currentIndex = (currentIndex + 1) % pairs.Count;
            getRecordings();
        }

        // Read the list of active prompt-response pairs
        private void LoadPairs()
        {
            fb.GetUserInfo(uid).Subscribe(user =>
            {
                fb.GetCurrentPairs(uid, user.currentTopicId).Subscribe(list =>
                {
                    pairs = list;
                    getRecordings();
                });
            });
        }

        // Read the pair of recordings for the current prompt
        private void getRecordings()
        {
            var prompts = pairs[currentIndex].prompts;
            var recs = new FirestoreBlob[2];
            fb.GetRecording(uid, prompts[0]).Subscribe(rec => {
                recs[0] = rec;
                fb.GetRecording(uid, prompts[1]).Subscribe(rec => {
                    recs[1] = rec;
                    _recordings.OnNext(recs);
                });
            });
        }

    }
}
