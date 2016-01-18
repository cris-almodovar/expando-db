using FlexLucene.Analysis;
using FlexLucene.Analysis.Core;
using FlexLucene.Analysis.En;
using FlexLucene.Analysis.Pattern;
using FlexLucene.Analysis.Tokenattributes;
using java.util.regex;
using System;
using System.Collections.Generic;

namespace ExpandoDB.Search
{
    /// <summary>
    /// Lucene analyzer that breaks up Text into individual tokens using a list of separator chars, and then stems the tokens using
    /// the Porter stemming algorithm.
    /// </summary>
    public class FullTextAnalyzer : Analyzer
    {
        public const string DEFAULT_SEPARATOR_CHARS = @"[\s,:;.()?!@#%^&*|/\\+÷°±{}\[\]<>\-`~'""$£€¢¥©®™•§†‡–—¶]";
        private readonly string _separatorChars;
        private readonly bool _enableStemming;
        private readonly bool _ignoreCase;

        /// <summary>
        /// Initializes a new instance of the <see cref="FullTextAnalyzer" /> class.
        /// </summary>
        /// <param name="enableStemming">if set to <c>true</c>, the FullTextIndex will stem 
        /// the tokens that make up the texts, using the Porter stemming algorithm.</param>
        /// <param name="ignoreCase">if set to <c>true</c>, character casing is ignored.</param>
        /// <param name="separatorChars">A string whose component characters will be used to split the texts into tokens.</param>   
        public FullTextAnalyzer(bool enableStemming = true, bool ignoreCase = true, string separatorChars = DEFAULT_SEPARATOR_CHARS)
        {
            if (String.IsNullOrWhiteSpace(separatorChars))
                separatorChars = DEFAULT_SEPARATOR_CHARS;

            _enableStemming = enableStemming;
            _ignoreCase = ignoreCase;
            _separatorChars = separatorChars;
        }        

        /// <summary>
        /// Creates the components.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns></returns>
        protected override AnalyzerTokenStreamComponents CreateComponents(string fieldName)
        {
            if (String.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("fieldName cannot be null or blank");

            var pattern = Pattern.compile(_separatorChars);
            var tokenizer = new PatternTokenizer(pattern, -1);
            var stream = _ignoreCase ? new LowerCaseFilter(tokenizer) as TokenStream : tokenizer as TokenStream;

            if (_enableStemming)
                stream = new PorterStemFilter(stream);

            return new AnalyzerTokenStreamComponents(tokenizer, stream);
        }

        /// <summary>
        /// Breaks up the text into tokens.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="enableStemming">if set to <c>true</c>, the FullTextIndex will stem 
        /// the tokens that make up the texts, using the Porter stemming algorithm.</param>
        /// <param name="ignoreCase">if set to <c>true</c>, character casing is ignored.</param>
        /// <param name="separatorChars">A string whose component characters will be used to split the texts into tokens.</param> 
        /// <returns></returns>
        public static IEnumerable<string> Tokenize(string text, bool enableStemming = true, bool ignoreCase = true, string separatorChars = DEFAULT_SEPARATOR_CHARS)
        {
            if (String.IsNullOrWhiteSpace(text))
                throw new ArgumentException("text cannot be null or blank");
            if (String.IsNullOrWhiteSpace(separatorChars))
                separatorChars = DEFAULT_SEPARATOR_CHARS;

            using (var analyzer = new FullTextAnalyzer(enableStemming, ignoreCase, separatorChars))
            {
                using (var stream = analyzer.TokenStream("text", text))
                {
                    var attrib = stream.AddAttribute(typeof(CharTermAttribute)) as CharTermAttribute;
                    stream.Reset();
                    while (stream.IncrementToken())
                    {
                        yield return attrib.ToString();
                    }
                    stream.End();
                }
            }
        }
    }
}
