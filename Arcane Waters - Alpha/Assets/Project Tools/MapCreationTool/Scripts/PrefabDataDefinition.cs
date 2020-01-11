using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MapCreationTool
{
   public class PrefabDataDefinition : MonoBehaviour
   {
      public string title = "";
      public DataField[] dataFields = new DataField[0];
      public SelectDataField[] selectDataFields = new SelectDataField[0];
      public CustomDataField[] customDataFields = new CustomDataField[0];

      /// <summary>
      /// Turns all custom fields into regular data fields
      /// </summary>
      public void restructureCustomFields() {
         foreach (var customData in customDataFields) {
            if (customData.type == CustomFieldType.Direction) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  options = new string[] { "North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest" }
               };
            } else if (customData.type == CustomFieldType.NPC && NPCManager.instance.npcCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  options = NPCManager.instance.formSelectionOptions()
               };
            } else if (customData.type == CustomFieldType.ShopPanelType && NPCManager.instance.npcCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               List<string> optionList = new List<string>();

               optionList.Add(Panel.Type.None.ToString());
               optionList.Add(Panel.Type.Adventure.ToString());
               optionList.Add(Panel.Type.Shipyard.ToString());
               optionList.Add(Panel.Type.Merchant.ToString());

               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  options = optionList.ToArray()
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
      String,
      Bool
   }

   public enum CustomFieldType
   {
      Direction,
      NPC,
      ShopPanelType
   }
}