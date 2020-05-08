using UnityEngine;

public class SceneSelector : MonoBehaviour
{
   #region Public Variables

   [Tooltip("The index of the scene that contains Nubis.")]
   public int NubisSceneIndex = 1;

   [Tooltip("The index of the scene that represents the Game.")]
   public int GameSceneIndex = 1;

   #endregion

   public void Start () {
#if NUBIS
      UnityEngine.SceneManagement.SceneManager.LoadScene(NubisSceneIndex);
#else
      UnityEngine.SceneManagement.SceneManager.LoadScene(GameSceneIndex);
#endif
   }

   #region Private Variables

   #endregion
}
