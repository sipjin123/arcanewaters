using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

namespace ItemDefinitionTool
{
   public class ItemDefinitionList : MonoBehaviour
   {
      #region Public Variables

      // Singleton instance
      public static ItemDefinitionList self;

      // Prefab for the item definition entry in the list
      public ItemDefinitionListEntry entryPref;

      #endregion

      private void Awake () {
         self = this;
      }

      public void set (List<ItemDefinition> definitions) {
         // Destroy old entries
         foreach (Transform child in transform) {
            Destroy(child.gameObject);
         }

         // Create new definition entries
         foreach (ItemDefinition definition in definitions) {
            ItemDefinitionListEntry entry = Instantiate(entryPref, transform);
            entry.set(definition);
         }
      }

      #region Private Variables

      #endregion
   }
}