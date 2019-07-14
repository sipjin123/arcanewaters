using UnityEngine;
using System.Linq;

namespace Crosstales.BWF.Filter
{
    /// <summary>Base class for all filters.</summary>
    public abstract class BaseFilter : IFilter
    {

        #region Variables

        /// <summary>Prefix for every found bad word.</summary>
        //public string MarkPrefix = "<color=red>";

        /// <summary>Postfix for every found bad word.</summary>
        //public string MarkPostfix = "</color>";

        protected System.Collections.Generic.Dictionary<string, Data.Source> sources = new System.Collections.Generic.Dictionary<string, Data.Source>();

        #endregion

        /*
                #region Properties

                /// <summary>All sources of the current filter.</summary>
                /// <returns>List with all sources for the current filter</returns>
                public virtual System.Collections.Generic.List<Model.Source> Sources
                {
                    get
                    {
                        System.Collections.Generic.List<Model.Source> result = new System.Collections.Generic.List<Model.Source>();

                        if (isReady)
                        {
                            result = sources.OrderBy(x => x.Key).Select(y => y.Value).ToList();
                        }
                        else
                        {
                            logFilterNotReady();
                        }

                        return result;
                    }
                }

                #endregion
            */

        #region Implemented methods

        public virtual System.Collections.Generic.List<Data.Source> Sources
        {
            get
            {
                System.Collections.Generic.List<Data.Source> result = new System.Collections.Generic.List<Data.Source>();

                if (isReady)
                {
                    result = sources.OrderBy(x => x.Key).Select(y => y.Value).ToList();
                }
                else
                {
                    logFilterNotReady();
                }

                return result;
            }
        }

        public abstract bool isReady
        {
            get;
        }

        public abstract bool Contains(string text, params string[] sourceNames);

        public abstract System.Collections.Generic.List<string> GetAll(string text, params string[] sourceNames);

        //public abstract string ReplaceAll(string text, params string[] sourceNames);

        //public abstract string Replace(string text, System.Collections.Generic.List<string> badWords, bool markOnly = false, string prefix = "", string postfix = "");

        public abstract string ReplaceAll(string text, bool markOnly = false, string prefix = "", string postfix = "", params string[] sourceNames);

        /*
        public virtual string ReplaceAll(string text, bool markOnly = false, string prefix = "", string postfix = "", params string[] sourceNames)
        {
            return Replace(text, GetAll(text, sourceNames), markOnly, prefix, postfix);
        }
        */

        //public abstract string Mark(string text, string prefix = "<b><color=red>", string postfix = "</color></b>");

        /*
        public virtual string Mark(string text, System.Collections.Generic.List<string> badWords, string prefix = "<b><color=red>", string postfix = "</color></b>")
        {
            //Debug.Log("Mark: " + text + " - " + badWords.Count);

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
                    foreach (string badword in badWords)
                    {
                        if (!string.IsNullOrEmpty(badword))
                        {
                            result = result.Replace(badword, _prefix + badword + _postfix);
                        }
                    }
                }
            }

            return result;
        }
        */

        public virtual string Unmark(string text, string prefix = "<b><color=red>", string postfix = "</color></b>")
        {
            string result = text;

            string _prefix = prefix;
            string _postfix = postfix;

            if (string.IsNullOrEmpty(text))
            {
                if (Util.Constants.DEV_DEBUG)
                    Debug.LogWarning("Parameter 'text' is null or empty!" + System.Environment.NewLine + "=> 'Unmark()' will return an empty string.");

                result = string.Empty;
            }
            else
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'prefix' is null!" + System.Environment.NewLine + "=> Using an empty string as prefix.");

                    _prefix = string.Empty;
                }

                if (string.IsNullOrEmpty(postfix))
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'postfix' is null!" + System.Environment.NewLine + "=> Using an empty string as postfix.");

                    _postfix = string.Empty;
                }

                result = result.Replace(_prefix, string.Empty);
                result = result.Replace(_postfix, string.Empty);
            }

            return result;
        }

        public virtual string Mark(string text, bool replace = false, string prefix = "<b><color=red>", string postfix = "</color></b>", params string[] sourceNames)
        {
            //Debug.Log("MarkAll: " + text);

            return ReplaceAll(text, !replace, prefix, postfix, sourceNames);
        }

        #endregion


        #region Protected methods

        protected void logFilterNotReady()
        {
            Debug.LogWarning("Filter is not ready - please wait until 'isReady' returns true.");
        }

        protected void logResourceNotFound(string res)
        {
            if (Util.Constants.DEV_DEBUG)
                Debug.LogWarning("Resource not found: '" + res + "'" + System.Environment.NewLine + "Did you call the method with the correct resource name?");
        }

        protected void logContains()
        {
            if (Util.Constants.DEV_DEBUG)
                Debug.LogWarning("Parameter 'text' is null or empty!" + System.Environment.NewLine + "=> 'Contains()' will return 'false'.");
        }

        protected void logGetAll()
        {
            if (Util.Constants.DEV_DEBUG)
                Debug.LogWarning("Parameter 'text' is null or empty!" + System.Environment.NewLine + "=> 'GetAll()' will return an empty list.");
        }

        protected void logReplaceAll()
        {
            if (Util.Constants.DEV_DEBUG)
                Debug.LogWarning("Parameter 'text' is null or empty!" + System.Environment.NewLine + "=> 'ReplaceAll()' will return an empty string.");
        }

        protected void logReplace()
        {
            if (Util.Constants.DEV_DEBUG)
                Debug.LogWarning("Parameter 'text' is null or empty!" + System.Environment.NewLine + "=> 'Replace()' will return an empty string.");
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)