using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Serialization
{
    /// <summary>
    /// The DateTime formats supported by the JSON serializer/deserializer.
    /// </summary>
    public static class DateTimeFormat
    {
        public const string DATE_ONLY = "yyyy-MM-dd";

        public const string DATE_HHMM = "yyyy-MM-ddTHH:mm";
        public const string DATE_HHMM_UTC = "yyyy-MM-ddTHH:mmZ";
        public const string DATE_HHMM_TIMEZONE = "yyyy-MM-ddTHH:mmzzz";

        public const string DATE_HHMMSS = "yyyy-MM-ddTHH:mm:ss";
        public const string DATE_HHMMSS_UTC = "yyyy-MM-ddTHH:mm:ssZ";
        public const string DATE_HHMMSS_TIMEZONE = "yyyy-MM-ddTHH:mm:sszzz";

        public const string DATE_HHMMSSF = "yyyy-MM-ddTHH:mm:ss.f";
        public const string DATE_HHMMSSF_UTC = "yyyy-MM-ddTHH:mm:ss.fZ";
        public const string DATE_HHMMSSF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fzzz";

        public const string DATE_HHMMSSFF = "yyyy-MM-ddTHH:mm:ss.ff";
        public const string DATE_HHMMSSFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffZ";
        public const string DATE_HHMMSSFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffzzz";

        public const string DATE_HHMMSSFFF = "yyyy-MM-ddTHH:mm:ss.fff";
        public const string DATE_HHMMSSFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffZ";
        public const string DATE_HHMMSSFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffzzz";

        public const string DATE_HHMMSSFFFF = "yyyy-MM-ddTHH:mm:ss.ffff";
        public const string DATE_HHMMSSFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffffZ";
        public const string DATE_HHMMSSFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffffzzz";

        public const string DATE_HHMMSSFFFFF = "yyyy-MM-ddTHH:mm:ss.fffff";
        public const string DATE_HHMMSSFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffffZ";
        public const string DATE_HHMMSSFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffzzz";

        public const string DATE_HHMMSSFFFFFF = "yyyy-MM-ddTHH:mm:ss.ffffff";
        public const string DATE_HHMMSSFFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffffffZ";
        public const string DATE_HHMMSSFFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffffffzzz";

        public const string DATE_HHMMSSFFFFFFF = "yyyy-MM-ddTHH:mm:ss.fffffff";
        public const string DATE_HHMMSSFFFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        public const string DATE_HHMMSSFFFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
    }
}
