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

   // Type of attack (cannon, cone, circle) + attack effect (slow, stun etc.)
   public enum CannonAttackOption {
      Standard_NoEffect = 0,
      Standard_Slow = 1,
      Standard_Frozen = 2,
      Cone_NoEffect = 3,
      Circle_NoEffect = 4
   }

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
      _currentAttackOption = CannonAttackOption.Standard_NoEffect;
      cannonBoxList[0].setCannons();
   }

   public void resetAllHighlights () {
      foreach (CannonBox cannonBox in cannonBoxList) {
         cannonBox.highlightSkill.SetActive(false);
      }
   }

   public void cannonReleased () {
      switch (_currentAttackOption) {
         case CannonAttackOption.Standard_NoEffect:
            break;
         case CannonAttackOption.Standard_Slow:
            _attackCooldown[_currentAttackIndex] += 10f;
            break;
         case CannonAttackOption.Standard_Frozen:
            _attackCooldown[_currentAttackIndex] += 20f;
            break;
         case CannonAttackOption.Cone_NoEffect:
            _attackCooldown[_currentAttackIndex] += 10f;
            break;
         case CannonAttackOption.Circle_NoEffect:
            _attackCooldown[_currentAttackIndex] += 20f;
            break;
      }
   }

   public bool isOnCooldown () {
      return _attackCooldown[_currentAttackIndex] > 0.0f;
   }

   public void useCannonType (CannonAttackOption attackType, int index) {
      if (Global.player == null) {
         return;
      }

      PlayerShipEntity ship = (PlayerShipEntity) Global.player;
      if (ship == null) {
         return;
      }

      _currentAttackIndex = index;
      _currentAttackOption = attackType;

      switch (attackType) {
         case CannonAttackOption.Standard_NoEffect:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Normal;
            ship.cannonEffectType = Status.Type.None;
            break;

         case CannonAttackOption.Standard_Slow:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Normal;
            ship.cannonEffectType = Status.Type.Slowed;
            break;

         case CannonAttackOption.Standard_Frozen:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Normal;
            ship.cannonEffectType = Status.Type.Frozen;
            break;

         case CannonAttackOption.Cone_NoEffect:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Cone;
            ship.cannonEffectType = Status.Type.None;
            break;

         case CannonAttackOption.Circle_NoEffect:
            ship.cannonAttackType = PlayerShipEntity.CannonAttackType.Circle;
            ship.cannonEffectType = Status.Type.None;
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
            _boxes[i].cooldownText.gameObject.SetActive(false);
         } else {
            _boxes[i].cooldownText.gameObject.SetActive(true);
            _boxes[i].cooldownText.text = Mathf.FloorToInt(_attackCooldown[i]).ToString();
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
   protected CannonAttackOption _currentAttackOption;

   // Stored current attack index - 0 is the leftmost position in the panel
   protected int _currentAttackIndex;

   #endregion
}
