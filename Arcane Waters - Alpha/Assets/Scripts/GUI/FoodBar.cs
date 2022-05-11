using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FoodBar : ClientMonoBehaviour
{
   #region Public Variables

   // The fill of food
   public Image fillImage;

   #endregion

   private void Start () {
      _target = GetComponentInParent<NetEntity>();
      if (_target == null) {
         enabled = false;
      }
   }

   private void Update () {
      fillImage.fillAmount = _target.maxFood > 0 ? Mathf.Clamp01(_target.currentFood / _target.maxFood) : 0;
   }

   #region Private Variables

   // Our target entity
   private NetEntity _target;

   #endregion
}
