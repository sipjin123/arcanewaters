using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class ShipJourneyPanel : ClientMonoBehaviour
{
   #region Public Variables

   // Self
   public static ShipJourneyPanel self;

   // The canvas group component
   public CanvasGroup canvasGroup;

   // Text for displaying journey type
   public TMP_Text journeyLengthText;

   // Progress bar elements
   public RectTransform progressFillContainer;
   public RectTransform progressFill;

   #endregion

   private void Update () {
      // Only enable at sea in open world
      if (Global.player == null || !Global.player.isPlayerShip() || !WorldMapManager.isWorldMapArea(Global.player.areaKey)) {
         hide();
         return;
      }

      show();

      if (Global.player.worldAreaVisitStreak == _lastStreak) {
         return;
      }
      _lastStreak = Global.player.worldAreaVisitStreak;

      setProgressBar(Mathf.Clamp01(Global.player.worldAreaVisitStreak * 0.1f));

      if (Global.player.worldAreaVisitStreak < 2) {
         journeyLengthText.text = "OUTING";
      } else if (Global.player.worldAreaVisitStreak < 4) {
         journeyLengthText.text = "SHORT TRIP";
      } else if (Global.player.worldAreaVisitStreak < 8) {
         journeyLengthText.text = "EXPEDITION";
      } else {
         journeyLengthText.text = "LIFE AT SEA";
      }
   }

   private void setProgressBar (float val) {
      progressFill.sizeDelta = new Vector2(progressFillContainer.sizeDelta.x * val, progressFill.sizeDelta.y);
   }

   private void hide () {
      if (canvasGroup.IsShowing()) {
         _lastStreak = 0;
         canvasGroup.alpha = 0;
      }
   }

   private void show () {
      if (!canvasGroup.IsShowing()) {
         canvasGroup.alpha = 1;
      }
   }

   #region Private Variables

   // Last streak we checked
   private int _lastStreak = 0;

   #endregion
}
