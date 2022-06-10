using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class BiomeActivityRow : MonoBehaviour
{
   #region Public Variables

   // The progress bar
   public Image progressBarImage;

   // The progress text field
   public Text progressField;

   // The player reached text field
   public Text playersReachedField;

   #endregion

   public void initializeRow (int playerReachedCount) {
      playersReachedField.text = playerReachedCount.ToString();

      // Update exploration progress
      int visitedWorldMapAreasCount = WorldMapManager.self.getVisitedAreasCoordsList().Count;
      Vector2Int worldMapDimensions = WorldMapManager.self.getMapSize();

      // Temporary
      int forestBiomeTotalAreaCount = 30;
      float progressAmount =  (float) visitedWorldMapAreasCount / forestBiomeTotalAreaCount;

      progressBarImage.fillAmount = progressAmount;
      progressField.text = Mathf.RoundToInt(progressAmount * 100) + "%";
   }

   #region Private Variables

   #endregion
}
