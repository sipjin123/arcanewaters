using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FullScreenSeparatePanel : MonoBehaviour {

   // Associated Canvas Groups
   public List<CanvasGroup> allCanvasGroups = new List<CanvasGroup>();

   virtual protected void Start() {
      allCanvasGroups = new List<CanvasGroup>(GetComponentsInChildren<CanvasGroup>(true));
      allCanvasGroups.Add(GetComponent<CanvasGroup>());
   }
}
