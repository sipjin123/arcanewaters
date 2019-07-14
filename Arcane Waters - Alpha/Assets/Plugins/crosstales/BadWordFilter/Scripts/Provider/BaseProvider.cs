using UnityEngine;

namespace Crosstales.BWF.Provider
{
    /// <summary>Base class for all providers.</summary>
    [ExecuteInEditMode]
    public abstract class BaseProvider : MonoBehaviour, IProvider
    {

        #region Variables

        /// <summary>Name to identify the provider.</summary>
        //[Tooltip("Name to identify the provider.")]
        //public string Name = string.Empty; //to identify the provider

        [Header("Regex Options")]
        /// <summary>Option1 (default: RegexOptions.IgnoreCase).</summary>
        [Tooltip("Option1 (default: RegexOptions.IgnoreCase).")]
        public System.Text.RegularExpressions.RegexOptions RegexOption1 = System.Text.RegularExpressions.RegexOptions.IgnoreCase; //DEFAULT

        /// <summary>Option2 (default: RegexOptions.CultureInvariant).</summary>
        [Tooltip("Option2 (default: RegexOptions.CultureInvariant).")]
        public System.Text.RegularExpressions.RegexOptions RegexOption2 = System.Text.RegularExpressions.RegexOptions.CultureInvariant; //DEFAULT

        /// <summary>Option3 (default: RegexOptions.None).</summary>
        [Tooltip("Option3 (default: RegexOptions.None).")]
        public System.Text.RegularExpressions.RegexOptions RegexOption3 = System.Text.RegularExpressions.RegexOptions.None;

        /// <summary>Option4 (default: RegexOptions.None).</summary>
        [Tooltip("Option4 (default: RegexOptions.None).")]
        public System.Text.RegularExpressions.RegexOptions RegexOption4 = System.Text.RegularExpressions.RegexOptions.None;

        /// <summary>Option5 (default: RegexOptions.None).</summary>
        [Tooltip("Option5 (default: RegexOptions.None).")]
        public System.Text.RegularExpressions.RegexOptions RegexOption5 = System.Text.RegularExpressions.RegexOptions.None;

        [Header("Sources")]
        /// <summary>All sources for this provider.</summary>
        [Tooltip("All sources for this provider.")]
        [ContextMenuItem("Create Source", "createSource")]
        public Data.Source[] Sources;
        

        [Header("Load Behaviour")]
        /// <summary>Clears all existing bad words on 'Load' (default: true).</summary>
        [Tooltip("Clears all existing bad words on 'Load' (default: true).")]
        public bool ClearOnLoad = true;

        //protected System.Collections.Generic.List<System.Guid> coRoutines = new System.Collections.Generic.List<System.Guid>();
        protected System.Collections.Generic.List<string> coRoutines = new System.Collections.Generic.List<string>();

        protected static bool loggedUnsupportedPlatform = false;
        protected bool loading = false;

        #endregion


        /*
        #region Properties

        /// <summary>Checks the readiness status of the provider.</summary>
        /// <returns>True if the provider is ready.</returns>
        public bool isReady
        {
            get;
            protected set;
        }

        #endregion
            */

        /*
        #region Events

        public delegate void ProviderReady();

        /// <summary>An event triggered whenever the provider is ready.</summary>
        public event ProviderReady OnProviderReady;

        #endregion
    */

        #region Implemented methods

        public bool isReady
        {
            get;
            set;
        }

        public abstract void Load();

        public abstract void Save();
        
        #endregion


        #region Abstract methods

        /// <summary>Intialize the provider.</summary>
        protected abstract void init();

        #endregion


        #region MonoBehaviour methods

        public void Awake()
        {
            Load();
        }

        /*
        public void OnEnable()
        {
            name = Name;
        }
        */

        #endregion


        #region Protected methods

        protected void logNoResourcesAdded()
        {
            Debug.LogWarning("No 'Resources' for " + name + " added!" + System.Environment.NewLine + "If you want to use this functionality, please add your desired 'Resources'.");
        }

        protected void createSource()
        {
            Util.Helper.CreateSource();
        }

        //protected void raiseOnProviderReady()
        //{
        //    isReady = true;

        //    if (OnProviderReady != null)
        //    {
        //        OnProviderReady();
        //    }
        //}

        #endregion
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)