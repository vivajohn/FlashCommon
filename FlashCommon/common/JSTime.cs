using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FlashCommon
{
    public class JSTime
    {
        // This takes into account the difference between the server's DateTime.Now and the browser's local time
        public static long ServerOffset = 0;

        // Returns the current date-time in Javascript milliseconds
        public static long Now 
        { 
            get 
            {
                var js = DateTime.UtcNow.Subtract(ZeroDate);
                return Convert.ToInt64(js.TotalMilliseconds);
            }
        }

        public static DateTime ZeroDate
        {
            get
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            }
        }
    }
}
