using UnityEngine;

namespace DungeonGen
{
  public class LevelSchema
  {
    public const int sectionSize = 3;
    public int roomCount { get; }
    public Vector3 roomDimensionsMin { get; }
    public Vector3 roomDimensionsMax { get; }
    public int roomsBeforeReduceDoorMin { get; }
    public float doorChanceOnRoomOverlap { get; }

    public LevelSchema(int roomCount, Vector3 roomDimensionsMin, Vector3 roomDimensionsMax, int roomsBeforeReduceDoorMin, float doorChanceOnRoomOverlap)
    {
      this.roomCount = roomCount;
      this.roomDimensionsMin = roomDimensionsMin;
      this.roomDimensionsMax = roomDimensionsMax;
      this.roomsBeforeReduceDoorMin = roomsBeforeReduceDoorMin;
      this.doorChanceOnRoomOverlap = doorChanceOnRoomOverlap;
    }
  }
}
