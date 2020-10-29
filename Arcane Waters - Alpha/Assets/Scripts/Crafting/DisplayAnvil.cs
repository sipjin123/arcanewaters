using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DisplayAnvil : MonoBehaviour {
   #region Public Variables

   // Reference to the anvil that can be interacted
   public GameObject functionalCraftingAnvil;

   // Reference to the sprite renderer
   public SpriteRenderer spriteRenderer;

   #endregion

   public void loadFunctionalAnvil () {
      spriteRenderer.enabled = false;
      functionalCraftingAnvil.SetActive(true);
   }

   #region Private Variables
      
   #endregion
}
