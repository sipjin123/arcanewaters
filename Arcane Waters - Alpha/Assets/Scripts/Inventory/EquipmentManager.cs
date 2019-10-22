using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using Mirror;

public class EquipmentManager : NetworkBehaviour {
   #region Public Variables

   // The Layers we're interested in
   public BodyLayer bodyLayer;

   // The Sprites we're interested in
   public SpriteRenderer bodySprite;

   #endregion

   public virtual void Awake () {
      _body = GetComponent<BodyEntity>();
      _battler = GetComponent<BattlerBehaviour>();
   }

   protected Gender.Type getGender () {
      if (_battler != null) {
         return _battler.gender;
      }

      return _body.gender;
   }

   #region Private Variables

   // The Entity we're associated with, if any
   protected BodyEntity _body;

   // The Battler we're associated with, if any
   protected BattlerBehaviour _battler;

   #endregion
}
