using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipHealthPanel : ClientMonoBehaviour
{
   #region Public Variables

   // The amount of health represented by one sailor
   public static int HP_PER_SAILOR = 100;

   // The maximum number of sailors that will be displayed
   public static int MAX_SAILORS = 18;

   // The prefab we use for creating sailors
   public SailorHP sailorPrefab;

   // The container of sailor objects
   public GameObject sailorContainer;

   // The canvas group component
   public CanvasGroup canvasGroup;

   // Self
   public static ShipHealthPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Start () {
      sailorContainer.DestroyChildren();

      for (int i = 0; i < MAX_SAILORS; i++) {
         SailorHP sailor = Instantiate(sailorPrefab, sailorContainer.transform, false);
         sailor.deactivate();
         _sailors.Add(sailor);
      }
   }

   private void Update () {
      // Only enable at sea
      if (Global.player == null || !Global.player.isPlayerShip()) {
         hide();
         return;
      }

      show();

      if (Global.player.currentHealth == _lastHealth) {
         return;
      }

      int hpStep = 0;
      for (int i = 0; i < _sailors.Count; i++) {
         if ((hpStep + (HP_PER_SAILOR / 2)) < Global.player.currentHealth) {
            _sailors[i].setStatus(SailorHP.Status.Healthy);
         } else if (hpStep < Global.player.currentHealth) {
            _sailors[i].setStatus(SailorHP.Status.Damaged);
         } else if (hpStep < Global.player.maxHealth) {
            _sailors[i].setStatus(SailorHP.Status.KnockedOut);
         } else {
            _sailors[i].setStatus(SailorHP.Status.Hidden);
         }
         hpStep += HP_PER_SAILOR;
      }

      if (Global.player.currentHealth > 0 && Global.player.currentHealth < _lastHealth) {
         int blinkLoopCount;
         float sailorLeftCount = (float) Global.player.currentHealth / HP_PER_SAILOR;

         // The closer the death, the more intense the red blinking
         if (sailorLeftCount <= 1) {
            blinkLoopCount = 4;
         } else if (sailorLeftCount <= 2) {
            blinkLoopCount = 3;
         } else if (sailorLeftCount <= 3) {
            blinkLoopCount = 2;
         } else if (sailorLeftCount <= 4) {
            blinkLoopCount = 1;
         } else {
            blinkLoopCount = 0;
         }

         for (int i = 0; i < _sailors.Count; i++) {
            _sailors[i].blink(blinkLoopCount);
         }
      }

      _lastHealth = Global.player.currentHealth;

      PowerupPanel.self.rebuildLayoutGroup();
   }

   private void hide () {
      if (canvasGroup.IsShowing()) {
         _lastHealth = 0;
         canvasGroup.alpha = 0;
      }
   }

   private void show() {
      if (!canvasGroup.IsShowing()) {
         canvasGroup.alpha = 1;
      }
   }

   #region Private Variables

   // The list of all sailor objects
   private List<SailorHP> _sailors = new List<SailorHP>();

   // The last registered ship health value
   private float _lastHealth = 0;

   #endregion
}
