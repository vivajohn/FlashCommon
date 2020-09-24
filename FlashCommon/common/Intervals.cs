using System;
using System.Collections.Generic;
using System.Text;

namespace FlashCommon
{
    // Calculates the next playback time
    // (Adapted from the original typescript version.)
    public class Intervals
    {
        private long[] intervals;
        private int maxIndex;
        private int oneDayInterval;

        public Intervals()
        {
            var oneMinute = 60 * 1000;
            var oneHour = 60 * oneMinute;
            var oneDay = 24 * oneHour;

            intervals = new long[] { 
              // Pimsleur's intervals:
              // 5 seconds, 25 seconds, 2 minutes, 10 minutes, 1 hour, 5 hours, 1 day, 5 days, 25 days, 4 months, 2 years
              // The first two are not realistic in this context, so they are modified. The last two don't give much
              // practice, so they are changed. '2 days' also added to give more practice.
              30 * 1000,
              oneMinute,
              2 * oneMinute,
              10 * oneMinute,
              oneHour,
              5 * oneHour,
              oneDay,
              2 * oneDay,
              5 * oneDay,
              25 * oneDay,
              60 * oneDay,
              120 * oneDay,
              180 * oneDay
            };

            maxIndex = intervals.Length - 1;
            oneDayInterval = Array.IndexOf(intervals, oneDay);
        }

        // Get the next intervals index and date depending on whether the user
        // successfully answered the prompt or not.
        public (int index, long date) next(int currentIndex, bool success)
        {
            return success ? setSuccess(currentIndex) : setFailure(currentIndex);
        }


        private (int index, long date) setSuccess(int currentIndex)
        {
            if (currentIndex < maxIndex) currentIndex++;
            return (currentIndex, JSTime.Now + intervals[currentIndex]);
        }

        private (int index, long date) setFailure(int currentIndex)
        {
            if (currentIndex > 0)
            {
                if (currentIndex > oneDayInterval)
                {
                    // If the next try is far in the future, go back to one day
                    currentIndex = oneDayInterval;
                }
                else
                {
                    currentIndex--;
                }
            }
            return (currentIndex, JSTime.Now + intervals[currentIndex]);
        }
    }
}
