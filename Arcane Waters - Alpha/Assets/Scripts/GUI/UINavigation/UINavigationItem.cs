using System;
using UnityEngine;

public class UINavigationItem : MonoBehaviour {
   #region Public Variables
   public  GameObject selected;
   #endregion

   private void Awake () {
      if (selected == null) {
         transform.Find("navItemSelected");
      }
   }

   public void Select () {
      selected?.SetActive(true);
   }

   public void Deselect () {
      selected?.SetActive(false);
   }

   public void Equip () {
   }

   public void Use () {
   }

   public void Interact () {
   }
   
   #region Private Variables
   #endregion
}
