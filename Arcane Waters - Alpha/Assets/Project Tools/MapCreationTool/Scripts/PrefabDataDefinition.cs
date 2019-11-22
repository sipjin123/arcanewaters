using UnityEngine;
using System.Linq;
using System;

namespace MapCreationTool
{
   public class PrefabDataDefinition : MonoBehaviour
   {
      public string title = "";
      public DataField[] dataFields = new DataField[0];
      public SelectDataField[] selectDataFields = new SelectDataField[0];
      public CustomDataField[] customDataFields = new CustomDataField[0];

      private void Awake () {
         foreach (var customData in customDataFields) {
            if (customData.type == CustomFieldType.Direction) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  options = new string[] { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" }
               };
            }
         }
      }
   }

   [System.Serializable]
   public class DataField
   {
      public string name;
      public string defaultValue;
      public DataFieldType type;
   }

   [System.Serializable]
   public class SelectDataField
   {
      public string name;
      public int defaultOption;
      public string[] options;
   }

   [System.Serializable]
   public class CustomDataField
   {
      public string name;
      public CustomFieldType type;
   }

   public enum DataFieldType
   {
      Int,
      Float,
      String
   }

   public enum CustomFieldType
   {
      Direction
   }
}