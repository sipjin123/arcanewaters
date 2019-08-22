using UnityEditor;

namespace ItemEditor
{
   /// <summary>
   /// Custom editor boiler plate code, in case a custom editor wants to be created for an
   /// AbilityData ScriptableObject.
   /// </summary>
   [CustomEditor(typeof(AbilityData))]
   public class AbilityEditor : Editor
   {
      #region Public Variables

      #endregion

      // This is called whenever we select a scriptable object file.
      private void OnEnable () {
         // We save that reference to be able to know which item we are editing.
         _item = (AbilityData) target;
      }

      public override void OnInspectorGUI () {
         base.OnInspectorGUI();
      }

      #region Private Variables

      // Which item will we edit whenever we select a scriptable object file
      private AbilityData _item;

      #endregion
   }
}


