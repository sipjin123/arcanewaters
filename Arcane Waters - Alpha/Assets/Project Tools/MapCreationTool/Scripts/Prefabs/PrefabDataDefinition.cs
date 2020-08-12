using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;

namespace MapCreationTool
{
   public class PrefabDataDefinition : MonoBehaviour
   {
      [Tooltip("Title of the prefab, that appears in the editor")]
      public string title = "";

      [Tooltip("Can this prefab be placed on other prefabs")]
      public bool canInheritPosition = false;

      [Tooltip("Can this prefab have other prefabs on top")]
      public bool canControlPosition = false;

      [Tooltip("At what intervals can the position be changed, 0 if any position is valid")]
      public Vector2 positionStep = Vector2.zero;

      [Tooltip("Data fields that can be set by the user")]
      public DataField[] dataFields = new DataField[0];

      [Tooltip("Dropdown datafields that can be set by the user")]
      public SelectDataField[] selectDataFields = new SelectDataField[0];

      [Tooltip("Custom defined datafields, that are restructured to regular fields on start")]
      public CustomDataField[] customDataFields = new CustomDataField[0];

      /// <summary>
      /// Turns all custom fields into regular data fields
      /// </summary>
      public void restructureCustomFields () {
         foreach (CustomDataField customData in customDataFields) {
            if (customData.type == CustomFieldType.Direction) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = SelectOption.formOptions("North", "NorthEast", "East", "SouthEast", "South", "SouthWest", "West", "NorthWest"),
                  defaultOption = 4
               };
            } else if (customData.type == CustomFieldType.NPC && NPCManager.instance.npcCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = NPCManager.instance.formSelectionOptions()
               };
            } else if (customData.type == CustomFieldType.ShopPanelType && NPCManager.instance.npcCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = SelectOption.formOptions(Panel.Type.None.ToString(), Panel.Type.Adventure.ToString(), Panel.Type.Shipyard.ToString(), Panel.Type.Merchant.ToString())
               };
            } else if (customData.type == CustomFieldType.ShopName && ShopManager.instance.shopEntryCount > 0 && NPCManager.instance.npcCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = ShopManager.instance.formSelectionOptions()
               };
            } else if (customData.type == CustomFieldType.LandMonster && MonsterManager.instance.landMonsterCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = MonsterManager.instance.formLandMonsterSelectionOptions()
               };
            } else if (customData.type == CustomFieldType.SeaMonster && MonsterManager.instance.seaMonsterCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = MonsterManager.instance.formSeaMonsterSelectionOptions()
               };
            } else if (customData.type == CustomFieldType.Book && BooksManager.instance.booksCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = BooksManager.instance.formSelectionOptions()
               };
            } else if (customData.type == CustomFieldType.Ship && ShipManager.instance.shipCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = MonsterManager.instance.formPirateShipOptions()
               };
            } else if (customData.type == CustomFieldType.ActionName) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = SelectOption.formOptions(GenericActionTrigger.actions.Keys.ToArray())
               };
            } else if (customData.type == CustomFieldType.Discovery && MapEditorDiscoveriesManager.instance.discoveriesCount > 0) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = MapEditorDiscoveriesManager.instance.formSelectionOptions()
               };
            } else if (customData.type == CustomFieldType.SecretType) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = SecretsMapManager.instance.formSelectionOptions()
               };
            } else if (customData.type == CustomFieldType.SecretStartSprite) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = SecretsMapManager.instance.formInitialSprite()
               };
            } else if (customData.type == CustomFieldType.SecretInteractSprite) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = SecretsMapManager.instance.formInitialSprite()
               };
            } else if (customData.type == CustomFieldType.TreasureType) {
               Array.Resize(ref selectDataFields, selectDataFields.Length + 1);
               selectDataFields[selectDataFields.Length - 1] = new SelectDataField {
                  name = customData.name,
                  toolTip = customData.toolTip,
                  options = SecretsMapManager.instance.formInitialSprite()
               };
            }
         }

         customDataFields = new CustomDataField[0];
      }

      [Serializable]
      public class DataField
      {
         public string name;
         public string defaultValue;
         public string toolTip;
         public DataFieldType type;
      }

      [Serializable]
      public class SelectDataField
      {
         public string name;
         public int defaultOption;
         public string toolTip;
         public SelectOption[] options;
      }

      [Serializable]
      public class CustomDataField
      {
         public string name;
         public string toolTip;
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
         ShopPanelType,
         ShopName,
         LandMonster,
         SeaMonster,
         ActionName,
         Book,
         Discovery,
         SecretType,
         SecretStartSprite,
         SecretInteractSprite,
         Ship,
         TreasureType,
      }
   }
}
