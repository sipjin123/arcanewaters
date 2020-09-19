using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BaseMapEntry : MonoBehaviour
{
   #region Public Variables

   // Preview image of the target base map
   public Image previewImage;

   // Title of the map
   public Text title;

   // Label that shows price in gold
   public Text goldLabel;

   // Label that shows price in gems
   public Text gemLabel;

   #endregion

   public void setData (string name, Sprite previewSprite, int goldPrice, int gemPrice, UnityAction onClick) {
      title.text = name;

      previewImage.sprite = previewSprite;

      goldLabel.text = goldPrice.ToString();
      gemLabel.text = gemPrice.ToString();

      GetComponent<Button>().onClick.RemoveAllListeners();
      GetComponent<Button>().onClick.AddListener(onClick);
      GetComponent<Button>().onClick.AddListener(() => {
         SoundManager.play2DClip(SoundManager.Type.Layouts_Destinations);
      });
   }

   public void setInteractable (bool interactable) {
      GetComponent<Button>().interactable = interactable;
   }
}
