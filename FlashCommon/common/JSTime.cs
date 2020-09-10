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
                var now = DateTime.Now.Ticks;
                var js = new DateTime(1970, 1, 1);
                return (now - js.Ticks) / 10000;
            }
        }
    }
}
