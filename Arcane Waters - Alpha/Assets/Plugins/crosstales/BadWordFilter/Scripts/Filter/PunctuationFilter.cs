using UnityEngine;
using System.Linq;

namespace Crosstales.BWF.Filter
{
    /// <summary>Filter for excessive punctuation. The class can also replace all punctuationa inside a string.</summary>
    public class PunctuationFilter : BaseFilter
    {

        #region Variables

        /// <summary>RegEx to find excessive punctuation.</summary>
        public System.Text.RegularExpressions.Regex RegularExpression { get; private set; }

        private int characterNumber;

        #endregion


        #region Properties

        /// <summary>Defines the number of allowed punctuations in a row.</summary>
        public int CharacterNumber
        {
            get { return characterNumber; }
            set
            {
                if (value < 2)
                {
                    characterNumber = 2;
                }
                else
                {
                    characterNumber = value;
                }

                RegularExpression = new System.Text.RegularExpressions.Regex(@"[?!,.;:-]{" + (characterNumber + 1) + @",}", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            }
        }

        /// <summary>Checks the readiness status of the filter.</summary>
        /// <returns>True if the filter is ready.</returns>
        public override bool isReady
        {
            get
            {
                return true; //is always ready
            }
        }

        #endregion


        #region Constructor

        /// <summary>Instantiate the class.</summary>
        /// <param name="punctuationCharacterNumber">Defines the number of allowed punctuations in a row.</param>
        public PunctuationFilter(int punctuationCharacterNumber /*, string markPrefix, string markPostfix */)
        {
            CharacterNumber = punctuationCharacterNumber;

            //MarkPrefix = markPrefix;
            //MarkPostfix = markPostfix;
        }

        #endregion


        #region Implemented methods

        public override bool Contains(string text, params string[] sources) //sources are ignored
        {
            bool result = false;

            if (System.String.IsNullOrEmpty(text))
            {
                logContains();
            }
            else
            {

                result = RegularExpression.Match(text).Success;
            }

            return result;
        }

        public override System.Collections.Generic.List<string> GetAll(string text, params string[] sources) //sources are ignored
        {
            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();

            if (System.String.IsNullOrEmpty(text))
            {
                logGetAll();
            }
            else
            {
                System.Text.RegularExpressions.MatchCollection matches = RegularExpression.Matches(text);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                    {
                        if (Util.Constants.DEV_DEBUG)
                            Debug.Log("Test string contains an excessive punctuation: '" + capture.Value + "'");

                        if (!result.Contains(capture.Value))
                        {
                            result.Add(capture.Value);
                        }
                    }
                }
            }

            return result.Distinct().OrderBy(x => x).ToList();
        }


        public override string ReplaceAll(string text, bool markOnly = false, string prefix = "", string postfix = "", params string[] sourceNames) //sources are ignored
        {
            string result = text;

            if (System.String.IsNullOrEmpty(text))
            {
                logReplaceAll();

                result = string.Empty;
            }
            else
            {
                System.Text.RegularExpressions.MatchCollection matches = RegularExpression.Matches(text);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                    {
                        if (Util.Constants.DEV_DEBUG)
                            Debug.Log("Test string contains an excessive punctuation: '" + capture.Value + "'");

                        result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + capture.Value.Substring(0, characterNumber) + postfix);
                    }
                }
            }

            return result;
        }


            /*
        public override string Replace(string text, System.Collections.Generic.List<string> badWords, bool markOnly, string prefix = "", string postfix = "")
        {
            string result = text;

            if (string.IsNullOrEmpty(text))
            {
                logReplace();

                result = string.Empty;
            }
            else
            {
                if (badWords == null || badWords.Count == 0)
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'badWords' is null or empty!" + System.Environment.NewLine + "=> 'Replace()' will return the original string.");
                }
                else
                {
                    foreach (string badword in badWords)
                    {
                        result = result.Replace(badword, markOnly ? prefix + badword + postfix : prefix + badword.Substring(0, characterNumber) + postfix);
                    }
                }
            }

            return result;
        }
        */

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)