using UnityEditor;
using Crosstales.BWF.EditorUtil;

namespace Crosstales.BWF.EditorExtension
{
    /// <summary>Base-class for custom editors of children of the 'BaseProvider'-class.</summary>
    public abstract class BaseProviderEditor : Editor
    {
        #region Variables

        private Provider.BaseProvider script;

        #endregion


        #region Editor methods

        public virtual void OnEnable()
        {
            script = (Provider.BaseProvider)target;

            //UnityEngine.Debug.Log(script.isActiveAndEnabled);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            //EditorHelper.SeparatorUI();

            if (script.isActiveAndEnabled)
            {
                if (script.Sources != null && script.Sources.Length > 0)
                {
                    //do nothing
                }
                else
                {
                   EditorHelper.SeparatorUI();
                   EditorGUILayout.HelpBox("Please add an entry to 'Sources'!", MessageType.Warning);
                }
            }
            else
            {
                EditorHelper.SeparatorUI();

                EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
            }
        }

        #endregion
    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)