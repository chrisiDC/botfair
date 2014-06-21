using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotFair.BusinessLayer
{
    class TimeZone
    {
        public static DateTime Convert(DateTime time)
        {
          
            //var convertedTime =TimeZoneInfo.ConvertTime(time, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")).Date;

            return DateTime.Now;
        }
    }
}
