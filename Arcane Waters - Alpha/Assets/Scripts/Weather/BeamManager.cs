using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BeamManager : MonoBehaviour {
   #region Public Variables

   #endregion

   void Start () {
      _layers = new List<BeamLayer>(GetComponentsInChildren<BeamLayer>());
   }

   void Update () {
      foreach (BeamLayer layer in _layers) {
         layer.gameObject.SetActive(shouldThisShow());
      }
   }

   protected bool shouldThisShow () {
      return !ClientScene.ready & !Global.isRedirecting;
   }

   #region Private Variables

   // The layers we manage
   protected List<BeamLayer> _layers;

   #endregion
}
