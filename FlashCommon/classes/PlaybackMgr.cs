using CustomExtensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace FlashCommon
{
    // Manages everything related to playing back the list of
    // currently active prompt-response pairs.
    public class PlaybackMgr : IDisposable
    {
        public ReplaySubject<bool> IsLoading { get; } = new ReplaySubject<bool>(1);

        // The audio data for the current prompt-response pair
        private ReplaySubject<FirestoreBlob[]> _recordings = new ReplaySubject<FirestoreBlob[]>(1);
        public IObservable<FirestoreBlob[]> Recordings { get { return _recordings; } }
        
        private string uid;
        private IDatabase db;
        private SortedList<long, PromptResponsePair> pairs;
        private IDisposable subscription;

        public PlaybackMgr(IDatabase db) {
            this.db = db;
            db.CurrentUserId.Once().Subscribe(uid =>
            {
                this.uid = uid;
                LoadPairs();
            });
        }

        public void Dispose()
        {
            _recordings.Dispose();
            IsLoading.Dispose();
            DisposeTimer();
        }

        private void DisposeTimer()
        {
            if (subscription != null)
            {
                subscription.Dispose();
                subscription = null;
            }
        }

        // With the current prompt, update its next playback time and its time interval.
        // Move on to the next prompt.
        public DateTime? NextCard(bool success)
        {
            if (pairs.Count > 0)
            {
                // Calculate the next playback time
                var pair = pairs.Values[0];
                pairs.RemoveAt(0);
                var nextInfo = (new Intervals()).next(pair.interval, success);
                pair.interval = nextInfo.index;
                pair.nextDate = nextInfo.date;
                pairs.Add(pair.nextDate, pair);

                pair = pairs.Values[0]; // new current pair
                var now = JSTime.Now;
                if (pair.nextDate > now)
                {
                    // Playback time is in the future, wait...
                    int ms = (int)(pair.nextDate - now);
                    subscription = Task.Delay(ms).ToObservable().Subscribe(_ => {
                        DisposeTimer();
                        getRecordings();
                    });
                    return DateTime.Now.AddMilliseconds(ms);
                }

                getRecordings();
            }
            return null;
        }

        // Read the list of active prompt-response pairs
        private void LoadPairs()
        {
            db.GetUserInfo(uid).Where(x => x != null).Once().Subscribe(user =>
            {
                db.GetCurrentPairs(uid, user.currentTopicId).Once().Subscribe(list =>
                {
                    pairs = new SortedList<long, PromptResponsePair>(list.Count);
                    foreach (var p in list) pairs.Add(p.nextDate, p);
                    if (pairs.Count > 0)
                    {
                        getRecordings();
                    }
                    else
                    {
                        IsLoading.OnNext(false);
                        _recordings.OnNext(null);
                    }
                });
            });
        }

        // Read the pair of recordings for the current prompt
        private void getRecordings()
        {
            // Keeps the screen from flashing when the result is returned quickly
            var isFast = false;
            Task.Delay(750).ToObservable().Where(_ => !isFast).Subscribe(_ => {
                IsLoading.OnNext(true);
            });

            var prompts = pairs.Values[0].prompts;
            var recs = new FirestoreBlob[2];
            var o = new IObservable<FirestoreBlob>[2];
            o[0] = db.GetRecording(uid, prompts[0]).Do(rec0 => recs[0] = rec0);
            o[1] = db.GetRecording(uid, prompts[1]).Do(rec1 =>recs[1] = rec1);
            Observable.Merge(o).LastAsync().Once().Subscribe(r => {
                isFast = true;
                IsLoading.OnNext(false);
                _recordings.OnNext(recs);
            });
        }

    }
}
