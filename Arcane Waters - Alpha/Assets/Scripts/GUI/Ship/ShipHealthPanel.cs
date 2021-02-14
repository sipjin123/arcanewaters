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
   public static int MAX_SAILORS = 20;

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

      int hpStep = 0;
      for (int i = 0; i < _sailors.Count; i++) {
         if ((hpStep + (HP_PER_SAILOR / 2)) < Global.player.currentHealth) {
            _sailors[i].setStatus(SailorHP.Status.Healthy);
         } else if (hpStep < Global.player.currentHealth) {
            _sailors[i].setStatus(SailorHP.Status.Damaged);
         } else if (hpStep <= Global.player.maxHealth) {
            _sailors[i].setStatus(SailorHP.Status.KnockedOut);
         } else {
            _sailors[i].setStatus(SailorHP.Status.Hidden);
         }
         hpStep += HP_PER_SAILOR;
      }
   }

   private void hide () {
      if (canvasGroup.IsShowing()) {
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

   #endregion
}
