using System;
using System.Collections.Generic;
using System.Text;

namespace FlashCommon
{
    public class JSTime
    {

        // Returns the current date-time in Javascript milliseconds
        public static long Now 
        { 
            get 
            {
                var jsZeroDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                var js = DateTime.UtcNow.Subtract(jsZeroDate);
                return Convert.ToInt64(js.TotalMilliseconds);
            }
        }
    }
}
