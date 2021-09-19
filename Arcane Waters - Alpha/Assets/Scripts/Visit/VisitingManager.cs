using UnityEngine;
// TODO: Do complex visit logic here such as guild visits etc

public class VisitingManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static VisitingManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   #region Private Variables

   #endregion
}
