using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(CircleCollider2D))]
public class PlayerTargetSelector : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Update () {
      // Get our current target from SelectionManager 
      _currentTarget = SelectionManager.self.selectedEntity;

      // If our target has died, select a new target
      if (_currentTarget != null && _currentTarget.isDead()) {
         selectNextTarget();
      }

      // Check if our target changed 
      if (_currentTarget != _lastTarget) {
         updateCurrentTargetIndex();
      }

      // Select the next nearby target using Tab
      if (InputManager.self.inputMaster.LandBattle.NextTarget.WasPressedThisFrame()) {
         selectNextTarget();
      }

      /*
      // Disabling the auto deselecting of enemy ships
      if (_currentTarget != null) {
         float distance = Vector2.Distance(transform.position, _currentTarget.transform.position);

         // If our target is out of our area, start the timer
         if (distance > ESCAPE_DISTANCE) {
            _selectedTargetTimeOutOfArea += Time.deltaTime;

            // If the target has been out of our area for longer than ESCAPE_TIME, unselect it
            if (_selectedTargetTimeOutOfArea > ESCAPE_TIME) {
               SelectionManager.self.setSelectedEntity(null);

               // Notify the player their target has escaped
               showTargetEscaped();
            }
         } else {
            _selectedTargetTimeOutOfArea = 0.0f;
         }
      }
      */

      // Keep track of our current target in case it changes
      _lastTarget = _currentTarget;
   }

   private void showTargetEscaped () {
      GameObject text = Instantiate(PrefabsManager.self.battleTextPrefab, transform.position, Quaternion.identity);
      text.transform.SetParent(transform);
      text.GetComponentInChildren<BattleText>().setCustomText(TARGET_ESCAPED_TEXT, Color.red);
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      // If a ship entered our area and it's our enemy, add it to the list
      SeaEntity entity = collision.GetComponentInChildren<SeaEntity>();
      if (entity != null && Global.player != null) {
         if (Global.player.isEnemyOf(entity)) {
            _nearbyShips.Add(entity);
         }
      }
   }

   private void OnTriggerExit2D (Collider2D collision) {
      SeaEntity entity = collision.GetComponentInChildren<SeaEntity>();
      if (entity != null) {
         int index = _nearbyShips.IndexOf(entity);

         // If a ship exited our area and it was on the list, remove it
         if (index >= 0) {
            _nearbyShips.RemoveAt(index);

            // Since we removed a ship from our list, the index of our currently selected ship can change
            updateCurrentTargetIndex();
         }
      }
   }

   public SeaEntity getTarget () {
      return _currentTarget;
   }

   protected void updateCurrentTargetIndex () {
      int index = _nearbyShips.IndexOf(_currentTarget);

      // Only update the index if the currently selected ship is in our area
      if(index > -1) {
         _currentTargetIndex = index;
      }
   }

   protected void selectNextTarget () {
      // Do nothing if there are no nearby ships
      if (_nearbyShips.Count < 1) {
         return;
      }

      _currentTargetIndex++;

      if (_currentTargetIndex >= _nearbyShips.Count) {
         _currentTargetIndex = 0;
      }

      // Set the new target
      _currentTarget = _nearbyShips[_currentTargetIndex];
      SelectionManager.self.setSelectedEntity(_currentTarget);
   }

   #region Private Variables

   // The target currently selected
   protected SeaEntity _currentTarget = null;

   // The target in the previous frame, used for comparing changes
   protected SeaEntity _lastTarget = null;

   // A list containing all the ships in range
   protected List<SeaEntity> _nearbyShips = new List<SeaEntity>();

   // How much time our current time has been outside of our area
   protected float _selectedTargetTimeOutOfArea = 0.0f;

   // The index of the currently selected ship
   protected int _currentTargetIndex = 0;

   // The distance for a target to escape
   protected const float ESCAPE_DISTANCE = 7.5f;

   // The time for a ship to be considered out of our area
   protected const float ESCAPE_TIME = 2.0f;

   // The text shown when a target has escaped
   protected const string TARGET_ESCAPED_TEXT = "Target escaped";

   #endregion
}
