using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SoonGif_RunAlongPath : MonoBehaviour
{
   #region Public Variables

   // Parent of the path we run along
   public Transform pathParent;

   // The entity we control
   public NetEntity targetEntity;

   // The speed at which the entity runs
   public float fastRunSpeed = 6f;
   public float slowRunSpeed = 1f;

   #endregion


   private void FixedUpdate () {
      if (targetEntity == null || pathParent == null) {
         return;
      }

      Vector2 pos = pathParent.GetChild(_currentPathIndex).position;

      if (pos == targetEntity.getRigidbody().position) {
         _currentPathIndex++;
         if (_currentPathIndex >= pathParent.childCount) {
            _currentPathIndex = 0;
         }
      }

      float runSpeed = KeyUtils.GetKey(UnityEngine.InputSystem.Key.Numpad9) ? fastRunSpeed : slowRunSpeed;

      targetEntity.getRigidbody().MovePosition(Vector2.MoveTowards(
         targetEntity.getRigidbody().position,
         pos,
         runSpeed * Time.deltaTime));

      Vector2 moveVector = (pos - targetEntity.getRigidbody().position).normalized;
      float minDist = float.MaxValue;
      foreach (var entry in _moveDirs) {
         if (Vector2.SqrMagnitude(entry.vec - moveVector) < minDist) {
            minDist = Vector2.SqrMagnitude(entry.vec - moveVector);
            targetEntity.facing = entry.dir;
         }
      }
      targetEntity.getMainCollider().isTrigger = true;
      targetEntity.getRigidbody().velocity = moveVector * runSpeed;
   }

   #region Private Variables

   // Current path point we target
   private int _currentPathIndex = 0;

   // Possible directions we can move in, corresponding to Direction enum
   private (Direction dir, Vector2 vec)[] _moveDirs = new (Direction dir, Vector2 vec)[] {
      (Direction.North, new Vector2(0, 1f)),
      (Direction.East, new Vector2(1, 0f)),
      (Direction.West, new Vector2(-1, 0)),
      (Direction.South, new Vector2(0, -1)),
      (Direction.NorthEast, new Vector2(0.7071f, 0.7071f)),
      (Direction.NorthWest, new Vector2(-0.7071f, 0.7071f)),
      (Direction.SouthEast, new Vector2(0.7071f, -0.7071f)),
      (Direction.SouthWest, new Vector2(-0.7071f, -0.7071f))
   };

   #endregion
}
