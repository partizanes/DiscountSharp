using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DiscountSharp.tools
{
    public static class TimeSpanExtension
    {
        public static bool IsBetween(this TimeSpan time,
                                     TimeSpan startTime, TimeSpan endTime)
        {
            if (endTime == startTime)
                return true;

            if (endTime < startTime){
                return time <= endTime ||
                    time >= startTime;
            }

            return time >= startTime &&
                time <= endTime;
        }
    }
}
