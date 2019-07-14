using UnityEngine;
using System.Linq;

namespace Crosstales.BWF
{
    /// <summary>BWF is a multi-manager for all available managers.</summary>
    [ExecuteInEditMode]
    [HelpURL("https://www.crosstales.com/media/data/assets/badwordfilter/api/class_crosstales_1_1_b_w_f_1_1_b_w_f_manager.html")]
    public class BWFManager : MonoBehaviour
    {
        #region Variables

        private GameObject root;

        private bool sentReady = false;

        #endregion


        #region Static properties

        /// <summary>Checks the readiness status of all managers.</summary>
        /// <returns>True if all managers are ready.</returns>
        public static bool isReady
        {
            get
            {
                return Manager.BadWordManager.isReady && Manager.DomainManager.isReady && Manager.CapitalizationManager.isReady && Manager.PunctuationManager.isReady;
            }
        }

        #endregion


        #region MonoBehaviour methods

        public void OnEnable()
        {
            root = transform.root.gameObject;
        }

        public void Update()
        {
            if (Util.Helper.isEditorMode)
            {
                if (root != null)
                {
                    if (Util.Config.ENSURE_NAME)
                        root.name = Util.Constants.MANAGER_SCENE_OBJECT_NAME; //ensure name
                }
            }

            if (!sentReady && isReady)
            {
                sentReady = true;

                onBWFReady();
            }
        }

        #endregion


        #region Events

        public delegate void BWFReady();

        private static BWFReady _onBWFReady;

        /// <summary>An event triggered whenever BWF is ready.</summary>
        public static event BWFReady OnBWFReady
        {
            add { _onBWFReady += value; }
            remove { _onBWFReady -= value; }
        }

        #endregion


        #region Static methods

        /// <summary>Loads the filter of a manager.</summary>
        /// <param name="mask">Active manager (default: ManagerMask.All, optional)</param>
        public static void Load(Model.ManagerMask mask = Model.ManagerMask.All)
        {
            if ((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                Manager.BadWordManager.Load();
            }

            if ((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                Manager.DomainManager.Load();
            }

            if ((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                Manager.CapitalizationManager.Load();
            }

            if ((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                Manager.PunctuationManager.Load();
            }
        }

        /// <summary>Returns all sources for a manager.</summary>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <returns>List with all sources for the selected manager</returns>
        public static System.Collections.Generic.List<Data.Source> Sources(Model.ManagerMask mask = Model.ManagerMask.All)
        {
            System.Collections.Generic.List<Data.Source> result = new System.Collections.Generic.List<Data.Source>(30);

            if ((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result.AddRange(Manager.BadWordManager.Sources);
            }

            if ((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result.AddRange(Manager.DomainManager.Sources);
            }

            return result.Distinct().OrderBy(x => x.Name).ToList();
        }

        /// <summary>Searches for unwanted words in a text.</summary>
        /// <param name="text">Text to check</param>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>True if a match was found</returns>
        public static bool Contains(string text, Model.ManagerMask mask = Model.ManagerMask.All, params string[] sourceNames)
        {
            return (((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All) && Manager.BadWordManager.Contains(text, sourceNames)) ||
                (((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All) && Manager.DomainManager.Contains(text, sourceNames)) ||
               (((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All) && Manager.CapitalizationManager.Contains(text)) ||
               (((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All) && Manager.PunctuationManager.Contains(text));
        }

        /// <summary>Searches for unwanted words in a text (call as thread).</summary>
        /// <param name="result">out-parameter: true if a match was found</param>
        /// <param name="text">Text to check</param>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        public static void ContainsMT(out bool result, ref string text, Model.ManagerMask mask = Model.ManagerMask.All, params string[] sourceNames)
        {
            result = (((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All) && Manager.BadWordManager.Contains(text, sourceNames)) ||
                (((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All) && Manager.DomainManager.Contains(text, sourceNames)) ||
               (((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All) && Manager.CapitalizationManager.Contains(text)) ||
               (((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All) && Manager.PunctuationManager.Contains(text));
        }

        /// <summary>Searches for unwanted words in a text.</summary>
        /// <param name="text">Text to check</param>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>List with all the matches</returns>
        public static System.Collections.Generic.List<string> GetAll(string text, Model.ManagerMask mask = Model.ManagerMask.All, params string[] sourceNames)
        {
            System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();

            if ((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result.AddRange(Manager.BadWordManager.GetAll(text, sourceNames));
            }

            if ((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result.AddRange(Manager.DomainManager.GetAll(text, sourceNames));
            }

            if ((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result.AddRange(Manager.CapitalizationManager.GetAll(text));
            }

            if ((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result.AddRange(Manager.PunctuationManager.GetAll(text));
            }

            return result.Distinct().OrderBy(x => x).ToList();
        }

        /// <summary>Searches for unwanted words in a text (call as thread).</summary>
        /// <param name="result">out-parameter: List with all the matches</param>
        /// <param name="text">Text to check</param>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        public static void GetAllMT(out System.Collections.Generic.List<string> result, ref string text, Model.ManagerMask mask = Model.ManagerMask.All, params string[] sourceNames)
        {
            System.Collections.Generic.List<string> tmp = new System.Collections.Generic.List<string>();

            if ((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                tmp.AddRange(Manager.BadWordManager.GetAll(text, sourceNames));
            }

            if ((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                tmp.AddRange(Manager.DomainManager.GetAll(text, sourceNames));
            }

            if ((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                tmp.AddRange(Manager.CapitalizationManager.GetAll(text));
            }

            if ((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                tmp.AddRange(Manager.PunctuationManager.GetAll(text));
            }

            result = tmp.Distinct().OrderBy(x => x).ToList();
        }

        /// <summary>Searches and replaces all unwanted words in a text.</summary>
        /// <param name="text">Text to check</param>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>Clean text</returns>
        public static string ReplaceAll(string text, Model.ManagerMask mask = Model.ManagerMask.All, params string[] sourceNames)
        {
            //TODO add pre- and postfix!

            string result = text ?? string.Empty;

            if ((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.BadWordManager.ReplaceAll(result, false, string.Empty, string.Empty, sourceNames);
            }

            if ((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.DomainManager.ReplaceAll(result, false, string.Empty, string.Empty, sourceNames);
            }

            if ((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.CapitalizationManager.ReplaceAll(result, false, string.Empty, string.Empty);
            }

            if ((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.PunctuationManager.ReplaceAll(result, false, string.Empty, string.Empty);
            }

            return result;
        }

        /// <summary>Searches and replaces all unwanted words in a text (call as thread).</summary>
        /// <param name="result">out-parameter: clean text</param>
        /// <param name="text">Text to check</param>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        public static void ReplaceAllMT(out string result, ref string text, Model.ManagerMask mask = Model.ManagerMask.All, params string[] sourceNames)
        {
            //TODO add pre- and postfix!

            result = text ?? string.Empty;

            if ((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.BadWordManager.ReplaceAll(result, false, string.Empty, string.Empty, sourceNames);
            }

            if ((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.DomainManager.ReplaceAll(result, false, string.Empty, string.Empty, sourceNames);
            }

            if ((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.CapitalizationManager.ReplaceAll(result, false, string.Empty, string.Empty);
            }

            if ((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.PunctuationManager.ReplaceAll(result, false, string.Empty, string.Empty);
            }
        }

        /*
        /// <summary>
        /// Replaces all unwanted words in a text.
        /// Use this method if you already have a list of bad words (e.g. from the 'GetAll()' method).
        /// </summary>
        /// <param name="text">Text containig unwanted words</param>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <param name="unwantedWords">Unwanted words to replace</param>
        /// <returns>Clean text</returns>
        public static string Replace(string text, System.Collections.Generic.List<string> unwantedWords, Model.ManagerMask mask = Model.ManagerMask.All)
        {
            //TODO add pre- and postfix!

            string result = text ?? string.Empty;

            if ((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.BadWordManager.Replace(result, unwantedWords, false, string.Empty, string.Empty);
            }

            if ((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.DomainManager.Replace(result, unwantedWords, false, string.Empty, string.Empty);
            }

            if ((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.CapitalizationManager.Replace(result, unwantedWords, false, string.Empty, string.Empty);
            }

            if ((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.PunctuationManager.Replace(result, unwantedWords, false, string.Empty, string.Empty);
            }

            return result;
        }
        */

        /// <summary>
        /// Marks the text with a prefix and postfix from a list of words.</summary>
        /// Use this method if you already have a list of bad words (e.g. from the 'GetAll()' method).
        /// </summary>
        /// <param name="text">Text containig unwanted words</param>
        /// <param name="unwantedWords">Unwanted words to mark</param>
        /// <param name="prefix">Prefix for every found unwanted word (optional)</param>
        /// <param name="postfix">Postfix for every found unwanted word (optional)</param>
        /// <returns>Text with marked unwanted words</returns>
        public static string Mark(string text, System.Collections.Generic.List<string> unwantedWords, string prefix = "<b><color=red>", string postfix = "</color></b>")
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

                    _prefix = string.Empty;
                }

                if (string.IsNullOrEmpty(postfix))
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'postfix' is null!" + System.Environment.NewLine + "=> Using an empty string as postfix.");

                    _postfix = string.Empty;
                }

                if (unwantedWords == null || unwantedWords.Count == 0)
                {
                    if (Util.Constants.DEV_DEBUG)
                        Debug.LogWarning("Parameter 'unwantedWords' is null or empty!" + System.Environment.NewLine + "=> 'Mark()' will return the original string.");
                }
                else
                {
                    foreach (string unwantedWord in unwantedWords)
                    {
                        if (!string.IsNullOrEmpty(unwantedWord))
                        {
                            result = result.Replace(unwantedWord, _prefix + unwantedWord + _postfix);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>Marks the text with a prefix and postfix.</summary>
        /// <param name="text">Text containig unwanted words</param>
        /// <param name="replace">Replace the bad words (default: false, optional)</param>
        /// <param name="prefix">Prefix for every found unwanted word (optional)</param>
        /// <param name="postfix">Postfix for every found unwanted word (optional)</param>
        /// <param name="mask">Active manager (default: Model.ManagerMask.All, optional)</param>
        /// <param name="sourceNames">Relevant sources (e.g. "english", optional)</param>
        /// <returns>Clean text</returns>
        public static string Mark(string text, bool replace = false, string prefix = "<b><color=red>", string postfix = "</color></b>", Model.ManagerMask mask = Model.ManagerMask.All, params string[] sourceNames)
        {
            string result = text ?? string.Empty;

            if ((mask & Model.ManagerMask.BadWord) == Model.ManagerMask.BadWord || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.BadWordManager.Mark(result, replace, prefix, postfix, sourceNames);
            }

            if ((mask & Model.ManagerMask.Domain) == Model.ManagerMask.Domain || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.DomainManager.Mark(result, replace, prefix, postfix, sourceNames);
            }

            if ((mask & Model.ManagerMask.Capitalization) == Model.ManagerMask.Capitalization || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.CapitalizationManager.Mark(result, replace, prefix, postfix);
            }

            if ((mask & Model.ManagerMask.Punctuation) == Model.ManagerMask.Punctuation || (mask & Model.ManagerMask.All) == Model.ManagerMask.All)
            {
                result = Manager.PunctuationManager.Mark(result, replace, prefix, postfix);
            }

            return result;
        }

        /// <summary>Unmarks the text with a prefix and postfix.</summary>
        /// <param name="text">Text with marked unwanted words</param>
        /// <param name="prefix">Prefix for every found unwanted word (optional)</param>
        /// <param name="postfix">Postfix for every found unwanted word (optional)</param>
        /// <returns>Text with unmarked unwanted words</returns>
        public static string Unmark(string text, string prefix = "<b><color=red>", string postfix = "</color></b>")
        {
            string result = text ?? string.Empty;

            //The different mangers all do the same.
            result = Manager.BadWordManager.Unmark(result, prefix, postfix);

            return result;
        }

        #endregion


        #region Callbacks

        private void onBWFReady()
        {
            if (_onBWFReady != null)
            {
                _onBWFReady();
            }
        }

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)