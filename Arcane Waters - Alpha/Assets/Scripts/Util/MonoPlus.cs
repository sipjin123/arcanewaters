using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MonoPlus<T> : MonoBehaviour where T : MonoBehaviour {
   #region Public Variables

   public static T Self {
      get {
         string typeString = typeof(T).Name;

         // Check if we've already cached the object
         if (_objects.ContainsKey(typeString)) {
            return _objects[typeString];
         }

         // See if we can find one
         T result = FindObjectOfType<T>();

         // Cache it
         if (result !=  null) {
            _objects[typeString] = result;
         }

         return result;
      }
   }

   #endregion

   #region Private Variables

   // A cache for objects that we want to have easy access to
   protected static Dictionary<string, T> _objects = new Dictionary<string, T>();

   #endregion
}
