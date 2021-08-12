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

      _currentAttackIndex = 0;
      _currentAttackOption = Attack.Type.Standard_NoEffect;
      cannonBoxList[0].setCannons();

      _cooldownPerSkill.Add(ShipAbilityManager.SHIP_ABILITY_DEFAULT[1], 10);
      _cooldownPerSkill.Add(ShipAbilityManager.SHIP_ABILITY_DEFAULT[2], 20);
      _cooldownPerSkill.Add(ShipAbilityManager.SHIP_ABILITY_DEFAULT[3], 10);
      _cooldownPerSkill.Add(ShipAbilityManager.SHIP_ABILITY_DEFAULT[4], 20);
   }

   public void overwriteShipCooldowns () {
      _cooldownPerSkill = new Dictionary<int, int>();

      if (cannonBoxList.Count > 0) {
         foreach (CannonBox cannonBox in cannonBoxList) {
            ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(cannonBox.abilityId);
            if (shipAbilityData != null) {
               if (!_cooldownPerSkill.ContainsKey(shipAbilityData.abilityId)) {
                  _cooldownPerSkill.Add(shipAbilityData.abilityId, (int) shipAbilityData.coolDown);
               }
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

   public void cannonReleased () {
      if (_cooldownPerSkill.ContainsKey(_currentAttackAbility)) {
         _attackCooldown[_currentAttackIndex] += _cooldownPerSkill[_currentAttackAbility];
         _isChargingCannon = false;
      }
   }

   public bool isOnCooldown () {
      return _attackCooldown[_currentAttackIndex] > 0.0f;
   }

   public void setIsChargingCannon (bool isChargingCannon) {
      _isChargingCannon = isChargingCannon;
   }

   public void useCannonType (int abilityId, int index) {
      if (Global.player == null || _isChargingCannon) {
         return;
      }

      PlayerShipEntity ship = (PlayerShipEntity) Global.player;
      if (ship == null) {
         return;
      }

      ShipAbilityData shipAbility = ShipAbilityManager.self.getAbility(abilityId);
      if (shipAbility == null) {
         D.debug("No ability with id: {" + abilityId + "}");
         return;
      }

      D.adminLog("Using cannon type: ID: " 
         + abilityId + " INDEX: " 
         + index + " Name: " 
         + shipAbility.abilityName + " Attack: " 
         + shipAbility.selectedAttackType, D.ADMIN_LOG_TYPE.SeaAbility);

      _currentAttackIndex = index;
      _currentAttackOption = shipAbility.selectedAttackType;
      _currentAttackAbility = shipAbility.abilityId;

      ship.cannonEffectType = (Status.Type) shipAbility.statusType;
      switch (shipAbility.selectedAttackType) {
         case Attack.Type.Standard_NoEffect:
         case Attack.Type.Standard_Slow:
         case Attack.Type.Standard_Stunned:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Normal;
            break;

         case Attack.Type.Cone_NoEffect:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Cone;
            break;

         case Attack.Type.Circle_NoEffect:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Circle;
            break;

         default:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Normal;
            break;
      }

      ship.Cmd_RequestCannonEffectType((int)ship.cannonEffectType);
      resetAllHighlights();
      cannonBoxList[index].setCannons();
   }

   private void Update () {
      // Hide this panel when we don't have a body
      _canvasGroup.alpha = (Global.player == null || !(Global.player is PlayerShipEntity)) ? 0f : 1f;
      _canvasGroup.blocksRaycasts = _canvasGroup.alpha > 0f;

      for (int i = 0; i < _attackCooldown.Length; i++) {
         if (_attackCooldown[i] == 0.0f) {
            continue;
         }

         _attackCooldown[i] -= Time.deltaTime;

         if (_attackCooldown[i] <= 0.0f) {
            _attackCooldown[i] = 0.0f;
         } else {
            _boxes[i].setCooldown(_attackCooldown[i] / (float) _cooldownPerSkill[cannonBoxList[i].abilityId]);
         }
      }
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our cannon boxes
   protected CannonBox[] _boxes;

   // Cooldown for each skill available in the panel
   protected float[] _attackCooldown = new float[5];

   // Stored current attack option
   protected Attack.Type _currentAttackOption;

   // Stored current attack ability
   protected int _currentAttackAbility; 

   // Stored current attack index - 0 is the leftmost position in the panel
   protected int _currentAttackIndex;

   // Cooldown value to set after using skill
   protected Dictionary<int, int> _cooldownPerSkill = new Dictionary<int, int>();

   // Check whether player ship is currently charging cannon
   protected bool _isChargingCannon = false;

   #endregion
}
