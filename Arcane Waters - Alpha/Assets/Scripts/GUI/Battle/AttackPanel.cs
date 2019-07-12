using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackPanel : MonoBehaviour {
   #region Public Variables

   // Our Container object
   public GameObject container;

   #endregion

   private void Start () {
      // Look up components
      _canvasGroup = GetComponent<CanvasGroup>();
      _button = GetComponentInChildren<Button>();
   }

   private void Update () {
      Battler target = BattleSelectionManager.self.selectedBattler;

      // Only show the attack panel when we have a battler selected
      _canvasGroup.alpha = target is MonsterBattler ? 1f : 0f;

      if (target == null) {
         return;
      }

      // Hide the Canvas while our ability is on cooldown
      container.SetActive(Util.netTime() > getBattler().cooldownEndTime);

      // Place it on top of the selected battler
      this.transform.position = new Vector3(
         target.battleSpot.transform.position.x + .5f,
         target.battleSpot.transform.position.y,
         target.transform.position.z - 5f
      );
   }

   public void requestAttackTarget () {
      Battler target = BattleSelectionManager.self.selectedBattler;

      // We have to have a target to attack
      if (target == null) {
         return;
      }

      // Send the request to the server
      Global.player.rpc.Cmd_RequestAttack(target.netId, Ability.Type.Basic_Attack);
   }

   protected Battler getBattler () {
      // If we're not in a battle, there isn't one
      if (!Global.isInBattle()) {
         return null;
      }

      // If we already have it, we're done
      if (_battler != null && _battler.connectionToServer != null) {
         return _battler;
      }

      // Find our Battler and keep track of it
      foreach (Battler battler in FindObjectsOfType<Battler>()) {
         if (battler.player == Global.player) {
            _battler = battler;
         }
      }

      return _battler;
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // Our Button
   protected Button _button;

   // The player's Battler
   protected Battler _battler;
      
   #endregion
}
