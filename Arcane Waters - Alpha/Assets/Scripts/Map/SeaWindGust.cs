using UnityEngine;
using MapCreationTool.Serialization;

public class SeaWindGust : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // Visual effect of the effector
   public WindGustVFX windGustVFX = null;

   // Effector component of the wind gust
   public AreaEffector2D effector = null;

   // Collider used by effector
   public BoxCollider2D effectorCollider = null;

   // How much force to add based on wind strength
   public AnimationCurve forceOverStrength = AnimationCurve.Linear(0f, 2f, 1f, 10f);

   #endregion

   public void receiveData (DataField[] dataFields) {
      Vector2 size = new Vector2(30, 4);

      foreach (DataField field in dataFields) {
         switch (field.k) {
            case DataField.WIND_GUST_SIZE_X_KEY:
               if (field.tryGetFloatValue(out float w)) {
                  size.x = Mathf.Clamp(w, 1, 1000);
               }
               break;

            case DataField.WIND_GUST_SIZE_Y_KEY:
               if (field.tryGetFloatValue(out float h)) {
                  size.y = Mathf.Clamp(h, 1, 1000);
               }
               break;

            case DataField.WIND_GUST_ROTATION_KEY:
               if (field.tryGetFloatValue(out float rotation)) {
                  transform.rotation = Quaternion.Euler(0, 0, rotation);
               }
               break;

            case DataField.WIND_GUST_STRENGTH_KEY:
               if (field.tryGetFloatValue(out float strength)) {
                  windGustVFX.setStrength(Mathf.Clamp(strength, 0.1f, 1f));
                  effector.forceMagnitude = forceOverStrength.Evaluate(strength);
               }
               break;
         }
      }

      windGustVFX.setSize(size);

      // Retract the collider a bit to make the visual slighty larger than the collider
      size = new Vector2(Mathf.Clamp(size.x - 1f, 1f, 1000000f), Mathf.Clamp(size.y - 1f, 1f, 1000000f));
      effectorCollider.size = size;
   }

   #region Private Variables

   #endregion
}
