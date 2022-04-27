using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

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

      if (Global.player.currentHealth == _lastHealth && Global.player.maxHealth == _lastMaxHealth) {
         return;
      }

      float hpPerSailor = getHpPerSailor();

      float hpStep = 0;
      for (int i = 0; i < _sailors.Count; i++) {
         if ((hpStep + (hpPerSailor / 2)) < Global.player.currentHealth) {
            _sailors[i].setStatus(SailorHP.Status.Healthy);
         } else if (hpStep < Global.player.currentHealth) {
            _sailors[i].setStatus(SailorHP.Status.Damaged);
         } else if (hpStep < Global.player.maxHealth) {
            _sailors[i].setStatus(SailorHP.Status.KnockedOut);
         } else {
            _sailors[i].setStatus(SailorHP.Status.Hidden);
         }
         hpStep += hpPerSailor;
      }

      // If current health is 100% or greater, make sure all sailor icons are showing full health
      if (Global.player.currentHealth >= Global.player.maxHealth) {
         foreach (SailorHP sailorIcon in _sailors.Where(s => s.isActiveAndEnabled)) {
            sailorIcon.setStatus(SailorHP.Status.Healthy);
         }
      }

      if (Global.player.currentHealth > 0 && Global.player.currentHealth < _lastHealth) {
         int blinkLoopCount;
         float sailorLeftCount = (float) Global.player.currentHealth / hpPerSailor;

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
      _lastMaxHealth = Global.player.maxHealth;

      PowerupPanel.self.rebuildLayoutGroup();
   }

   private void hide () {
      if (canvasGroup.IsShowing()) {
         _lastHealth = 0;
         canvasGroup.alpha = 0;

         // Deactivate all sailors so the layout group can shrink
         for (int i = 0; i < _sailors.Count; i++) {
            _sailors[i].setStatus(SailorHP.Status.Hidden);
         }
      }
   }

   private void show() {
      if (!canvasGroup.IsShowing()) {
         canvasGroup.alpha = 1;
      }
   }

   private float getHpPerSailor () {
      
      // If max health is small enough to be shown on screen with default HP_PER_SAILOR, don't change it
      if (Global.player.maxHealth <= MAX_SAILORS * HP_PER_SAILOR) {
         return HP_PER_SAILOR;
      
      // If max health is too large to be shown on screen with default HP_PER_SAILOR, we will scale HP_PER_SAILOR to fit
      } else {
         return (float)Global.player.maxHealth / MAX_SAILORS;
      }
   }

   #region Private Variables

   // The list of all sailor objects
   private List<SailorHP> _sailors = new List<SailorHP>();

   // The last registered ship health value
   private float _lastHealth = 0, _lastMaxHealth;

   #endregion
}
