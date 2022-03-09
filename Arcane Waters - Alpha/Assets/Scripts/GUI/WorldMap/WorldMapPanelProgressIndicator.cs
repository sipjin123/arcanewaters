using UnityEngine;
using UnityEngine.UI;

public class WorldMapPanelProgressIndicator : MonoBehaviour
{
   #region Public Variables

   // Reference to the control that displays the progress as a progress bar
   public Image progressBar;

   // Reference to the control that displays the progress as a percentage
   public TMPro.TextMeshProUGUI progressText; 

   #endregion

   public void updateProgress(float ratio) {
      if (progressBar) {
         progressBar.fillAmount = ratio;
      }

      if (progressText) {
         progressText.text = $"{(int) (ratio * 100.0f)}%";
      }
   }
}
