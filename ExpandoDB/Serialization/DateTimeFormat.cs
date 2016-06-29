using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Serialization
{
    /// <summary>
    /// Defines the DateTime formats supported by the ExpandoDB JSON serializer/deserializer.
    /// </summary>
    public static class DateTimeFormat
    {
        /// <summary>
        /// Date in "yyyy-MM-dd" format
        /// </summary>
        public const string DATE_ONLY = "yyyy-MM-dd";
        /// <summary>
        /// Date and time in "yyyy-MM-ddTHH:mm" format
        /// </summary>
        public const string DATE_HHMM = "yyyy-MM-ddTHH:mm";
        /// <summary>
        /// Date and time in "yyyy-MM-ddTHH:mmZ" format
        /// </summary>
        public const string DATE_HHMM_UTC = "yyyy-MM-ddTHH:mmZ";
        /// <summary>
        /// Date and time in "yyyy-MM-ddTHH:mmzzz" format
        /// </summary>
        public const string DATE_HHMM_TIMEZONE = "yyyy-MM-ddTHH:mmzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss" format.
        /// </summary>
        public const string DATE_HHMMSS = "yyyy-MM-ddTHH:mm:ss";
        /// <summary>
        /// Date amd time in xxx format
        /// </summary>
        public const string DATE_HHMMSS_UTC = "yyyy-MM-ddTHH:mm:ssZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:sszzz" format
        /// </summary>
        public const string DATE_HHMMSS_TIMEZONE = "yyyy-MM-ddTHH:mm:sszzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.f" format
        /// </summary>
        public const string DATE_HHMMSSF = "yyyy-MM-ddTHH:mm:ss.f";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fZ" format
        /// </summary>
        public const string DATE_HHMMSSF_UTC = "yyyy-MM-ddTHH:mm:ss.fZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fzzz" format
        /// </summary>
        public const string DATE_HHMMSSF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ff" format
        /// </summary>
        public const string DATE_HHMMSSFF = "yyyy-MM-ddTHH:mm:ss.ff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffZ" format
        /// </summary>
        public const string DATE_HHMMSSFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fff" format
        /// </summary>
        public const string DATE_HHMMSSFFF = "yyyy-MM-ddTHH:mm:ss.fff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffff" format
        /// </summary>
        public const string DATE_HHMMSSFFFF = "yyyy-MM-ddTHH:mm:ss.ffff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffff" format
        /// </summary>
        public const string DATE_HHMMSSFFFFF = "yyyy-MM-ddTHH:mm:ss.fffff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffff" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFF = "yyyy-MM-ddTHH:mm:ss.ffffff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.ffffffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.ffffffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.ffffffzzz";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffff" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFFF = "yyyy-MM-ddTHH:mm:ss.fffffff";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffffZ" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFFF_UTC = "yyyy-MM-ddTHH:mm:ss.fffffffZ";
        /// <summary>
        /// Date amd time in "yyyy-MM-ddTHH:mm:ss.fffffffzzz" format
        /// </summary>
        public const string DATE_HHMMSSFFFFFFF_TIMEZONE = "yyyy-MM-ddTHH:mm:ss.fffffffzzz";
    }
}
