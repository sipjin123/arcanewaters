using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InteractableBall : InteractableObjEntity {
   #region Public Variables

   // Range of distance that projectile will move from initial position
   public float distanceMin;
   public float distanceMax;

   // The interactable object
   public GameObject spriteObj;

   // The object spawned to mark the destination of the obj
   public GameObject destinationIndicatorPrefab;

   // The cached indicator spawned
   public GameObject cachedDestinationIndicator;

   // Standard size of tile in world
   public const float TILE_SIZE = 0.166666f;

   #endregion

   protected override void Awake () {
      base.Awake();
      defaultShadowPosY = 0.1f;
   }

   [ClientRpc]
   public void Rpc_BroadInit (Vector2 startPos, Vector2 dir, double startTime, float archHeight, float lifeTime, float dist, float totalRotation) {
      init(startPos, dir, startTime, archHeight, lifeTime, dist, totalRotation);
   }

   public void init (Vector2 startPos, Vector2 dir, double startTime, float archHeight, float lifeTime, float dist, float totalRotation) {
      _startTime = startTime;
      _archHeight = archHeight;
      _lifeTime = lifeTime;
      _distance = dist;

      // Use one or two full rotation (360 degree) when object is in the air
      _totalRotation = totalRotation;

      transform.position = startPos;
      _startPos = startPos;
      _endPos = _startPos + dir * _distance;

      GameObject destinationMark = Instantiate(destinationIndicatorPrefab);
      destinationMark.transform.position = _endPos;
      destinationMark.SetActive(true);
      cachedDestinationIndicator = destinationMark;
      simulatePhysics = true;
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();
      if (!simulatePhysics) {
         if (usesNetworkRigidBody) {
            double timeAlive = NetworkTime.time - _startTime;
            if ((NetworkServer.active && isSimulatingRigidBody) || (!NetworkServer.active && _archHeight> 0 && timeAlive < _lifeTime)) {
               float lerpTime = (float) (timeAlive / _lifeTime);
               float angleInDegrees = lerpTime * 180f;
               float objHeight = Util.getSinOfAngle(angleInDegrees) * _archHeight;
               _totalRotation = 360 * 2;

               Vector3 rot = spriteObj.transform.localRotation.eulerAngles;
               float rotLerp = -Mathf.Log(lerpTime + 0.1f) + 1.0f;
               spriteObj.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(rot.x, rot.y, _totalRotation * rotLerp));
               float newHeightOffset = transform.position.y + objHeight;
               if (newHeightOffset < transform.position.y) {
                  newHeightOffset = transform.position.y;
               }
               spriteObj.transform.position = new Vector3(transform.position.x, newHeightOffset, transform.position.z);
            } else {
               spriteObj.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            }
         }
      } else {
         double timeAlive = NetworkTime.time - _startTime;
         float lerpTime = (float) (timeAlive / _lifeTime);

         float angleInDegrees = lerpTime * 180f;
         float objHeight = Util.getSinOfAngle(angleInDegrees) * _archHeight;

         Vector3 rot = spriteObj.transform.localRotation.eulerAngles;
         float rotLerp = -Mathf.Log(lerpTime + 0.1f) + 1.0f;
         spriteObj.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(rot.x, rot.y, _totalRotation * rotLerp));
         Util.setXY(this.transform, Vector2.Lerp(_startPos, _endPos, lerpTime));
         Util.setLocalY(spriteObj.transform, objHeight);

         if (timeAlive > _lifeTime) {
            processDestruction();
         }
      }
   }

   protected void processDestruction () {
      if (cachedDestinationIndicator != null) {
         Destroy(cachedDestinationIndicator);
         cachedDestinationIndicator = null;
      }
      simulatePhysics = false;
   }

   #region Private Variables

   #endregion
}
