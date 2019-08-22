using UnityEditor;
namespace ItemEditor
{
   [CustomEditor(typeof(ItemBuilder))]
   public class ItemBuilderEditor : Editor
   {
      public override void OnInspectorGUI () {

         EditorGUILayout.BeginHorizontal("box");

         EditorGUILayout.LabelField("Build Mode");

         EditorGUILayout.EndHorizontal();
      }
   }
}
