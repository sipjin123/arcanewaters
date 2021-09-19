using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class BuffIcon : MonoBehaviour {
   #region Public Variables

   // Icon of the buff 
   public Image buffIcon;

   // The status sprite list
   public List<StatusSpritePair> statusSpritePair = new List<StatusSpritePair>();

   // The current status type
   public Status.Type statusType;

   // Reference to the simple anim component
   public SimpleAnimation simpleAnim;

   #endregion

   #region Private Variables

   #endregion
}

[Serializable]
public class StatusSpritePair {
   // Type of status
   public Status.Type statusType;
 
   // Sprite reference of the status
   public Sprite statusSprite;
}