using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mirror;

public class EquipmentManager : NetworkBehaviour
{
   #region Public Variables

   // The Layers we're interested in
   public BodyLayer bodyLayer;

   // The Sprites we're interested in
   public SpriteRenderer bodySprite;

   #endregion

   public virtual void Awake () {
      _body = GetComponent<BodyEntity>();
      _battler = GetComponent<Battler>();
   }

   protected Gender.Type getGender () {
      if (_battler != null) {
         return _battler.gender;
      }

      return _body.gender;
   }

   protected bool tryGetConnectionToClient (out NetworkConnection connection) {
      connection = null;

      if (_body != null) {
         connection = _body.connectionToClient;
      }

      if (_battler != null && _battler.player != null) {
         connection = _battler.player.connectionToClient;
      }

      if (connection == null) {
         return false;
      }

      return true;
   }

   #region Private Variables

   // The Entity we're associated with, if any
   protected BodyEntity _body;

   // The Battler we're associated with, if any
   protected Battler _battler;

   #endregion
}
