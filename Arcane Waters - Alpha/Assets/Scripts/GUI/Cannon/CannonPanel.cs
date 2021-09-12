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

   // The max ability count of player ships
   public const int MAX_ABILITY_COUNT = 5;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   private void Start () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _boxes = GetComponentsInChildren<CannonBox>();

      _currentAttackIndex = 0;
      _currentAttackOption = Attack.Type.Standard_NoEffect;
      cannonBoxList[0].setCannons();

      _abilityCooldownDurations[ShipAbilityManager.SHIP_ABILITY_DEFAULT[1]] = 10;
      _abilityCooldownDurations[ShipAbilityManager.SHIP_ABILITY_DEFAULT[2]] = 20;
      _abilityCooldownDurations[ShipAbilityManager.SHIP_ABILITY_DEFAULT[3]] = 10;
      _abilityCooldownDurations[ShipAbilityManager.SHIP_ABILITY_DEFAULT[4]] = 20;
   }

   public void updateCooldownDurations () {
      _abilityCooldownDurations.Clear();

      if (cannonBoxList.Count > 0) {
         foreach (CannonBox cannonBox in cannonBoxList) {
            ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(cannonBox.abilityId);
            if (shipAbilityData != null) {
               _abilityCooldownDurations[shipAbilityData.abilityId] = shipAbilityData.coolDown;
            } else {
               cannonBox.setAbilityIcon(-1);
               D.debug("Cannot process the cooldown of ability {" + shipAbilityData.abilityId + "}, missing data.");
            }
         }

         cannonBoxList[0].setCannons();
      }
   }

   public void setAbilityIcon (int index, int abilityId) {
      cannonBoxList[index].setAbilityIcon(abilityId);
   }

   public void resetAllHighlights () {
      foreach (CannonBox cannonBox in cannonBoxList) {
         cannonBox.highlightSkill.SetActive(false);
      }
   }

   private void Update () {
      // Hide this panel when we don't have a body
      _canvasGroup.alpha = (Global.player == null || !(Global.player is PlayerShipEntity)) ? 0f : 1f;
      _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0f;

      for (int i = 0; i < _currentAbilityCooldowns.Count; i++) {
         if (_currentAbilityCooldowns[i] == 0.0f) {
            continue;
         }

         _currentAbilityCooldowns[i] -= Time.deltaTime;

         if (_currentAbilityCooldowns[i] <= 0.0f) {
            _currentAbilityCooldowns[i] = 0.0f;
         }

         if (_abilityCooldownDurations.ContainsKey(cannonBoxList[i].abilityId)) {
            _boxes[i].setCooldown(_currentAbilityCooldowns[i] / (float) _abilityCooldownDurations[cannonBoxList[i].abilityId]);
         }
      }
   }

   public void abilityUsed (int abilityIndex) {
      int abilityId = cannonBoxList[abilityIndex].abilityId;
      
      if (_abilityCooldownDurations.ContainsKey(abilityId)) {
         _currentAbilityCooldowns[abilityIndex] += _abilityCooldownDurations[abilityId];
      }
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our cannon boxes
   protected CannonBox[] _boxes;

   // Stored current attack option
   protected Attack.Type _currentAttackOption;

   // Stored current attack ability
   protected int _currentAttackAbility; 

   // Stored current attack index - 0 is the leftmost position in the panel
   protected int _currentAttackIndex;

   // The current cooldown for each of the player's abilities (client-side, only used to update UI icons)
   private List<float> _currentAbilityCooldowns = new List<float>() { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

   // How long a cooldown each ability will have after casting it
   private Dictionary<int, float> _abilityCooldownDurations = new Dictionary<int, float>();

   #endregion
}
