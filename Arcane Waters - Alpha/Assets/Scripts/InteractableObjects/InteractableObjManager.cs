using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InteractableObjManager : MonoBehaviour {
   #region Public Variables

   // The instance of the object
   public static InteractableObjManager self;

   // Object id generated
   public int objectIdIndex = 0;

   // The interactable ball spawned by server
   public InteractableObjEntity interactableBall, interactableBallNetwork;

   // The interactable box spawned by server
   public InteractableObjEntity interactableBox;

   #endregion

   private void Awake () {
      self = this;
   }

   public InteractableObjEntity getObject (int id) {
      if (_interactableObjects.ContainsKey(id)) {
         return _interactableObjects[id];
      }

      return null;
   }

   public void registerObject (InteractableObjEntity entity) {
      objectIdIndex++;
      entity.objectId = objectIdIndex;
      _interactableObjects.Add(objectIdIndex, entity);
   }

   #region Private Variables

   // Collection of interactable objects
   private Dictionary<int, InteractableObjEntity> _interactableObjects = new Dictionary<int, InteractableObjEntity>();

   #endregion
}
