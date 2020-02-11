using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

[Serializable]
public class BattleSpot : MonoBehaviour {
   #region Public Variables

   // The board position of this spot
   public int boardPosition;

   // The Team associated with this battle spot
   public Battle.TeamType teamType;

   #endregion

   void Start () {
      _sprite = GetComponent<SpriteRenderer>();

      // Disable the sprite, it's only for debugging in the editor
      _sprite.enabled = false;
   }

   #region Private Variables

   // The Sprite Renderer used for this spot in the editor
   protected SpriteRenderer _sprite;

   #endregion
}
