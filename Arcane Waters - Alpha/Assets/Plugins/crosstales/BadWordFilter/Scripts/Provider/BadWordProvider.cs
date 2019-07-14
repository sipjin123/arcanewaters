using UnityEngine;

namespace Crosstales.BWF.Provider
{
    /// <summary>Base class for bad word providers.</summary>
    public abstract class BadWordProvider : BaseProvider
    {

        #region Variables

        protected System.Collections.Generic.List<Model.BadWords> badwords = new System.Collections.Generic.List<Model.BadWords>();

        private const string exactRegexStart = @"(?<![\w\d])";
        private const string exactRegexEnd = @"s?(?![\w\d])";

        private System.Collections.Generic.Dictionary<string, System.Text.RegularExpressions.Regex> exactBadwordsRegex = new System.Collections.Generic.Dictionary<string, System.Text.RegularExpressions.Regex>();
        private System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Text.RegularExpressions.Regex>> debugExactBadwordsRegex = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Text.RegularExpressions.Regex>>();
        private System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> simpleBadwords = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();

        #endregion


        #region Properties

        /// <summary>Exact RegEx for bad words.</summary>
        public System.Collections.Generic.Dictionary<string, System.Text.RegularExpressions.Regex> ExactBadwordsRegex
        {
            get { return exactBadwordsRegex; }
            protected set { exactBadwordsRegex = value; }
        }

        /// <summary>Debug-version of "Exact RegEx for bad words".</summary>
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Text.RegularExpressions.Regex>> DebugExactBadwordsRegex
        {
            get { return debugExactBadwordsRegex; }
            protected set { debugExactBadwordsRegex = value; }
        }

        /// <summary>Simplified version of "RegEx for bad words".</summary>
        public System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> SimpleBadwords
        {
            get { return simpleBadwords; }
            protected set { simpleBadwords = value; }
        }

        #endregion


        #region Implemented methods

        public override void Load()
        {
            if (ClearOnLoad)
            {
                badwords.Clear();
            }
        }

        protected override void init()
        {
            ExactBadwordsRegex.Clear();
            DebugExactBadwordsRegex.Clear();
            SimpleBadwords.Clear();

            if (Util.Config.DEBUG_BADWORDS)
                Debug.Log("++ BadWordProvider started in debug-mode ++");

            foreach (Model.BadWords badWord in badwords)
            {
                if (Util.Config.DEBUG_BADWORDS)
                {
                    try
                    {
                        System.Collections.Generic.List<System.Text.RegularExpressions.Regex> exactRegexes = new System.Collections.Generic.List<System.Text.RegularExpressions.Regex>(badWord.BadWordList.Count);

                        foreach (string line in badWord.BadWordList)
                        {
                            exactRegexes.Add(new System.Text.RegularExpressions.Regex(exactRegexStart + line + exactRegexEnd, RegexOption1 | RegexOption2 | RegexOption3 | RegexOption4 | RegexOption5));
                        }

                        if (!DebugExactBadwordsRegex.ContainsKey(badWord.Source.Name))
                        {
                            DebugExactBadwordsRegex.Add(badWord.Source.Name, exactRegexes);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("Could not generate debug regex for source '" + badWord.Source.Name + "': " + ex);

                        if (Util.Constants.DEV_DEBUG)
                            Debug.Log(badWord.BadWordList.CTDump());
                    }
                }
                else
                {
                    try
                    {
                        if (!ExactBadwordsRegex.ContainsKey(badWord.Source.Name))
                        {
                            ExactBadwordsRegex.Add(badWord.Source.Name, new System.Text.RegularExpressions.Regex(exactRegexStart + "(" + string.Join("|", badWord.BadWordList.ToArray()) + ")" + exactRegexEnd, RegexOption1 | RegexOption2 | RegexOption3 | RegexOption4 | RegexOption5));
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("Could not generate exact regex for source '" + badWord.Source.Name + "': " + ex);

                        if (Util.Constants.DEV_DEBUG)
                            Debug.Log(badWord.BadWordList.CTDump());
                    }
                }

                System.Collections.Generic.List<string> simpleWords = new System.Collections.Generic.List<string>(badWord.BadWordList.Count);

                simpleWords.AddRange(badWord.BadWordList);

                if (!SimpleBadwords.ContainsKey(badWord.Source.Name))
                {
                    SimpleBadwords.Add(badWord.Source.Name, simpleWords);
                }

                if (Util.Config.DEBUG_BADWORDS)
                    Debug.Log("Bad word resource '" + badWord.Source.Name + "' loaded and " + badWord.BadWordList.Count + " entries found.");

            }

            isReady = true;
            //raiseOnProviderReady();
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)