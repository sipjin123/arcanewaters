using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AdminPanelMetricsHolder : MonoBehaviour {
   #region Public Variables

   // Reference to the container that will hold metrics
   public VerticalLayoutGroup container;

   // Prefab from which metrics are created
   public GameObject metricPrefab;

   #endregion

   public void clearMetrics () {
      for (int i = container.transform.childCount-1; i >= 0; i--) {
         Transform child = container.transform.GetChild(i);
         Destroy(child.gameObject);
      }
   }

   public AdminPanelMetric addMetric(string name, string value) {
      GameObject newMetric = Instantiate(metricPrefab);
      newMetric.transform.SetParent(container.transform);
      bool hasComponent = newMetric.TryGetComponent(out AdminPanelMetric metricComponent);

      if (!hasComponent) {
         return null;
      }

      metricComponent.setName(name);
      metricComponent.setValue(value);
      return metricComponent;
   }

   #region Private Variables
      
   #endregion
}
