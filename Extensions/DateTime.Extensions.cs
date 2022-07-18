using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExifManipulationLibrary.Extensions
{
    public static class DateTime_Extensions
    {
        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.Zero) return dateTime; // Or could throw an ArgumentException
            if (dateTime == DateTime.MinValue || dateTime == DateTime.MaxValue) return dateTime; // do not modify "guard" values

            return dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public static DateTime AsUtc(this DateTime dateTime)
        {
            return DateTime.FromFileTime(dateTime.ToFileTimeUtc());
        }

        public static DateTime AsLocal(this DateTime dateTime)
        {
            return DateTime.FromFileTimeUtc(dateTime.ToFileTime());
        }
    }
}
