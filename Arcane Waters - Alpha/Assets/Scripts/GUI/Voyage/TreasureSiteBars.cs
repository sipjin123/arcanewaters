using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureSiteBars : MonoBehaviour
{
   #region Public Variables

   // Our capture bar image
   public Image captureBarImage;

   // The container for the capture bar
   public GameObject barContainer;

   // The color of the capture bar when allies are capturing the site
   public Color enemyCaptureBarColor;

   // The color of the capture bar when enemies are capturing the site
   public Color alliedCaptureBarColor;

   // The icon displayed when the site belongs to our team
   public GameObject alliedIcon;

   // The icon displayed when the site belongs to an enemy team
   public GameObject enemyIcon;

   #endregion

   void Awake () {
      // Look up components
      _treasureSite = GetComponentInParent<TreasureSite>();
   }

   void Update () {
      if (_treasureSite == null || Global.player == null || !VoyageManager.isInGroup(Global.player)) {
         barContainer.SetActive(false);
         alliedIcon.SetActive(false);
         enemyIcon.SetActive(false);
         return;
      }

      // Check if the site has already been captured
      if (_treasureSite.isCaptured()) {
         // Disable the capture bar
         barContainer.SetActive(false);

         // Display the correct ownership icon
         if (_treasureSite.voyageGroupId == Global.player.voyageGroupId) {
            alliedIcon.SetActive(true);
            enemyIcon.SetActive(false);
         } else {
            alliedIcon.SetActive(false);
            enemyIcon.SetActive(true);
         }
      } else {
         // Disable the ownership icons
         alliedIcon.SetActive(false);
         enemyIcon.SetActive(false);

         // Check if the site is being captured
         if (_treasureSite.capturePoints != 0) {
            // Display the capture bar
            barContainer.SetActive(true);

            // Update the capture percentage bar
            captureBarImage.fillAmount = _treasureSite.capturePoints;

            // Set the correct bar color
            if (_treasureSite.voyageGroupId == Global.player.voyageGroupId) {
               captureBarImage.color = alliedCaptureBarColor;
            } else {
               captureBarImage.color = enemyCaptureBarColor;
            }
         } else {
            // Disable the capture bar
            barContainer.SetActive(false);
         }
      }
   }

   #region Private Variables

   // Our associated treasure site
   protected TreasureSite _treasureSite;

   #endregion
}
