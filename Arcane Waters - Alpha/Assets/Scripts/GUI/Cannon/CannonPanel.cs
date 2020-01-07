using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CannonPanel : ClientMonoBehaviour {
   #region Public Variables

   // Self
   public static CannonPanel self;

   // Prefab for UI selection
   public CannonBox cannonBoxPrefab;

   // Template holder
   public Transform cannonBoxParent;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   private void Start () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _boxes = GetComponentsInChildren<CannonBox>();
   }

   public void setAbilityTab (string[] abilities) {
      cannonBoxParent.gameObject.DestroyChildren();

      Attack.Type initialAbility = Attack.Type.None;
      foreach (string abilityName in abilities) {
         CannonBox abilityTab = Instantiate(cannonBoxPrefab.gameObject, cannonBoxParent).GetComponent<CannonBox>();
         ShipAbilityData shipAbility = ShipAbilityManager.self.getAbility(abilityName);
         if (initialAbility == Attack.Type.None) {
            initialAbility = shipAbility.selectedAttackType;
            abilityTab.setCannons();
         }

         abilityTab.attackType = shipAbility.selectedAttackType;
         abilityTab.skillIcon.sprite = ImageManager.getSprite(shipAbility.skillIconPath);
      }
   }

   private void Update () {
      // Hide this panel when we don't have a body
      _canvasGroup.alpha = (Global.player is PlayerShipEntity) ? 1f : 0f;
      _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our cannon boxes
   protected CannonBox[] _boxes;

   #endregion
}
