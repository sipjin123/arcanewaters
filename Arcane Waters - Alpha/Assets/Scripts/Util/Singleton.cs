using UnityEngine;

/// <summary>
/// Inherit from this base class to create a singleton.
/// e.g. public class MyClassName : Singleton<MyClassName> {}
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
   #region Public Variables

   #endregion

   /// <summary>
   /// Access singleton instance through this property.
   /// </summary>
   public static T Instance {
      get {
         if (_isShuttingDown) {
            Debug.LogWarning("[Singleton] Instance '" + typeof(T) + "' already destroyed. Returning null.");
            return null;
         }

         lock (_lock) {
            if (_instance == null) {
               // Search for existing instance.
               _instance = (T) FindObjectOfType(typeof(T));

               // Create new instance if one doesn't already exist.
               if (_instance == null) {
                  // Need to create a new GameObject to attach the singleton to.
                  var singletonObject = new GameObject();
                  _instance = singletonObject.AddComponent<T>();
                  singletonObject.name = typeof(T).ToString() + " (Singleton)";

                  // Make instance persistent.
                  DontDestroyOnLoad(singletonObject);
               }
            }

            return _instance;
         }
      }
   }


   private void OnApplicationQuit () {
      _isShuttingDown = true;
   }


   private void OnDestroy () {
      _isShuttingDown = true;
   }

   #region Private Variables

   // Check to see if we're about to be destroyed.
   private static bool _isShuttingDown = false;

   // An object that can be used to lock this singleton
   private static object _lock = new object();

   // Our associated instance
   private static T _instance;

   #endregion
}