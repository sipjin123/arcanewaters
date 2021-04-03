using UnityEngine;

public class GenericGameManager : MonoBehaviour {
   #region Public Variables

   #endregion

   protected virtual void Awake () {
      D.adminLog(GetType().ToString() + ".Awake", D.ADMIN_LOG_TYPE.Initialization);
   }

   #region Private Variables
      
   #endregion
}
