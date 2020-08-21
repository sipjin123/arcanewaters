using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Pathfinding;
using UnityEngine.Tilemaps;

public class OfflineArea : MonoBehaviour {
   #region Public Variables

   #endregion

   public GridGraph getGraph () {
      if (_graph == null) {
         configurePathfindingGraph();
      }

      return _graph;
   }

   private void configurePathfindingGraph () {
      _graph = AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
      _graph.center = transform.position;
      Tilemap firstTilemap = GetComponentInChildren<Tilemap>();
      _graph.SetDimensions(firstTilemap.size.x, firstTilemap.size.y, firstTilemap.cellSize.x * GetComponentInChildren<Grid>().transform.localScale.x * _pathfindingNodeSizeScale);
      _graph.rotation = new Vector3(-90.0f, 0.0f, 0.0f);
      _graph.collision.use2D = true;
      _graph.collision.Initialize(_graph.transform, 1.0f);
      _graph.collision.type = ColliderType.Sphere;
      _graph.collision.mask = LayerMask.GetMask("GridColliders");
      _graph.Scan();
   }

   #region Private Variables

   // The pathfinding graph
   private GridGraph _graph;

   // The scale of the pathfinding node size
   [SerializeField, Min(1)]
   private float _pathfindingNodeSizeScale = 1;

   #endregion
}
