using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEditor;

public class MaterialInstancer : Editor {

   [MenuItem("GameObject/Instance Material")]
   public static void Instance () {
      if (Selection.activeGameObject == null) {
         Debug.LogError("No Valid Object Selected");
         return;
      }

      Image image = Selection.activeGameObject.GetComponent<Image>();
      if (image != null) {
         image.material = Instantiate(image.material);
      } else {
         Material mat = Selection.activeGameObject.GetComponent<SpriteRenderer>().sharedMaterial;
         Selection.activeGameObject.GetComponent<SpriteRenderer>().sharedMaterial = new Material(mat);
      }
   }

}
