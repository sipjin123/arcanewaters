using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ArrowManager : ClientMonoBehaviour {
   #region Public Variables

   // Our Direction Arrows
   public DirectionArrow arrow1;
   public DirectionArrow arrow2;
   public DirectionArrow arrow3;

   #endregion

   void Start () {
      // Lookup components
      _entity = GetComponentInParent<PlayerShipEntity>();

      // We only care about our own ship
      if (!_entity.isLocalPlayer) {
         this.gameObject.SetActive(false);
      }
   }

   private void Update () {
      // If we're not using this movement mode, then don't do anything
      arrow2.gameObject.SetActive(SeaManager.moveMode == SeaManager.MoveMode.Arrows);

      // Check the angle based on our ship's direction
      //float angle = -Util.angle(_entity.getRigidbody().velocity);

      // Pass along the angle to each of our arrows
      arrow1.targetAngle = _entity.desiredAngle;
      arrow2.targetAngle = _entity.desiredAngle;
      arrow3.targetAngle = _entity.desiredAngle;
   }

   #region Private Variables

   // The associated entity
   protected PlayerShipEntity _entity;

   #endregion
}
