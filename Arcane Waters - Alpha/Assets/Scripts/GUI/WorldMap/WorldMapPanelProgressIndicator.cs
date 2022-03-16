using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WorldMapPanelProgressIndicator : MonoBehaviour
{
   #region Public Variables

   // Image that displays the current progress as a progress bar
   public Image imgProgress;

   // Text control that displays the current progress as a percentage
   public TextMeshProUGUI txtProgress; 

   #endregion

   public void updateProgress(float ratio) {
      if (imgProgress) {
         imgProgress.fillAmount = ratio;
      }

      if (txtProgress) {
         txtProgress.text = $"{(int) (ratio * 100.0f)}%";
      }
   }
}
