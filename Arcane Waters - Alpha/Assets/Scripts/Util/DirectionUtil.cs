using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

// The Direction we're facing
public enum Direction { North = 1, NorthEast = 2, East = 3, SouthEast = 4, South = 5, SouthWest = 6, West = 7, NorthWest = 8 }

public class DirectionUtil {
   #region Public Variables

   #endregion

   public static Direction getDirectionForAngle (float angle) {
      angle %= 360f;

      if (angle < 0f) {
         angle += 360f;
      } else if (angle > 360f) {
         angle -= 360f;
      }

      if (angle <= 22.5) {
         return Direction.North;
      } else if (angle >= 22.5 && angle <= 67.5) {
         return Direction.NorthWest;
      } else if (angle >= 67.5 && angle <= 112.5) {
         return Direction.West;
      } else if (angle >= 112.5 && angle <= 157.5) {
         return Direction.SouthWest;
      } else if (angle >= 157.5 && angle <= 202.5) {
         return Direction.South;
      } else if (angle >= 202.5 && angle <= 247.5) {
         return Direction.SouthEast;
      } else if (angle >= 247.5 && angle <= 292.5) {
         return Direction.East;
      } else if (angle >= 292.5 && angle <= 337.5) {
         return Direction.NorthEast;
      } else if (angle >= 337.5) {
         return Direction.North;
      }

      D.warning("Unable to get direction for angle: " + angle);
      return Direction.North;
   }

   public static Direction getDirectionForVelocity (Vector2 vec) {
      // Figure out the angle of our velocity vector
      float angle = Util.AngleBetween(Vector2.up, vec);

      return getDirectionForAngle(angle);
   }

   public static Direction getDirectionForProjectileVector (Vector2 vec) {
      // Figure out the angle of our velocity vector
      float angle = Util.AngleBetween(Vector2.up, vec);

      if (angle <= 45 || angle >= 315) {
         return Direction.North;
      } else if (angle >= 45 && angle <= 135) {
         return Direction.West;
      } else if (angle >= 135 && angle <= 225) {
         return Direction.South;
      } else if (angle >= 225 && angle <= 315) {
         return Direction.East;
      }

      D.warning("Unable to get direction for velocity: " + vec);
      return Direction.North;
   }

   public static Direction getBodyDirectionForVector (Vector2 vec) {
      // Figure out the angle of our velocity vector
      float angle = Util.AngleBetween(Vector2.up, vec);

      if (angle <= 45 || angle >= 315) {
         return Direction.North;
      } else if (angle >= 45 && angle <= 135) {
         return Direction.West;
      } else if (angle >= 135 && angle <= 225) {
         return Direction.South;
      } else if (angle >= 225 && angle <= 315) {
         return Direction.East;
      }

      D.warning("Unable to get direction for vector: " + vec);
      return Direction.North;
   }

   public static Direction getDirectionForInput (short x, short y) {
      if (x == 0 && y == 1) {
         return Direction.North;
      } else if (x == 1 && y == 1) {
         return Direction.NorthEast;
      } else if (x == 1 && y == 0) {
         return Direction.East;
      } else if (x == 1 && y == -1) {
         return Direction.SouthEast;
      } else if (x == 0 && y == -1) {
         return Direction.South;
      } else if (x == -1 && y == -1) {
         return Direction.SouthWest;
      } else if (x == -1 && y == 0) {
         return Direction.West;
      } else if (x == -1 && y == 1) {
         return Direction.NorthWest;
      }

      D.warning("Couldn't figure out facing direction for input: " + x + ", " + y);
      return Direction.North;
   }

   public static Direction getBodyDirectionForVelocity (Vector2 velocity, Direction currentDirection) {
      Direction direction = currentDirection;

      // The minimum velocity to justify a facing direction change
      float MIN = .20f;

      if (velocity.x > MIN && Mathf.Abs(velocity.x) * 1.5f > Mathf.Abs(velocity.y)) {
         direction = Direction.East;
      } else if (velocity.x < -MIN && Mathf.Abs(velocity.x) * 1.5f > Mathf.Abs(velocity.y)) {
         direction = Direction.West;
      } else if (velocity.y > MIN) {
         direction = Direction.North;
      } else if (velocity.y < -MIN) {
         direction = Direction.South;
      }

      return direction;
   }

   public static Direction getBodyDirectionForVelocity (NetEntity entity) {
      Vector2 velocity = entity.getRigidbody().velocity;

      // If we have a velocity of any magnitude, calculate a new facing direction
      if (velocity.magnitude > 0f) {
         return getBodyDirectionForVector(velocity);
      }

      // Otherwise, just stick with our previous facing direction
      return entity.facing;
   }

   public static Direction getSeaDirectionForVelocity (Vector2 vec, Direction currentDirection, bool includeDiagonals = true) {
      // If we're not moving, just keep our current facing direction
      if (vec == Vector2.zero) {
         return currentDirection;
      }

      Direction newDirection = currentDirection;

      // Figure out the angle of our velocity vector
      float angle = Util.AngleBetween(Vector2.up, vec);

      if (angle <= 22.5) {
         newDirection = Direction.North;
      } else if (angle >= 22.5 && angle <= 67.5) {
         newDirection = Direction.NorthWest;
      } else if (angle >= 67.5 && angle <= 112.5) {
         newDirection = Direction.West;
      } else if (angle >= 112.5 && angle <= 157.5) {
         newDirection = Direction.SouthWest;
      } else if (angle >= 157.5 && angle <= 202.5) {
         newDirection = Direction.South;
      } else if (angle >= 202.5 && angle <= 247.5) {
         newDirection = Direction.SouthEast;
      } else if (angle >= 247.5 && angle <= 292.5) {
         newDirection = Direction.East;
      } else if (angle >= 292.5 && angle <= 337.5) {
         newDirection = Direction.NorthEast;
      } else if (angle >= 337.5) {
         newDirection = Direction.North;
      }

      // If we don't want to include diagonals, then simplify the result
      if (!includeDiagonals) {
         if (newDirection == Direction.NorthEast || newDirection == Direction.SouthEast) {
            newDirection = Direction.East;
         }

         if (newDirection == Direction.NorthWest || newDirection == Direction.SouthWest) {
            newDirection = Direction.West;
         }
      }

      return newDirection;
   }

   public static Direction getBodyDirectionForInput (Vector2 inputVector, Direction currentDirection) {
      // Figure out which direction we're moving
      float moveX = inputVector.x;
      float moveY = inputVector.y;

      float absX = Mathf.Abs(moveX);
      float absY = Mathf.Abs(moveY);

      if (absY >= 1.5 * absX && moveY > 0f) {
         return Direction.North;
      } else if (absY >= 1.5 * absX && moveY < 0f) {
         return Direction.South;
      } else if (moveX < 0f) {
         return Direction.West;
      } else if (moveX > 0f) {
         return Direction.East;
      }

      return currentDirection;
   }

   public static Vector2 getVectorForDirection (Direction direction) {
      Vector2 vec = Vector2.zero;

      switch (direction) {
         case Direction.North:
            return new Vector2(0f, 1f);
         case Direction.NorthEast:
            return new Vector2(.7f, .7f);
         case Direction.East:
            return new Vector2(1f, 0f);
         case Direction.SouthEast:
            return new Vector2(.7f, -.7f);
         case Direction.South:
            return new Vector2(0f, -1f);
         case Direction.SouthWest:
            return new Vector2(-.7f, -.7f);
         case Direction.West:
            return new Vector2(-1f, 0f);
         case Direction.NorthWest:
            return new Vector2(-.7f, .7f);
      }

      return vec;
   }

   public static Direction getDirectionForWind (Vector2 vec) {
      // Treat no wind as north since we can't return null
      if (vec == Vector2.zero) {
         return Direction.North;
      }

      // Figure out the angle of our vector
      float angle = Util.AngleBetween(Vector2.up, vec);

      if (angle <= 22.5) {
         return Direction.North;
      } else if (angle >= 22.5 && angle <= 67.5) {
         return Direction.NorthWest;
      } else if (angle >= 67.5 && angle <= 112.5) {
         return Direction.West;
      } else if (angle >= 112.5 && angle <= 157.5) {
         return Direction.SouthWest;
      } else if (angle >= 157.5 && angle <= 202.5) {
         return Direction.South;
      } else if (angle >= 202.5 && angle <= 247.5) {
         return Direction.SouthEast;
      } else if (angle >= 247.5 && angle <= 292.5) {
         return Direction.East;
      } else if (angle >= 292.5 && angle <= 337.5) {
         return Direction.NorthEast;
      } else if (angle >= 337.5) {
         return Direction.North;
      }

      return Direction.North;
   }

   /*public static Direction getDirectionForNewDesiredPosition (SeaEntity seaEntity, Vector2 newDesiredPosition) {
      Vector2 directionVector = newDesiredPosition - (Vector2) seaEntity.transform.position;
      Direction newDirection = DirectionUtil.getSeaDirectionForVelocity(directionVector, seaEntity.direction, seaEntity.hasDiagonals);

      return newDirection;
   }*/

   public static bool areOpposite (Direction direction1, Direction direction2) {
      if ((direction1 == Direction.North && direction2 == Direction.South) ||
          (direction1 == Direction.NorthEast && direction2 == Direction.SouthWest) ||
          (direction1 == Direction.East && direction2 == Direction.West) ||
          (direction1 == Direction.SouthEast && direction2 == Direction.NorthWest) ||
          (direction1 == Direction.South && direction2 == Direction.North) ||
          (direction1 == Direction.SouthWest && direction2 == Direction.NorthEast) ||
          (direction1 == Direction.West && direction2 == Direction.East) ||
          (direction1 == Direction.NorthWest && direction2 == Direction.SouthEast)) {
         return true;
      }

      return false;
   }

   public static bool isWithTheWind (Direction moveDirection, Direction windDirection) {
      switch (moveDirection) {
         case Direction.North:
            if (windDirection == Direction.NorthWest || windDirection == Direction.North || windDirection == Direction.NorthEast) {
               return true;
            }
            break;
         case Direction.NorthEast:
            if (windDirection == Direction.North || windDirection == Direction.NorthEast || windDirection == Direction.East) {
               return true;
            }
            break;
         case Direction.East:
            if (windDirection == Direction.NorthEast || windDirection == Direction.East || windDirection == Direction.SouthEast) {
               return true;
            }
            break;
         case Direction.SouthEast:
            if (windDirection == Direction.East || windDirection == Direction.SouthEast || windDirection == Direction.South) {
               return true;
            }
            break;
         case Direction.South:
            if (windDirection == Direction.SouthEast || windDirection == Direction.South || windDirection == Direction.SouthWest) {
               return true;
            }
            break;
         case Direction.SouthWest:
            if (windDirection == Direction.South || windDirection == Direction.SouthWest || windDirection == Direction.West) {
               return true;
            }
            break;
         case Direction.West:
            if (windDirection == Direction.SouthWest || windDirection == Direction.West || windDirection == Direction.NorthWest) {
               return true;
            }
            break;
         case Direction.NorthWest:
            if (windDirection == Direction.West || windDirection == Direction.NorthWest || windDirection == Direction.North) {
               return true;
            }
            break;
      }

      return false;
   }

   public static bool isAgainstTheWind (Direction moveDirection, Direction windDirection) {
      switch (moveDirection) {
         case Direction.South:
            if (windDirection == Direction.NorthWest || windDirection == Direction.North || windDirection == Direction.NorthEast) {
               return true;
            }
            break;
         case Direction.SouthWest:
            if (windDirection == Direction.North || windDirection == Direction.NorthEast || windDirection == Direction.East) {
               return true;
            }
            break;
         case Direction.West:
            if (windDirection == Direction.NorthEast || windDirection == Direction.East || windDirection == Direction.SouthEast) {
               return true;
            }
            break;
         case Direction.NorthWest:
            if (windDirection == Direction.East || windDirection == Direction.SouthEast || windDirection == Direction.South) {
               return true;
            }
            break;
         case Direction.North:
            if (windDirection == Direction.SouthEast || windDirection == Direction.South || windDirection == Direction.SouthWest) {
               return true;
            }
            break;
         case Direction.NorthEast:
            if (windDirection == Direction.South || windDirection == Direction.SouthWest || windDirection == Direction.West) {
               return true;
            }
            break;
         case Direction.East:
            if (windDirection == Direction.SouthWest || windDirection == Direction.West || windDirection == Direction.NorthWest) {
               return true;
            }
            break;
         case Direction.SouthEast:
            if (windDirection == Direction.West || windDirection == Direction.NorthWest || windDirection == Direction.North) {
               return true;
            }
            break;
      }

      return false;
   }

   public static bool isPressingDirection (Direction direction) {
      switch (direction) {
         case Direction.North:
            return (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow));
         case Direction.East:
            return (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow));
         case Direction.South:
            return (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow));
         case Direction.West:
            return (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow));
         case Direction.NorthEast:
            return isPressingDirection(Direction.North) && isPressingDirection(Direction.East);
         case Direction.SouthEast:
            return isPressingDirection(Direction.South) && isPressingDirection(Direction.East);
         case Direction.SouthWest:
            return isPressingDirection(Direction.South) && isPressingDirection(Direction.West);
         case Direction.NorthWest:
            return isPressingDirection(Direction.North) && isPressingDirection(Direction.West);
      }

      return false;
   }

   public static List<Direction> getAvailableDirections (bool includeDiagonals, bool onlyVertical = false) {
      if (onlyVertical) {
         return new List<Direction>() { Direction.North, Direction.South };
      }

      if (includeDiagonals) {
         return new List<Direction>() {
            Direction.NorthEast, Direction.SouthEast, Direction.SouthWest, Direction.NorthWest,
            Direction.North, Direction.East, Direction.South, Direction.West
         };
      }

      return new List<Direction>() { Direction.North, Direction.East, Direction.South, Direction.West };
   }

   public static float getAngle (Direction direction) {
      return 360f - (((int) direction * 45f) - 45f);
   }

   public static Direction getFacingDirection (bool hasDiagonals, Direction selectedDirection) {
      // If we have diagonals, we don't have to do anything
      if (hasDiagonals) {
         return selectedDirection;
      }

      // We don't have diagonals, so we need to replace certain directions with just East or West
      switch (selectedDirection) {
         case Direction.NorthEast:
         case Direction.SouthEast:
            return Direction.East;
         case Direction.SouthWest:
         case Direction.NorthWest:
            return Direction.West;
      }

      // No change was needed
      return selectedDirection;
   }

   #region Private Variables

   #endregion
}
