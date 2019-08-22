using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class RandomMapsPanel : Panel, IPointerClickHandler
{
   #region Public Variables

   // The container of our map rows
   public GameObject rowsContainer;

   // The prefab we use for creating new rows
   public RandomMapRow rowPrefab;

   #endregion

   public void showPanelUsingMapSummaries (MapSummary[] mapSummaryArray) {
      // Clear out any old data
      rowsContainer.DestroyChildren();

      // Create a row for each Random Map Data
      foreach (MapSummary mapSummary in mapSummaryArray) {
         RandomMapRow row = Instantiate(rowPrefab);
         row.transform.SetParent(rowsContainer.transform);
         row.setRowFromSummary(mapSummary);
      }

      // Display the panel now that we have all of the data
      PanelManager.self.pushIfNotShowing(Type.RandomMaps);
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   #region Private Variables

   #endregion
}
