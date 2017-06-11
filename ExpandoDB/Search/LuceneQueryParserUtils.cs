using Common.Logging;
using ExpandoDB.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Implements utility methods required by the <see cref="LuceneQueryParser"/> class.
    /// </summary>
    public static class LuceneQueryParserUtils
    {
        internal const string QUERY_PARSER_ILLEGAL_CHARS = @"[\+&|!\(\)\{\}\[\]^""~\*\?:\\/ ]";
        private static readonly System.Text.RegularExpressions.Regex _queryParserIllegalCharsRegex = new System.Text.RegularExpressions.Regex(QUERY_PARSER_ILLEGAL_CHARS);
        private static readonly ILog _log = LogManager.GetLogger(nameof(LuceneQueryParserUtils));

        /// <summary>
        /// Determines whether the given field name contains illegal characters.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>
        ///   <c>true</c> if the given field name contains illegal chars; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsIllegalChars(this string fieldName)
        {
            return _queryParserIllegalCharsRegex.IsMatch(fieldName);
        }

        /// <summary>
        /// Converts the given string value to <see cref="Double"/>
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="LuceneQueryParserException"></exception>
        public static double ToDouble(this string value)
        {
            double doubleValue;
            if (!Double.TryParse(value, out doubleValue))
                throw new LuceneQueryParserException($"Invalid number in query: '{value}'");

            return doubleValue;
        }

        /// <summary>
        /// Converts the given string value to <see cref="DateTime"/> ticks, which is an <see cref="long"/> value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="LuceneQueryParserException"></exception>
        public static long ToDateTimeLongTicks(this string value)
        {
            var dateTimeValue = DateTime.MinValue;
            if (!DynamicJsonSerializer.TryParseDateTime(value, ref dateTimeValue))
                throw new LuceneQueryParserException($"Invalid DateTime in query: '{value}'");

            var utcDateTime = dateTimeValue.ToUniversalTime();

            return utcDateTime.Ticks;
        }

        /// <summary>
        /// Converts the given string value to <see cref="int"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="LuceneQueryParserException"></exception>
        public static int ToInteger(this string value)
        {
            bool boolValue;
            if (!Boolean.TryParse(value, out boolValue))
                throw new LuceneQueryParserException($"Invalid boolean in query: '{value}'");

            var numericBool = boolValue ? 1 : 0;
            return numericBool;
        }

        /// <summary>
        /// Converts the given string value to a <see cref="Guid"/> string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        /// <exception cref="LuceneQueryParserException"></exception>
        public static string ToGuidString(this string value)
        {
            Guid guid;
            if (!Guid.TryParse(value, out guid))
                throw new LuceneQueryParserException($"Invalid GUID in query: '{value}'");

            return guid.ToString().ToLower();
        }

    }
}
