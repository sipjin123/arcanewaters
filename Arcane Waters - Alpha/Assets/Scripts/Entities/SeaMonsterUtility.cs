using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public static class SeaMonsterUtility
{
   public static int getDirectionToFace (NetEntity attacker, Vector3 currentPos) {
      int horizontalDirection = 0;
      int verticalDirection = 0;

      float offset = .1f;

      Vector2 spot = attacker.transform.position;
      if (spot.x > currentPos.x + offset) {
         horizontalDirection = (int) Direction.East;
      } else if (spot.x < currentPos.x - offset) {
         horizontalDirection = (int) Direction.West;
      } else {
         horizontalDirection = 0;
      }

      if (spot.y > currentPos.y + offset) {
         verticalDirection = (int) Direction.North;
      } else if (spot.y < currentPos.y - offset) {
         verticalDirection = (int) Direction.South;
      } else {
         verticalDirection = 0;
      }

      int finalDirection = 0;
      if (horizontalDirection == (int) Direction.East) {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.NorthEast;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.SouthEast;
         }

         if (verticalDirection == 0) {
            finalDirection = (int) Direction.East;
         }
      } else if (horizontalDirection == (int) Direction.West) {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.NorthWest;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.SouthWest;
         }

         if (verticalDirection == 0) {
            finalDirection = (int) Direction.West;
         }
      } else {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.North;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.South;
         }
      }

      return finalDirection;
   }

   public static Vector2 getRandomPositionAroundPosition (Vector2 position, Vector2 locationSetup) {
      float minXRange = .4f;
      float maxXRange = .6f;
      float minYRange = .6f;
      float maxYRange = .8f;

      float randomizedX = (locationSetup.x != 0 && locationSetup.y != 0) ? Random.Range(minXRange, maxXRange) : Random.Range(minYRange, maxYRange);
      float randomizedY = (locationSetup.x != 0 && locationSetup.y != 0) ? Random.Range(minXRange, maxXRange) : Random.Range(minYRange, maxYRange);

      randomizedX *= locationSetup.x;
      randomizedY *= locationSetup.y;

      Vector2 newSpot = new Vector2(position.x, position.y) + new Vector2(randomizedX, randomizedY);
      return newSpot;
   }
}