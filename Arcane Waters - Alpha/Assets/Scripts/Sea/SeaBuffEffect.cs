using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class SeaBuffEffect : MonoBehaviour {
   #region Public Variables

   // A reference to the renderer that displays the buff sprite
   public SpriteRenderer buffSpriteRenderer;

   #endregion

   private void Awake () {
      _spriteGroup = GetComponent<SpriteGroup>();

      Sequence sequence = DOTween.Sequence();
      sequence.Append(transform.DOLocalMoveY(0.25f, 0.6f).SetEase(Ease.InSine));
      sequence.AppendInterval(0.15f);
      sequence.Append(DOTween.To(() => _spriteGroup.alpha, (x) => _spriteGroup.alpha = x, 0.0f, 0.3f));
      sequence.AppendCallback(() => Destroy(gameObject));
   }

   #region Private Variables

   // A reference to the sprite group attached to this effect
   protected SpriteGroup _spriteGroup;

   #endregion
}
