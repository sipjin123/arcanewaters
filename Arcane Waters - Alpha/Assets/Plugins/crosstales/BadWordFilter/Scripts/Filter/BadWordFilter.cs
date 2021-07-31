using UnityEngine;
using System.Linq;

namespace Crosstales.BWF.Filter
{
    /// <summary>Filter for bad words. The class can also replace all bad words inside a string.</summary>
    public class BadWordFilter : BaseFilter
    {

        #region Variables

        /// <summary>Replace characters for bad words.</summary>
        public string ReplaceCharacters;

        /// <summary>Replace Leet speak in the input string.</summary>
        public bool ReplaceLeetSpeak;

        /// <summary>Use simple detection algorithm.</summary>
        public bool SimpleCheck;

        private readonly System.Collections.Generic.List<Provider.BadWordProvider> tempBadWordProviderLTR; //left-to-right
        private readonly System.Collections.Generic.List<Provider.BadWordProvider> tempBadWordProviderRTL; //right-to-left

        private readonly System.Collections.Generic.Dictionary<string, System.Text.RegularExpressions.Regex> exactBadwordsRegex = new System.Collections.Generic.Dictionary<string, System.Text.RegularExpressions.Regex>(30);
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Text.RegularExpressions.Regex>> debugExactBadwordsRegex = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Text.RegularExpressions.Regex>>(30);
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> simpleBadwords = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>(30);

        private bool ready = false;
        private bool readyFirstime = false;

        private System.Collections.Generic.List<Provider.BadWordProvider> badWordProviderLTR = new System.Collections.Generic.List<Provider.BadWordProvider>(); //left-to-right
        private System.Collections.Generic.List<Provider.BadWordProvider> badWordProviderRTL = new System.Collections.Generic.List<Provider.BadWordProvider>(); //right-to-left

        #endregion


        #region Properties

        /// <summary>List of all left-to-right providers.</summary>
        /// <returns>All left-to-right providers.</returns>
        public System.Collections.Generic.List<Provider.BadWordProvider> BadWordProviderLTR
        {
            get { return badWordProviderLTR; }
            set
            {
                badWordProviderLTR = value;
                if (badWordProviderLTR != null && badWordProviderLTR.Count > 0)
                {
                    foreach (Provider.BadWordProvider bp in badWordProviderLTR)
                    {
                        if (bp != null)
                        {
                            if (Util.Config.DEBUG_BADWORDS)
                            {
                                debugExactBadwordsRegex.CTAddRange(bp.DebugExactBadwordsRegex);
                            }
                            else
                            {
                                exactBadwordsRegex.CTAddRange(bp.ExactBadwordsRegex);
                            }

                            simpleBadwords.CTAddRange(bp.SimpleBadwords);
                        }
                        else
                        {
                            if (!Util.Helper.isEditorMode)
                                Debug.LogError("A LTR-BadWordProvider is null!");
                        }
                    }
                }
                else
                {
                    badWordProviderLTR = new System.Collections.Generic.List<Provider.BadWordProvider>();

                    if (!Util.Helper.isEditorMode)
                        Debug.LogWarning("No 'BadWordProviderLTR' added!" + System.Environment.NewLine + "If you want to use this functionality, please add your desired 'BadWordProviderLTR' in the editor or script.");
                }
            }
        }

        /// <summary>List of all right-to-left providers.</summary>
        /// <returns>All right-to-left providers.</returns>
        public System.Collections.Generic.List<Provider.BadWordProvider> BadWordProviderRTL
        {
            get { return badWordProviderRTL; }
            set
            {
                badWordProviderRTL = value;
                if (badWordProviderRTL != null && badWordProviderRTL.Count > 0)
                {
                    foreach (Provider.BadWordProvider bp in badWordProviderRTL)
                    {
                        if (bp != null)
                        {
                            if (Util.Config.DEBUG_BADWORDS)
                            {
                                debugExactBadwordsRegex.CTAddRange(bp.DebugExactBadwordsRegex);
                            }
                            else
                            {
                                exactBadwordsRegex.CTAddRange(bp.ExactBadwordsRegex);
                            }

                            simpleBadwords.CTAddRange(bp.SimpleBadwords);
                        }
                        else
                        {
                            if (!Util.Helper.isEditorMode)
                                Debug.LogError("A RTL-BadWordProvider is null!");
                        }
                    }
                }
                else
                {
                    badWordProviderRTL = new System.Collections.Generic.List<Provider.BadWordProvider>();

                    // if (!Util.Helper.isEditorMode)
                    //     Debug.LogWarning("No 'BadWordProviderRTL' added!" + System.Environment.NewLine + "If you want to use this functionality, please add your desired 'BadWordProviderRTL' in the editor or script.");
                }
            }
        }

        /// <summary>Checks the readiness status of the filter.</summary>
        /// <returns>True if the filter is ready.</returns>
        public override bool isReady
        {
            get
            {
                bool result = true;

                if (!ready)
                {
                    if (tempBadWordProviderLTR != null)
                    {
                        foreach (Provider.BadWordProvider bp in tempBadWordProviderLTR)
                        {
                            if (bp != null && !bp.isReady)
                            {
                                result = false;
                                break;
                            }
                        }
                    }

                    if (result)
                    {
                        if (tempBadWordProviderRTL != null)
                        {
                            foreach (Provider.BadWordProvider bp in tempBadWordProviderRTL)
                            {
                                if (bp != null && !bp.isReady)
                                {
                                    result = false;
                                    break;
                                }
                            }
                        }
                    }

                    if (!readyFirstime && result)
                    {
                        BadWordProviderLTR = tempBadWordProviderLTR;
                        BadWordProviderRTL = tempBadWordProviderRTL;

                        if (BadWordProviderLTR != null)
                        {
                            foreach (Provider.BadWordProvider bpLTR in BadWordProviderLTR)
                            {
                                if (bpLTR != null)
                                {
                                    foreach (Data.Source src in bpLTR.Sources)
                                    {
                                        if (src != null)
                                        {
                                            if (!sources.ContainsKey(src.Name))
                                            {
                                                sources.Add(src.Name, src);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (BadWordProviderRTL != null)
                        {
                            foreach (Provider.BadWordProvider bpRTL in BadWordProviderRTL)
                            {
                                if (bpRTL != null)
                                {
                                    foreach (Data.Source src in bpRTL.Sources)
                                    {
                                        if (src != null)
                                        {

                                            if (!sources.ContainsKey(src.Name))
                                            {
                                                sources.Add(src.Name, src);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        readyFirstime = true;
                    }
                }

                ready = result;

                return result;
            }
        }

        #endregion


        #region Constructor

        /// <summary>Instantiate the class.</summary>
        /// <param name="badWordProviderLTR">List of all left-to-right providers.</param>
        /// <param name="badWordProviderRTL">List of all right-to-left providers.</param>
        /// <param name="replaceCharacters">Replace characters for bad words.</param>
        /// <param name="replaceLeetSpeak">Replace Leet speak in the input string.</param>
        /// <param name="simpleCheck">Use simple detection algorithm.</param>
        public BadWordFilter(System.Collections.Generic.List<Provider.BadWordProvider> badWordProviderLTR, System.Collections.Generic.List<Provider.BadWordProvider> badWordProviderRTL, string replaceCharacters, bool leetSpeak, bool simpleCheck /*, string markPrefix, string markPostfix*/)
        {

            tempBadWordProviderLTR = badWordProviderLTR;
            tempBadWordProviderRTL = badWordProviderRTL;

            ReplaceCharacters = replaceCharacters;
            ReplaceLeetSpeak = leetSpeak;
            SimpleCheck = simpleCheck;

            //MarkPrefix = markPrefix;
            //MarkPostfix = markPostfix;
        }

        #endregion


        #region Implemented methods

        public override bool Contains(string text, params string[] sourceNames)
        {
            bool result = false;

            if (isReady)
            {
                if (string.IsNullOrEmpty(text))
                {
                    logContains();
                }
                else
                {

                    string _text = replaceLeetToText(text);
                    System.Text.RegularExpressions.Match match;

                    #region DEBUG

                    if (Util.Config.DEBUG_BADWORDS)
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {

                            if (SimpleCheck)
                            {
                                foreach (System.Collections.Generic.List<string> words in simpleBadwords.Values)
                                {
                                    foreach (string simpleWord in words)
                                    {
                                        if (_text.CTContains(simpleWord))
                                        {
                                            Debug.Log("Test string contains a bad word detected by word '" + simpleWord + "'");
                                            result = true;
                                            break;
                                        }
                                    }

                                    if (result)
                                        break;
                                }
                            }
                            else
                            {
                                foreach (System.Collections.Generic.List<System.Text.RegularExpressions.Regex> badWordRegexes in debugExactBadwordsRegex.Values)
                                {

                                    foreach (System.Text.RegularExpressions.Regex badWordRegex in badWordRegexes)
                                    {
                                        match = badWordRegex.Match(_text);
                                        if (match.Success)
                                        {
                                            Debug.Log("Test string contains a bad word: '" + match.Value + "' detected by regex '" + badWordRegex + "'");
                                            result = true;
                                            break;
                                        }

                                        if (result)
                                            break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Collections.Generic.List<System.Text.RegularExpressions.Regex> badWordRegexes;

                            for (int ii = 0; ii < sourceNames.Length && !result; ii++)
                            {

                                if (SimpleCheck)
                                {
                                    System.Collections.Generic.List<string> words;
                                    if (simpleBadwords.TryGetValue(sourceNames[ii], out words))
                                    {

                                        foreach (string simpleWord in words)
                                        {
                                            if (_text.CTContains(simpleWord))
                                            {
                                                Debug.Log("Test string contains a bad word detected by word '" + simpleWord + "'" + "' from source '" + sourceNames[ii] + "'");
                                                result = true;
                                                break;
                                            }
                                        }

                                        if (result)
                                            break;
                                    }
                                    else
                                    {
                                        logResourceNotFound(sourceNames[ii]);
                                    }
                                }
                                else
                                {

                                    if (debugExactBadwordsRegex.TryGetValue(sourceNames[ii], out badWordRegexes))
                                    {
                                        foreach (System.Text.RegularExpressions.Regex badWordRegex in badWordRegexes)
                                        {

                                            match = badWordRegex.Match(_text);
                                            if (match.Success)
                                            {
                                                Debug.Log("Test string contains a bad word: '" + match.Value + "' detected by regex '" + badWordRegex + "'" + "' from source '" + sourceNames[ii] + "'");
                                                result = true;
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        logResourceNotFound(sourceNames[ii]);
                                    }
                                }
                            }
                        }

                        #endregion

                    }
                    else
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            if (SimpleCheck)
                            {
                                foreach (System.Collections.Generic.List<string> words in simpleBadwords.Values)
                                {
                                    foreach (string simpleWord in words)
                                    {
                                        if (_text.CTContains(simpleWord))
                                        {
                                            result = true;
                                            break;
                                        }
                                    }

                                    if (result)
                                        break;
                                }
                            }
                            else
                            {
                                foreach (System.Text.RegularExpressions.Regex badWordRegex in exactBadwordsRegex.Values)
                                {
                                    if (badWordRegex.Match(_text).Success)
                                    {
                                        result = true;
                                        break;
                                    } else {
                                       // Since the match function does not consider bad words after prefixes, loop through the entire word to check if bad words are added at the middle up to the end
                                       int wordLength = _text.Length - 1;
                                       for (int i = 0; i < wordLength; i++) {
                                          string rePrefixedWord = _text.Remove(0, i);
                                 
                                          if (badWordRegex.Match(rePrefixedWord).Success) {
                                             result = true;
                                             break;
                                          }
                                       }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (string badWordsResource in sourceNames)
                            {
                                if (SimpleCheck)
                                {
                                    System.Collections.Generic.List<string> words;
                                    if (simpleBadwords.TryGetValue(badWordsResource, out words))
                                    {

                                        foreach (string simpleWord in words)
                                        {
                                            if (_text.CTContains(simpleWord))
                                            {
                                                result = true;
                                                break;
                                            }
                                        }

                                        if (result)
                                            break;
                                    }
                                    else
                                    {
                                        logResourceNotFound(badWordsResource);
                                    }
                                }
                                else
                                {
                                    System.Text.RegularExpressions.Regex badWordRegex;

                                    if (exactBadwordsRegex.TryGetValue(badWordsResource, out badWordRegex))
                                    {
                                        match = badWordRegex.Match(_text);
                                        if (match.Success)
                                        {
                                            result = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        logResourceNotFound(badWordsResource);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                logFilterNotReady();
            }

            return result;
        }

        public override System.Collections.Generic.List<string> GetAll(string text, params string[] sourceNames)
        {
            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();

            if (isReady)
            {
                if (string.IsNullOrEmpty(text))
                {
                    logGetAll();
                }
                else
                {

                    string _text = replaceLeetToText(text);

                    #region DEBUG

                    if (Util.Config.DEBUG_BADWORDS)
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {

                            if (SimpleCheck)
                            {
                                foreach (System.Collections.Generic.List<string> words in simpleBadwords.Values)
                                {
                                    foreach (string simpleWord in words)
                                    {
                                        if (_text.CTContains(simpleWord))
                                        {
                                            Debug.Log("Test string contains a bad word detected by word '" + simpleWord + "'");

                                            if (!result.Contains(simpleWord))
                                            {
                                                result.Add(simpleWord);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (System.Collections.Generic.List<System.Text.RegularExpressions.Regex> badWordsResources in debugExactBadwordsRegex.Values)
                                {
                                    foreach (System.Text.RegularExpressions.Regex badWordsResource in badWordsResources)
                                    {
                                        System.Text.RegularExpressions.MatchCollection matches = badWordsResource.Matches(_text);

                                        foreach (System.Text.RegularExpressions.Match match in matches)
                                        {
                                            foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                            {
                                                Debug.Log("Test string contains a bad word: '" + capture.Value + "' detected by regex '" + badWordsResource + "'");

                                                if (!result.Contains(capture.Value))
                                                {
                                                    result.Add(capture.Value);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (string badWordsResource in sourceNames)
                            {
                                if (SimpleCheck)
                                {
                                    System.Collections.Generic.List<string> words;
                                    if (simpleBadwords.TryGetValue(badWordsResource, out words))
                                    {

                                        foreach (string simpleWord in words)
                                        {
                                            if (_text.CTContains(simpleWord))
                                            {
                                                Debug.Log("Test string contains a bad word detected by word '" + simpleWord + "'" + "' from source '" + badWordsResource + "'");

                                                if (!result.Contains(simpleWord))
                                                {
                                                    result.Add(simpleWord);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        logResourceNotFound(badWordsResource);
                                    }
                                }
                                else
                                {
                                    System.Collections.Generic.List<System.Text.RegularExpressions.Regex> badWordRegexes;

                                    if (debugExactBadwordsRegex.TryGetValue(badWordsResource, out badWordRegexes))
                                    {
                                        foreach (System.Text.RegularExpressions.Regex badWordRegex in badWordRegexes)
                                        {
                                            System.Text.RegularExpressions.MatchCollection matches = badWordRegex.Matches(_text);

                                            foreach (System.Text.RegularExpressions.Match match in matches)
                                            {
                                                foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                                {
                                                    Debug.Log("Test string contains a bad word: '" + capture.Value + "' detected by regex '" + badWordRegex + "'" + "' from source '" + badWordsResource + "'");

                                                    if (!result.Contains(capture.Value))
                                                    {
                                                        result.Add(capture.Value);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        logResourceNotFound(badWordsResource);
                                    }
                                }
                            }
                        }

                        #endregion

                    }
                    else
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            if (SimpleCheck)
                            {
                                foreach (System.Collections.Generic.List<string> words in simpleBadwords.Values)
                                {
                                    foreach (string simpleWord in words)
                                    {
                                        if (_text.CTContains(simpleWord))
                                        {
                                            if (!result.Contains(simpleWord))
                                            {
                                                result.Add(simpleWord);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (System.Text.RegularExpressions.Regex badWordsResource in exactBadwordsRegex.Values)
                                {
                                    System.Text.RegularExpressions.MatchCollection matches = badWordsResource.Matches(_text);

                                    foreach (System.Text.RegularExpressions.Match match in matches)
                                    {
                                        foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                        {
                                            if (!result.Contains(capture.Value))
                                            {
                                                result.Add(capture.Value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (string badWordsResource in sourceNames)
                            {
                                if (SimpleCheck)
                                {
                                    System.Collections.Generic.List<string> words;
                                    if (simpleBadwords.TryGetValue(badWordsResource, out words))
                                    {
                                        foreach (string simpleWord in words)
                                        {
                                            if (_text.CTContains(simpleWord))
                                            {
                                                if (!result.Contains(simpleWord))
                                                {
                                                    result.Add(simpleWord);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        logResourceNotFound(badWordsResource);
                                    }
                                }
                                else
                                {
                                    System.Text.RegularExpressions.Regex badWordRegex;
                                    if (exactBadwordsRegex.TryGetValue(badWordsResource, out badWordRegex))
                                    {
                                        System.Text.RegularExpressions.MatchCollection matches = badWordRegex.Matches(_text);

                                        foreach (System.Text.RegularExpressions.Match match in matches)
                                        {
                                            foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                            {
                                                if (!result.Contains(capture.Value))
                                                {

                                                    result.Add(capture.Value);
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        logResourceNotFound(badWordsResource);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                logFilterNotReady();
            }

            return result.Distinct().OrderBy(x => x).ToList();
        }

        public override string ReplaceAll(string text, bool markOnly, string prefix = "", string postfix = "", params string[] sourceNames)
        {
            string result = string.Empty;
            bool hasBadWords = false;

            if (isReady)
            {
                if (string.IsNullOrEmpty(text))
                {
                    logReplaceAll();
                }
                else
                {
                    string _text = result = replaceLeetToText(text);

                    if (SimpleCheck)
                    {
                        foreach (string badword in GetAll(_text, sourceNames))
                        {
                            _text = System.Text.RegularExpressions.Regex.Replace(_text, badword, Util.Helper.CreateString(ReplaceCharacters, badword.Length), System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            hasBadWords = true;
                        }

                        result = _text;
                    }
                    #region DEBUG
                    else if (Util.Config.DEBUG_BADWORDS)
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            foreach (System.Collections.Generic.List<System.Text.RegularExpressions.Regex> badWordsResources in debugExactBadwordsRegex.Values)
                            {
                                foreach (System.Text.RegularExpressions.Regex badWordsResource in badWordsResources)
                                {

                                    System.Text.RegularExpressions.MatchCollection matches = badWordsResource.Matches(_text);

                                    foreach (System.Text.RegularExpressions.Match match in matches)
                                    {
                                        foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                        {
                                            Debug.Log("Test string contains a bad word: '" + capture.Value + "' detected by regex '" + badWordsResource + "'");

                                            result = replaceCapture(result, capture, markOnly, prefix, postfix, result.Length - _text.Length);

                                            //result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateReplaceString(ReplaceCharacters, capture.Value.Length) + postfix);
                                            hasBadWords = true;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Collections.Generic.List<System.Text.RegularExpressions.Regex> badWordRegexes;

                            foreach (string badWordsResource in sourceNames)
                            {
                                if (debugExactBadwordsRegex.TryGetValue(badWordsResource, out badWordRegexes))
                                {
                                    foreach (System.Text.RegularExpressions.Regex badWordRegex in badWordRegexes)
                                    {
                                        System.Text.RegularExpressions.MatchCollection matches = badWordRegex.Matches(_text);

                                        foreach (System.Text.RegularExpressions.Match match in matches)
                                        {
                                            foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                            {
                                                Debug.Log("Test string contains a bad word: '" + capture.Value + "' detected by regex '" + badWordRegex + "'" + "' from source '" + badWordsResource + "'");

                                                result = replaceCapture(result, capture, markOnly, prefix, postfix, result.Length - _text.Length);
                                                //result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateReplaceString(ReplaceCharacters, capture.Value.Length) + postfix);

                                                hasBadWords = true;
                                            }
                                        }

                                    }
                                }
                                else
                                {
                                    logResourceNotFound(badWordsResource);
                                }
                            }
                        }

                        #endregion

                    }
                    else
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            foreach (System.Text.RegularExpressions.Regex badWordsResource in exactBadwordsRegex.Values)
                            {
                                System.Text.RegularExpressions.MatchCollection matches = badWordsResource.Matches(_text);

                                foreach (System.Text.RegularExpressions.Match match in matches)
                                {
                                    foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                    {
                                        //Debug.Log(capture.Value + " - Offset: " + (result.Length - _text.Length));

                                        result = replaceCapture(result, capture, markOnly, prefix, postfix, result.Length - _text.Length);
                                        //result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateReplaceString(ReplaceCharacters, capture.Value.Length) + postfix);

                                        hasBadWords = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Text.RegularExpressions.Regex badWordRegex;

                            foreach (string badWordsResource in sourceNames)
                            {
                                if (exactBadwordsRegex.TryGetValue(badWordsResource, out badWordRegex))
                                {
                                    System.Text.RegularExpressions.MatchCollection matches = badWordRegex.Matches(_text);

                                    foreach (System.Text.RegularExpressions.Match match in matches)
                                    {
                                        foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                        {
                                            //Debug.Log("Offset: " + (result.Length - _text.Length));

                                            result = replaceCapture(result, capture, markOnly, prefix, postfix, result.Length - _text.Length);
                                            //result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateReplaceString(ReplaceCharacters, capture.Value.Length) + postfix);

                                            hasBadWords = true;
                                        }
                                    }
                                }
                                else
                                {
                                    logResourceNotFound(badWordsResource);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                logFilterNotReady();
            }

            if (hasBadWords)
            {
                return result;
            }
            else
            {
                return text;
            }
        }
    
        /*
        public override string Replace(string text, System.Collections.Generic.List<string> badWords, bool markOnly, string prefix = "", string postfix = "")
        {
            string result = replaceLeetToText(text);

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
                        result = result.Replace(badword, markOnly ? prefix + badword + postfix : prefix + Util.Helper.CreateReplaceString(ReplaceCharacters, badword.Length) + postfix);
                    }
                }
            }

            return result;
        }
        */

            /*
        //EXPERIMENTAL!
        public override string Mark(string text, System.Collections.Generic.List<string> badWords, string prefix = "<b><color=red>", string postfix = "</color></b>")
        {
            string result = text;

            string _prefix = prefix;
            string _postfix = postfix;

            if (string.IsNullOrEmpty(text))
            {
                if (Util.Constants.DEV_DEBUG)
                    Debug.LogWarning("Parameter 'text' is null or empty!" + System.Environment.NewLine + "=> 'Mark()' will return an empty string.");

                result = string.Empty;
            }
            else
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'prefix' is null!" + System.Environment.NewLine + "=> Using an empty string as prefix.");

                    _prefix = MarkPrefix ?? string.Empty;
                }

                if (string.IsNullOrEmpty(postfix))
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'postfix' is null!" + System.Environment.NewLine + "=> Using an empty string as postfix.");

                    _postfix = MarkPostfix ?? string.Empty;
                }

                if (badWords == null || badWords.Count == 0)
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'badWords' is null or empty!" + System.Environment.NewLine + "=> 'Mark()' will return the original string.");
                }
                else
                {
                    string _badword;
                    foreach (string badword in badWords)
                    {
                        if (!string.IsNullOrEmpty(badword))
                        {
                            if (result.Contains(badword))
                            {
                                result = result.Replace(badword, _prefix + badword + _postfix);
                            }
                            else
                            {
                                _badword = replaceTextToLeet(badword);

                                //Debug.Log(_badword);

                                //TODO add other LEET possibilites!
                                result = result.Replace(_badword, _prefix + _badword + _postfix);
                            }
                        }
                    }
                }
            }

            return result;
        }
        */

        #endregion


        #region Private methods

        private string replaceCapture(string text, System.Text.RegularExpressions.Capture capture, bool markOnly, string prefix, string postfix, int offset)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(text);

            string replacement = markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateString(ReplaceCharacters, capture.Value.Length) + postfix;

            sb.Remove(capture.Index + offset, capture.Value.Length);
            sb.Insert(capture.Index + offset, replacement);

            return sb.ToString();
        }

        protected string replaceLeetToText(string input)
        {
            string result = input;

            if (ReplaceLeetSpeak && !string.IsNullOrEmpty(input))
            {
                // A
                result = result.Replace("@", "a");
                //result = rgx.Replace (result, "a"); 
                result = result.Replace("4", "a");
                //              result = result.Replace ("/\\", "a");
                //              result = result.Replace ("/-\\", "a");
                result = result.Replace("^", "a");

                // B
                result = result.Replace("8", "b");

                // C
                result = result.Replace("©", "c");
                result = result.Replace('¢', 'c');

                // D

                // E
                result = result.Replace("€", "e");
                result = result.Replace("3", "e");
                result = result.Replace("£", "e");

                // F
                result = result.Replace("ƒ", "f");

                // G
                result = result.Replace("6", "g");
                result = result.Replace("9", "g");

                // H
                result = result.Replace("#", "h");
                //result = result.Replace ("|-|", "h");
                //result = result.Replace ("}{", "h");
                //result = result.Replace ("]-[", "h");
                //result = result.Replace ("/-/", "h");
                //result = result.Replace (")-(", "h");

                // I
                result = result.Replace("1", "i");
                //result = result.Replace("!", "i");
                result = result.Replace("|", "i");
                //result = result.Replace ("][", "i");

                // J

                // K
                //result = result.Replace ("|<", "k");
                //result = result.Replace ("|{", "k");
                //result = result.Replace ("|(", "k");

                // L
                //result = result.Replace ("|_", "l");
                //result = result.Replace ("][_", "l");

                // M
                //result = result.Replace ("/\\/\\", "m");
                //result = result.Replace ("/v\\", "m");
                //result = result.Replace ("|V|", "m");
                //result = result.Replace ("]V[", "m");
                //result = result.Replace ("|\\/|", "m");

                // N
                //result = result.Replace ("|\\|", "n");
                //result = result.Replace ("/\\/", "n");
                //result = result.Replace ("/V", "n");

                // O
                result = result.Replace("0", "o");
                //result = result.Replace ("()", "o"); 

                // P
                //result = result.Replace ("|°", "p");
                //result = result.Replace ("|>", "p");

                // Q

                // R
                result = result.Replace("2", "r");
                result = result.Replace("®", "r");

                // S
                result = result.Replace("$", "s");
                result = result.Replace("5", "s");
                result = result.Replace("§", "s");

                // T
                result = result.Replace("7", "t");
                result = result.Replace("+", "t");
                result = result.Replace("†", "t");
                //              result = result.Replace ("']['", "t");

                // U
                //result = result.Replace ("µ", "u");
                //result = result.Replace ("|_|", "u");

                // V
                //result = result.Replace ("\\/", "v");

                // W
                //result = result.Replace ("\\/\\/", "w");

                // X
                //result = result.Replace ("><", "x");
                //result = result.Replace (")(", "x");

                // Y
                result = result.Replace("¥", "y");

                // Z
            }

            //Debug.LogWarning (result);

            return result;
        }

        protected string replaceTextToLeet(string input, bool obvious = true)
        {
            string result = input;

            if (ReplaceLeetSpeak && !string.IsNullOrEmpty(input))
            {
                if (obvious)
                {
                    // I
                    //result = result.Replace("i", "!");

                    // S
                    result = result.Replace("s", "$");
                }
                else
                {
                    // A
                    result = result.Replace("a", "@");
                    //result = result.Replace("4", "a");
                    //result = result.Replace("^", "a");

                    // B
                    result = result.Replace("b", "8");

                    // C
                    //result = result.Replace("©", "c");
                    //result = result.Replace('¢', 'c');

                    // D

                    // E
                    //result = result.Replace("€", "e");
                    result = result.Replace("e", "3");
                    //result = result.Replace("£", "e");

                    // F
                    //result = result.Replace("ƒ", "f");

                    // G
                    //result = result.Replace("6", "g");
                    result = result.Replace("g", "9");

                    // H
                    //result = result.Replace("#", "h");
                    //result = result.Replace ("|-|", "h");
                    //result = result.Replace ("}{", "h");
                    //result = result.Replace ("]-[", "h");
                    //result = result.Replace ("/-/", "h");
                    //result = result.Replace (")-(", "h");

                    // I
                    result = result.Replace("i", "1");
                    //result = result.Replace("i", "!");
                    //result = result.Replace("|", "i");
                    //result = result.Replace ("][", "i");

                    // J

                    // K
                    //result = result.Replace ("|<", "k");
                    //result = result.Replace ("|{", "k");
                    //result = result.Replace ("|(", "k");

                    // L
                    //result = result.Replace ("|_", "l");
                    //result = result.Replace ("][_", "l");

                    // M
                    //result = result.Replace ("/\\/\\", "m");
                    //result = result.Replace ("/v\\", "m");
                    //result = result.Replace ("|V|", "m");
                    //result = result.Replace ("]V[", "m");
                    //result = result.Replace ("|\\/|", "m");

                    // N
                    //result = result.Replace ("|\\|", "n");
                    //result = result.Replace ("/\\/", "n");
                    //result = result.Replace ("/V", "n");

                    // O
                    result = result.Replace("o", "0");
                    //result = result.Replace ("()", "o"); 

                    // P
                    //result = result.Replace ("|°", "p");
                    //result = result.Replace ("|>", "p");

                    // Q

                    // R
                    result = result.Replace("r", "2");
                    //result = result.Replace("®", "r");

                    // S
                    result = result.Replace("s", "$");
                    //result = result.Replace("s", "5");
                    //result = result.Replace("§", "s");

                    // T
                    result = result.Replace("t", "7");
                    //result = result.Replace("+", "t");
                    //result = result.Replace("†", "t");
                    //result = result.Replace ("']['", "t");

                    // U
                    //result = result.Replace ("µ", "u");
                    //result = result.Replace ("|_|", "u");

                    // V
                    //result = result.Replace ("\\/", "v");

                    // W
                    //result = result.Replace ("\\/\\/", "w");

                    // X
                    //result = result.Replace ("><", "x");
                    //result = result.Replace (")(", "x");

                    // Y
                    //result = result.Replace("¥", "y");

                    // Z
                }
            }

            //Debug.LogWarning (result);

            return result;
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)