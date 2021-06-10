using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VoyageGroupIndicator : MonoBehaviour {
   #region Public Variables

   // The target of this arrow
   public NetEntity arrowTarget;

   // The icon of the target
   public Image targetIcon;

   // Distance clamp of the arrow indicator relative to the global player
   public const float CLAM_DISTANCE = .5f;

   // Distance before the indicator will be hidden, means its too near to the player already
   public const float CLAM_HIDE_DISTANCE = 1;

   #endregion

   private void Update () {
      if (arrowTarget != null && Global.player != null) {
         if (Vector3.Distance(Global.player.transform.position, arrowTarget.transform.position) > CLAM_HIDE_DISTANCE) {
            Vector3 localPlayerPosition = Global.player.transform.position;
            Vector3 newPosition = arrowTarget.transform.position;

            float horizontalClamp = newPosition.x;
            float verticalClamp = newPosition.y;

            Vector3 pointC = Vector3.Lerp(localPlayerPosition, arrowTarget.transform.position, 0.25f);
            horizontalClamp = Mathf.Clamp(pointC.x, localPlayerPosition.x - CLAM_DISTANCE, localPlayerPosition.x + CLAM_DISTANCE);
            verticalClamp = Mathf.Clamp(pointC.y, localPlayerPosition.y - CLAM_DISTANCE, localPlayerPosition.y + CLAM_DISTANCE);
            transform.position = new Vector3(horizontalClamp, verticalClamp, localPlayerPosition.z);

            // Set object to face direction of the target
            Vector3 dir = transform.position - arrowTarget.transform.position;
            dir.z = 0.0f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 100);
         } else {
            gameObject.SetActive(false);
         }
      }
   }

   #region Private Variables
      
   #endregion
}
