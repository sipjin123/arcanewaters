using UnityEngine;
using System.Linq;

namespace Crosstales.BWF.Filter
{
    /// <summary>Filter for domains. The class can also replace all domains inside a string.</summary>
    public class DomainFilter : BaseFilter
    {

        #region Variables

        /// <summary>Replace characters for domains.</summary>
        public string ReplaceCharacters;

        private System.Collections.Generic.List<Provider.DomainProvider> domainProvider = new System.Collections.Generic.List<Provider.DomainProvider>();

        private readonly System.Collections.Generic.List<Provider.DomainProvider> tempDomainProvider;
        private readonly System.Collections.Generic.Dictionary<string, System.Text.RegularExpressions.Regex> domainsRegex = new System.Collections.Generic.Dictionary<string, System.Text.RegularExpressions.Regex>();
        private readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Text.RegularExpressions.Regex>> debugDomainsRegex = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<System.Text.RegularExpressions.Regex>>();
        private bool ready = false;
        private bool readyFirstime = false;

        #endregion


        #region Properties

        /// <summary>List of all domain providers.</summary>
        /// <returns>All domain providers.</returns>
        public System.Collections.Generic.List<Provider.DomainProvider> DomainProvider
        {
            get { return domainProvider; }
            set
            {
                domainProvider = value;
                if (domainProvider != null && domainProvider.Count > 0)
                {
                    foreach (Provider.DomainProvider dp in domainProvider)
                    {
                        if (dp != null)
                        {
                            if (Util.Config.DEBUG_DOMAINS)
                            {
                                debugDomainsRegex.CTAddRange(dp.DebugDomainsRegex);
                            }
                            else
                            {
                                domainsRegex.CTAddRange(dp.DomainsRegex);
                            }
                        }
                        else
                        {
                            if (!Util.Helper.isEditorMode)
                                Debug.LogError("DomainProvider is null!");
                        }
                    }
                }
                else
                {
                    domainProvider = new System.Collections.Generic.List<Provider.DomainProvider>();

                    if (!Util.Helper.isEditorMode)
                        Debug.LogWarning("No 'DomainProvider' added!" + System.Environment.NewLine + "If you want to use this functionality, please add your desired 'DomainProvider' in the editor or script.");
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
                    if (tempDomainProvider != null)
                    {
                        foreach (Provider.DomainProvider dp in tempDomainProvider)
                        {
                            if (dp != null && !dp.isReady)
                            {
                                result = false;
                                break;
                            }
                        }
                    }

                    if (!readyFirstime && result)
                    {
                        DomainProvider = tempDomainProvider;

                        if (DomainProvider != null)
                        {
                            foreach (Provider.DomainProvider dp in DomainProvider)
                            {
                                if (dp != null)
                                {
                                    foreach (Data.Source src in dp.Sources)
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
        /// <param name="domainProvider">List of all domain providers.</param>
        /// <param name="replaceCharacters">Replace characters for domains.</param>
        public DomainFilter(System.Collections.Generic.List<Provider.DomainProvider> domainProvider, string replaceCharacters /*, string markPrefix, string markPostfix*/)
        {
            tempDomainProvider = domainProvider;
            ReplaceCharacters = replaceCharacters;

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

                    #region DEBUG

                    if (Util.Config.DEBUG_DOMAINS)
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            foreach (System.Collections.Generic.List<System.Text.RegularExpressions.Regex> domainRegexes in debugDomainsRegex.Values)
                            {
                                foreach (System.Text.RegularExpressions.Regex domainRegex in domainRegexes)
                                {
                                    System.Text.RegularExpressions.Match match = domainRegex.Match(text);
                                    if (match.Success)
                                    {
                                        Debug.Log("Test string contains a domain: '" + match.Value + "' detected by regex '" + domainRegex + "'");
                                        result = true;
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Collections.Generic.List<System.Text.RegularExpressions.Regex> domainRegexes;

                            foreach (string domainResource in sourceNames)
                            {
                                if (debugDomainsRegex.TryGetValue(domainResource, out domainRegexes))
                                {
                                    foreach (System.Text.RegularExpressions.Regex domainRegex in domainRegexes)
                                    {
                                        System.Text.RegularExpressions.Match match = domainRegex.Match(text);
                                        if (match.Success)
                                        {
                                            Debug.Log("Test string contains a domain: '" + match.Value + "' detected by regex '" + domainRegex + "'" + "' from source '" + domainResource + "'");
                                            result = true;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    logResourceNotFound(domainResource);
                                }
                            }
                        }

                        #endregion

                    }
                    else
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            foreach (System.Text.RegularExpressions.Regex domainRegex in domainsRegex.Values)
                            {
                                if (domainRegex.Match(text).Success)
                                {
                                    result = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            System.Text.RegularExpressions.Regex domainRegex;

                            foreach (string domainResource in sourceNames)
                            {
                                if (domainsRegex.TryGetValue(domainResource, out domainRegex))
                                {
                                    System.Text.RegularExpressions.Match match = domainRegex.Match(text);
                                    if (match.Success)
                                    {
                                        result = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    logResourceNotFound(domainResource);
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

                    #region DEBUG

                    if (Util.Config.DEBUG_DOMAINS)
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            foreach (System.Collections.Generic.List<System.Text.RegularExpressions.Regex> domainRegexes in debugDomainsRegex.Values)
                            {
                                foreach (System.Text.RegularExpressions.Regex domainRegex in domainRegexes)
                                {
                                    System.Text.RegularExpressions.MatchCollection matches = domainRegex.Matches(text);

                                    foreach (System.Text.RegularExpressions.Match match in matches)
                                    {
                                        foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                        {
                                            Debug.Log("Test string contains a domain: '" + capture.Value + "' detected by regex '" + domainRegex + "'");

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
                            System.Collections.Generic.List<System.Text.RegularExpressions.Regex> domainRegexes;

                            foreach (string domainResource in sourceNames)
                            {
                                if (debugDomainsRegex.TryGetValue(domainResource, out domainRegexes))
                                {
                                    foreach (System.Text.RegularExpressions.Regex domainRegex in domainRegexes)
                                    {
                                        System.Text.RegularExpressions.MatchCollection matches = domainRegex.Matches(text);

                                        foreach (System.Text.RegularExpressions.Match match in matches)
                                        {
                                            foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                            {
                                                Debug.Log("Test string contains a domain: '" + capture.Value + "' detected by regex '" + domainRegex + "'" + "' from source '" + domainResource + "'");

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
                                    logResourceNotFound(domainResource);
                                }
                            }
                        }

                        #endregion

                    }
                    else
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            foreach (System.Text.RegularExpressions.Regex domainRegex in domainsRegex.Values)
                            {
                                System.Text.RegularExpressions.MatchCollection matches = domainRegex.Matches(text);

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
                        else
                        {
                            System.Text.RegularExpressions.Regex domainRegex;

                            foreach (string domainResource in sourceNames)
                            {
                                if (domainsRegex.TryGetValue(domainResource, out domainRegex))
                                {
                                    System.Text.RegularExpressions.MatchCollection matches = domainRegex.Matches(text);

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
                                    logResourceNotFound(domainResource);
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
            string result = text;

            if (isReady)
            {
                if (string.IsNullOrEmpty(text))
                {
                    logReplaceAll();

                    result = string.Empty;
                }
                else
                {

                    #region DEBUG

                    if (Util.Config.DEBUG_DOMAINS)
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            foreach (System.Collections.Generic.List<System.Text.RegularExpressions.Regex> domainRegexes in debugDomainsRegex.Values)
                            {
                                foreach (System.Text.RegularExpressions.Regex domainRegex in domainRegexes)
                                {
                                    System.Text.RegularExpressions.MatchCollection matches = domainRegex.Matches(text);

                                    foreach (System.Text.RegularExpressions.Match match in matches)
                                    {
                                        foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                        {
                                            Debug.Log("Test string contains a domain: '" + capture.Value + "' detected by regex '" + domainRegex + "'");

                                            result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateString(ReplaceCharacters, capture.Value.Length) + postfix);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Collections.Generic.List<System.Text.RegularExpressions.Regex> domainRegexes;

                            foreach (string domainResource in sourceNames)
                            {
                                if (debugDomainsRegex.TryGetValue(domainResource, out domainRegexes))
                                {
                                    foreach (System.Text.RegularExpressions.Regex domainRegex in domainRegexes)
                                    {
                                        System.Text.RegularExpressions.MatchCollection matches = domainRegex.Matches(text);

                                        foreach (System.Text.RegularExpressions.Match match in matches)
                                        {
                                            foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                            {
                                                Debug.Log("Test string contains a domain: '" + capture.Value + "' detected by regex '" + domainRegex + "'" + "' from source '" + domainResource + "'");

                                                result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateString(ReplaceCharacters, capture.Value.Length) + postfix);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    logResourceNotFound(domainResource);
                                }
                            }
                        }

                        #endregion

                    }
                    else
                    {
                        if (sourceNames == null || sourceNames.Length == 0)
                        {
                            foreach (System.Text.RegularExpressions.Regex domainRegex in domainsRegex.Values)
                            {
                                System.Text.RegularExpressions.MatchCollection matches = domainRegex.Matches(text);

                                foreach (System.Text.RegularExpressions.Match match in matches)
                                {
                                    foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                    {
                                        result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateString(ReplaceCharacters, capture.Value.Length) + postfix);
                                    }
                                }
                            }
                        }
                        else
                        {
                            System.Text.RegularExpressions.Regex domainRegex;

                            foreach (string domainResource in sourceNames)
                            {
                                if (domainsRegex.TryGetValue(domainResource, out domainRegex))
                                {
                                    System.Text.RegularExpressions.MatchCollection matches = domainRegex.Matches(text);

                                    foreach (System.Text.RegularExpressions.Match match in matches)
                                    {
                                        foreach (System.Text.RegularExpressions.Capture capture in match.Captures)
                                        {
                                            result = result.Replace(capture.Value, markOnly ? prefix + capture.Value + postfix : prefix + Util.Helper.CreateString(ReplaceCharacters, capture.Value.Length) + postfix);
                                        }
                                    }
                                }
                                else
                                {
                                    logResourceNotFound(domainResource);
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

        /*
        public override string Replace(string text, System.Collections.Generic.List<string> domains, bool markOnly, string prefix = "", string postfix = "")
        {
            string result = text;

            if (string.IsNullOrEmpty(text))
            {
                logReplace();

                result = string.Empty;
            }
            else
            {
                if (domains == null || domains.Count == 0)
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'badWords' is null or empty!" + System.Environment.NewLine + "=> 'Replace()' will return the original string.");
                }
                else
                {
                    foreach (string badword in domains)
                    {
                        result = result.Replace(badword, markOnly ? prefix + badword + postfix : prefix + Util.Helper.CreateReplaceString(ReplaceCharacters, badword.Length) + postfix);
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