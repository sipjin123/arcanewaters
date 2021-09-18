using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class StanceChangeEffect : MonoBehaviour {
   #region Public Variables

   // References to sprites representing each of the stances
   public Sprite offensiveStanceSprite, balancedStanceSprite, defensiveStanceSprite;

   // A reference to the child renderer that we will use to create the effect
   public SpriteRenderer effectRenderer;

   #endregion

   private void Awake () {
      resetValues();
   }

   private void resetValues () {
      effectRenderer.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
      effectRenderer.transform.localPosition = new Vector3(0.0f, 0.0f, effectRenderer.transform.localPosition.z);
      effectRenderer.transform.localScale = Vector3.one * 0.01f;
      effectRenderer.gameObject.SetActive(false);
      DOTween.Kill(effectRenderer);
      DOTween.Kill(effectRenderer.transform);
   }

   public void show (Battler.Stance stance, bool isLocalBattler, Transform battlerTransform) {
      resetValues();

      // Assign the appropriate sprite
      switch (stance) {
         case Battler.Stance.Attack: 
            effectRenderer.sprite = offensiveStanceSprite;
            break;
         case Battler.Stance.Balanced:
            effectRenderer.sprite = balancedStanceSprite;
            break;
         case Battler.Stance.Defense:
            effectRenderer.sprite = defensiveStanceSprite;
            break;
      }

      float effectHeight = (isLocalBattler) ? 0.55f : 0.45f;

      // Play SFX
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.STANCE_CHANGE, battlerTransform);

      effectRenderer.gameObject.SetActive(true);
      effectRenderer.DOFade(1.0f, 0.25f);
      effectRenderer.transform.DOScale(1.3f, 0.5f);
      Sequence seq = DOTween.Sequence();
      seq.Append(effectRenderer.transform.DOBlendableLocalMoveBy(Vector3.up * effectHeight, 0.5f).SetEase(Ease.OutCubic));
      seq.Append(effectRenderer.transform.DOScale(1.0f, 0.1f).SetEase(Ease.InCubic));
      seq.AppendInterval(0.5f);
      seq.Append(effectRenderer.DOFade(0.0f, 0.2f).OnComplete(() => effectRenderer.gameObject.SetActive(false)));
   }

   public void hide () {
      resetValues();
   }

   #region Private Variables
      
   #endregion
}
