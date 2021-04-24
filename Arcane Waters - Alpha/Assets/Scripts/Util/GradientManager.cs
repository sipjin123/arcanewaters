using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GradientManager : MonoBehaviour {
   #region Public Variables

   public static GradientManager self;

   // A gradient controlling the color of the boost cooldown bar outline
   public Gradient shipBoostCooldownBarOutlineColor;

   // A gradient controlling the color of the boost cooldown bar
   public Gradient shipBoostCooldownBarColor;

   // A gradient controlling the color of the boost fill circle
   public Gradient shipBoostCircleColor;

   // An animation curve controlling the alpha of the ship's wake
   public AnimationCurve shipBoostWakeAlpha;

   #endregion

   private void Awake () {
      self = this;
   }

   #region Private Variables

   #endregion
}
