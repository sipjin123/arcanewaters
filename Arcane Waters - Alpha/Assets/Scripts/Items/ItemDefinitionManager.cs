using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class ItemDefinitionManager : MonoBehaviour
{
   #region Public Variables

   // A convenient self reference
   public static ItemDefinitionManager self;

   // List of items for preview in the editor
   public List<ItemDefinition> itemDefinitionsPreview = new List<ItemDefinition>();

   #endregion

   private void Awake () {
      self = this;
   }

   public ItemDefinition getDefinition (int id) {
      if (_itemDefinitions.TryGetValue(id, out ItemDefinition definition)) {
         return definition;
      }

      return null;
   }

   public IEnumerable<ItemDefinition> getDefinitions () {
      return _itemDefinitions.Values;
   }

   public List<ItemDefinition> getDefinitions (params int[] ids) {
      List<ItemDefinition> result = new List<ItemDefinition>();

      foreach (int id in ids) {
         if (_itemDefinitions.TryGetValue(id, out ItemDefinition definition)) {
            result.Add(definition);
         }
      }

      return result;
   }

   public void loadFromDatabase (Action<List<ItemDefinition>> callback = null) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<ItemDefinition> itemDefinitions = DB_Main.getItemDefinitions();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (ItemDefinition itemDefinition in itemDefinitions) {
               storeItemDefinition(itemDefinition);
            }

            callback?.Invoke(itemDefinitions);
         });
      });
   }

   public void storeItemDefinition (ItemDefinition definition) {
      // Save the data in the memory cache
      if (!_itemDefinitions.ContainsKey(definition.id)) {
         _itemDefinitions.Add(definition.id, definition);

      } else {
         _itemDefinitions[definition.id] = definition;
      }

#if UNITY_EDITOR
      // If we are in editor, update entry in preview list
      int index = itemDefinitionsPreview.FindIndex(i => i.id == definition.id);
      if (index != -1) {
         itemDefinitionsPreview.RemoveAt(index);
      }
      itemDefinitionsPreview.Add(definition);
#endif
   }

   public void clearAllData () {
      _itemDefinitions.Clear();
      itemDefinitionsPreview.Clear();
   }

   #region Private Variables

   // Stores all the item data
   private Dictionary<int, ItemDefinition> _itemDefinitions = new Dictionary<int, ItemDefinition>();

   #endregion
}
