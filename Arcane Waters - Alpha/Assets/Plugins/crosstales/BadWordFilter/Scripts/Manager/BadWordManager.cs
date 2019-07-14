using UnityEngine;

namespace Crosstales.BWF.Manager
{
    /// <summary>Manager for for bad words.</summary>
    [DisallowMultipleComponent]
    [HelpURL("https://www.crosstales.com/media/data/assets/badwordfilter/api/class_crosstales_1_1_b_w_f_1_1_manager_1_1_bad_word_manager.html")]
    public class BadWordManager : BaseManager
    {

        #region Variables

        [Header("Specific Settings")]
        /// <summary>Replace characters for bad words (default: *).</summary>
        [Tooltip("Replace characters for bad words (default: *).")]
        public string ReplaceChars = "*"; //e.g. "?#@*&%!$^~+-/<>:;=()[]{}"

        /// <summary>Replace Leet speak in the input string (default: true).</summary>
        [Tooltip("Replace Leet speak in the input string (default: true).")]
        public bool ReplaceLeetSpeak = false;

        /// <summary>Use simple detection algorithm. This is the way to check for Chinese, Japanese, Korean and Thai bad words (default: false).</summary>
        [Tooltip("Use simple detection algorithm. This is the way to check for Chinese, Japanese, Korean and Thai bad words (default: false).")]
        public bool SimpleCheck = false;

        [Header("Bad Word Providers")]

        /// <summary>List of all left-to-right providers.</summary>
        [Tooltip("List of all left-to-right providers.")]
        public System.Collections.Generic.List<Provider.BadWordProvider> BadWordProviderLTR;

        /// <summary>List of all right-to-left providers.</summary>
        [Tooltip("List of all right-to-left providers.")]
        public System.Collections.Generic.List<Provider.BadWordProvider> BadWordProviderRTL;

        //private static bool initalized = false;
        private static Filter.BadWordFilter filter;
        private static BadWordManager instance;
        private static bool loggedFilterIsNull = false;
        private static bool loggedOnlyOneInstance = false;

        private const string clazz = "BadWordManager";

        #endregion


        #region Static properties

        /// <summary>Replace characters for bad words.</summary>
        public static string ReplaceCharacters
        {
            get
            {
                if (filter != null)
                {
                    return filter.ReplaceCharacters;
                }
                else
                {
                    if (instance != null)
                    {
                        return instance.ReplaceChars;
                    }
                }

                return "*";
            }

            set
            {
                if (filter != null)
                {
                    filter.ReplaceCharacters = value;
                    instance.ReplaceChars = value;
                }
                else
                {
                    if (instance != null)
                    {
                        instance.ReplaceChars = value;
                    }
                }
            }
        }

        /// <summary>Replace Leet speak in the input string.</summary>
        public static bool isReplaceLeetSpeak
        {
            get
            {
                if (filter != null)
                {
                    return filter.ReplaceLeetSpeak;
                }
                else
                {
                    if (instance != null)
                    {
                        return instance.ReplaceLeetSpeak;
                    }
                }

                return false;
            }

            set
            {
                if (filter != null)
                {
                    filter.ReplaceLeetSpeak = value;
                    instance.ReplaceLeetSpeak = value;
                }
                else
                {
                    if (instance != null)
                    {
                        instance.ReplaceLeetSpeak = value;
                    }
                }
            }
        }

        /// <summary>Use simple detection algorithm.</summary>
        public static bool isSimpleCheck
        {
            get
            {
                if (filter != null)
                {
                    return filter.SimpleCheck;
                }
                else
                {
                    if (instance != null)
                    {
                        return instance.SimpleCheck;
                    }
                }

                return false;
            }

            set
            {
                if (filter != null)
                {
                    filter.SimpleCheck = value;
                    instance.SimpleCheck = value;
                }
                else
                {
                    if (instance != null)
                    {
                        instance.SimpleCheck = value;
                    }
                }
            }
        }

        /// <summary>Checks the readiness status of the manager.</summary>
        /// <returns>True if the manager is ready.</returns>
        public static bool isReady
        {
            get
            {
                bool result = false;

                if (filter != null)
                {
                    result = filter.isReady;
                }
                else
                {
                    logFilterIsNull(clazz);
                }

                return result;
            }
        }

        /// <summary>Returns all sources for the manager.</summary>
        /// <returns>List with all sources for the manager</returns>
        public static System.Collections.Generic.List<Data.Source> Sources
        {
            get
            {
                System.Collections.Generic.List<Data.Source> result = new System.Collections.Generic.List<Data.Source>();

                if (filter != null)
                {
                    result = filter.Sources;
                }
                else
                {
                    logFilterIsNull(clazz);
                }

                return result;
            }
        }

        #endregion

        /*
        #region Events

        public delegate void ProviderReady();

        /// <summary>An event triggered whenever all providers are ready.</summary>
        public event ProviderReady OnProviderReady;

        #endregion
    */

        #region MonoBehaviour methods

        public void OnEnable()
        {
            //if (Util.Helper.isEditorMode || !initalized)
            if (instance == null)
            {
                instance = this;

                Load();

                if (!Util.Helper.isEditorMode && DontDestroy)
                {
                    DontDestroyOnLoad(transform.root.gameObject);
                    //initalized = true;
                }
                /*
                else
                {
                    initalized = true;
                }
                */
            }
            else
            {
                if (!Util.Helper.isEditorMode && DontDestroy && instance != this)
                {
                    if (!loggedOnlyOneInstance)
                    {
                        loggedOnlyOneInstance = true;

                        Debug.LogWarning("Only one active instance of '" + clazz + "' allowed in all scenes!" + System.Environment.NewLine + "This object will now be destroyed.");
                    }

                    Destroy(transform.root.gameObject, 0.2f);
                }
            }
        }

        #endregion


        #region Static methods

        /// <summary>Resets this object.</summary>
        public static void Reset()
        {
            filter = null;
            instance = null;
            loggedFilterIsNull = false;
            loggedOnlyOneInstance = false;
        }

        /// <summary>Loads the current filter with all settings from this object.</summary>
        public static void Load()
        {
            if (instance != null)
            {
                filter = new Filter.BadWordFilter(instance.BadWordProviderLTR, instance.BadWordProviderRTL, instance.ReplaceChars, instance.ReplaceLeetSpeak, instance.SimpleCheck);
            }
        }

        /// <summary>Searches for bad words in a text.</summary>
        /// <param name="text">Text to check</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>True if a match was found</returns>
        public static bool Contains(string text, params string[] sourceNames)
        {
            bool result = false;

            if (!string.IsNullOrEmpty(text))
            {
                if (filter != null)
                {
                    result = filter.Contains(text, sourceNames);
                }
                else
                {
                    logFilterIsNull(clazz);
                }
            }

            return result;
        }

        /// <summary>Searches for bad words in a text (call as thread).</summary>
        /// <param name="result">out-parameter: true if a match was found</param>
        /// <param name="text">Text to check</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>True if a match was found</returns>
        public static void ContainsMT(out bool result, string text, params string[] sourceNames)
        {
            result = Contains(text, sourceNames);
        }

        /// <summary>Searches for bad words in a text.</summary>
        /// <param name="text">Text to check</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>List with all the matches</returns>
        public static System.Collections.Generic.List<string> GetAll(string text, params string[] sourceNames)
        {
            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(text))
            {
                if (filter != null)
                {
                    result = filter.GetAll(text, sourceNames);
                }
                else
                {
                    logFilterIsNull(clazz);
                }
            }

            return result;
        }

        /// <summary>Searches for bad words in a text (call as thread).</summary>
        /// <param name="result">out-parameter: List with all the matches</param>
        /// <param name="text">Text to check</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        public static void GetAllMT(out System.Collections.Generic.List<string> result, string text, params string[] sourceNames)
        {
            result = GetAll(text, sourceNames);
        }

        /// <summary>Searches and replaces all bad words in a text.</summary>
        /// <param name="text">Text to check</param>
        /// <param name="markOnly">Only mark the words (default: false, optional)</param>
        /// <param name="prefix">Prefix for every found bad word (optional)</param>
        /// <param name="postfix">Postfix for every found bad word (optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>Clean text</returns>
        public static string ReplaceAll(string text, bool markOnly = false, string prefix = "", string postfix = "", params string[] sourceNames)
        {
            string result = text;

            if (!string.IsNullOrEmpty(text))
            {
                if (filter != null)
                {
                    result = filter.ReplaceAll(text, markOnly, prefix, postfix, sourceNames);
                }
                else
                {
                    logFilterIsNull(clazz);
                }
            }

            return result;
        }

        /// <summary>Searches and replaces all bad words in a text  (call as thread).</summary>
        /// <param name="result">out-parameter: clean text</param>
        /// <param name="text">Text to check</param>
        /// <param name="markOnly">Only mark the words (default: false, optional)</param>
        /// <param name="prefix">Prefix for every found bad word (optional)</param>
        /// <param name="postfix">Postfix for every found bad word (optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        public static void ReplaceAllMT(out string result, string text, bool markOnly = false, string prefix = "", string postfix = "", params string[] sourceNames)
        {
            result = ReplaceAll(text, markOnly, prefix, postfix, sourceNames);
        }

        /*
        /// <summary>
        /// Replaces all bad words in a text.
        /// Use this method if you already have a list of bad words (e.g. from the 'GetAll()' method).
        /// </summary>
        /// <param name="text">Text containig bad words</param>
        /// <param name="badWords">Bad words to replace</param>
        /// <param name="markOnly">Only mark the words (default: false, optional)</param>
        /// <param name="prefix">Prefix for every found bad word (optional)</param>
        /// <param name="postfix">Postfix for every found bad word (optional)</param>
        /// <returns>Clean text</returns>
        public static string Replace(string text, System.Collections.Generic.List<string> badWords, bool markOnly = false, string prefix = "", string postfix = "")
        {
            string result = text;

            if (!string.IsNullOrEmpty(text))
            {
                if (filter != null)
                {
                    result = filter.Replace(text, badWords, markOnly, prefix, postfix);
                }
                else
                {
                    logFilterIsNull(clazz);
                }
            }
            return result;
        }
        */

        /*
    /// <summary>Marks the text with a prefix and postfix from a list of words.</summary>
    /// <param name="text">Text containig bad words</param>
    /// <param name="badWords">Bad words to mark</param>
    /// <param name="prefix">Prefix for every found bad word (default: bold and red, optional)</param>
    /// <param name="postfix">Postfix for every found bad word (default: bold and red, optional)</param>
    /// <returns>Text with marked bad words</returns>
    public static string Mark(string text, System.Collections.Generic.List<string> badWords, string prefix = "<b><color=red>", string postfix = "</color></b>")
    {
        string result = text;

        if (!string.IsNullOrEmpty(text))
        {
            if (filter != null)
            {
                result = filter.Mark(text, badWords, prefix, postfix);
            }
            else
            {
                logFilterIsNull(clazz);
            }
        }
        return result;
    }
    */

        /// <summary>Marks the text with a prefix and postfix.</summary>
        /// <param name="text">Text containig bad words</param>
        /// <param name="replace">Replace the bad words (default: false, optional)</param>
        /// <param name="prefix">Prefix for every found bad word (default: bold and red, optional)</param>
        /// <param name="postfix">Postfix for every found bad word (default: bold and red, optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>Text with marked domains</returns>
        public static string Mark(string text, bool replace = false, string prefix = "<b><color=red>", string postfix = "</color></b>", params string[] sourceNames)
        {
            string result = text;

            if (!string.IsNullOrEmpty(text))
            {
                if (filter != null)
                {
                    result = filter.Mark(text, replace, prefix, postfix, sourceNames);
                }
                else
                {
                    logFilterIsNull(clazz);
                }
            }

            return result;
        }

        /// <summary>Unmarks the text with a prefix and postfix.</summary>
        /// <param name="text">Text with marked bad words</param>
        /// <param name="prefix">Prefix for every found bad word (default: bold and red, optional)</param>
        /// <param name="postfix">Postfix for every found bad word (default: bold and red, optional)</param>
        /// <returns>Text with unmarked bad words</returns>
        public static string Unmark(string text, string prefix = "<b><color=red>", string postfix = "</color></b>")
        {
            string result = text;

            if (!string.IsNullOrEmpty(text))
            {
                if (filter != null)
                {
                    result = filter.Unmark(text, prefix, postfix);
                }
                else
                {
                    logFilterIsNull(clazz);
                }
            }

            return result;
        }

        private static void logFilterIsNull(string clazz)
        {
            if (!loggedFilterIsNull)
            {
                Debug.LogWarning("'filter' is null!" + System.Environment.NewLine + "Did you add the '" + clazz + "' to the current scene?");
                loggedFilterIsNull = true;
            }
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)