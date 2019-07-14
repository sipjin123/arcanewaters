using UnityEngine;

namespace Crosstales.BWF.Manager
{
    /// <summary>Base class for all managers.</summary>
    [ExecuteInEditMode]
    public abstract class BaseManager : MonoBehaviour
    {

        #region Variables

        /*
        [Header("Marker Settings")]
        /// <summary>Mark prefix for bad words (default: bold and color).</summary>
        [Tooltip("Mark prefix for bad words (default: bold and color).")]
        public string MarkPrefix = "<b><color=red>";

        /// <summary>Mark postfix for bad words (default: bold and color).</summary>
        [Tooltip("Mark postfix for bad words (default: bold and color).")]
        public string MarkPostfix = "</color></b>";
        */

        [Header("Behaviour Settings")]
        /// <summary>Don't destroy gameobject during scene switches (default: true).</summary>
        [Tooltip("Don't destroy gameobject during scene switches (default: true).")]
        public bool DontDestroy = true;

        #endregion

        /*
        #region Events

        public delegate void ManagerReady();

        /// <summary>An event triggered whenever the manager is ready.</summary>
        public event ManagerReady OnManagerReady;

        #endregion

        protected void raiseOnManagerReady()
        {
            if (OnManagerReady != null)
            {
                OnManagerReady();
            }
        }
        */
    }
}
// © 2015-2019 crosstales LLC (https://www.crosstales.com)