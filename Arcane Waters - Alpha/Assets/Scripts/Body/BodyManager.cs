using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class BodyManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static BodyManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public BodyEntity getBody (int userId) {
      if (_bodies.ContainsKey(userId)) {
         return _bodies[userId];
      }

      return null;
   }

   public BodyEntity getBodyWithName (string userName) {
      BodyEntity newBody = _bodies.Values.ToList().Find(_ => _.entityName == userName);
      if (newBody == null) {
         D.warning("No body with username exists in the collection: " + userName);
         return null;
      }
      return newBody;
   }

   public void storeBody (BodyEntity body) {
      _bodies[body.userId] = body;
   }

   #region Private Variables

   // A mapping of userId to Body object
   protected Dictionary<int, BodyEntity> _bodies = new Dictionary<int, BodyEntity>();

   #endregion
}
