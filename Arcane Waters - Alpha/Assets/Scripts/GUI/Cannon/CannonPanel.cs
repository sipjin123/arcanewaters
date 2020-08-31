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

   // List of abilities in the cannon panel
   public List<CannonBox> cannonBoxList;

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

   public void resetAllHighlights () {
      foreach (CannonBox cannonBox in cannonBoxList) {
         cannonBox.highlightSkill.SetActive(false);
      }
   }

   public void setAbilityTab (int[] abilities) {
      cannonBoxParent.gameObject.DestroyChildren();
      cannonBoxList = new List<CannonBox>();

      Attack.Type initialAbility = Attack.Type.None;
      int index = 0;
      foreach (int abilityId in abilities) {
         CannonBox abilityTab = Instantiate(cannonBoxPrefab.gameObject, cannonBoxParent).GetComponent<CannonBox>();
         ShipAbilityData shipAbility = ShipAbilityManager.self.getAbility(abilityId);
         if (initialAbility == Attack.Type.None) {
            initialAbility = shipAbility.selectedAttackType;
            abilityTab.setCannons();
         }

         abilityTab.attackType = shipAbility.selectedAttackType;
         abilityTab.abilityId = shipAbility.abilityId;
         abilityTab.skillIcon.sprite = ImageManager.getSprite(shipAbility.skillIconPath);

         // Selects the default ability
         if (index == 0) {
            SeaManager.selectedAbilityId = shipAbility.abilityId;
         }

         cannonBoxList.Add(abilityTab);
         index++;
      }
      resetAllHighlights();
      cannonBoxList[0].setCannons();
   }

   public int getAbilityId (int index) {
      if (index < cannonBoxList.Count) {
         return cannonBoxList[index].abilityId;
      }
      return ShipAbilityInfo.DEFAULT_ABILITY;
   }

   private void Update () {
      // Hide this panel when we don't have a body
      _canvasGroup.alpha = (Global.player == null || !(Global.player is PlayerShipEntity)) ? 0f : 1f;
      _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our cannon boxes
   protected CannonBox[] _boxes;

   #endregion
}
