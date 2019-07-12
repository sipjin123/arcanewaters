using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShortcutPanel : ClientMonoBehaviour {
   #region Public Variables

   // Self
   public static ShortcutPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   private void Start () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _boxes = GetComponentsInChildren<ShortcutBox>();
   }

   private void Update () {
      // Hide this panel when we don't have a body
      _canvasGroup.alpha = (Global.player is PlayerBodyEntity && !Global.isInBattle()) ? 1f : 0f;
      _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0f;

      // Hide any shortcut boxes that aren't relevant yet
      foreach (ShortcutBox box in _boxes) {
         box.gameObject.SetActive((int)TutorialManager.currentStep >= (int)box.requiredTutorialStep);
      }
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our shortcut boxes
   protected ShortcutBox[] _boxes;

   #endregion
}
