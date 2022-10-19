using UnityEngine;

namespace DungeonGen
{
  public class LevelData
  {
    public Room[] rooms { get; }
    public Room startRoom { get; }
  }

  public class Room
  {
    public Block[][][] blocks { get; }
    public Vector3Int size { get; }
    // center position of room in global space
    public Vector3Int position { get; }
    public Room previousRoom { get; }
    public Room nextRoom { get; }
  }

  public class Block
  {
    // position relative to room
    public Vector3 relativePosition { get; }
    public BlockType type { get; }
    public Quaternion rotation { get; }
  }

  public enum BlockType
  {
    BORDER, // the outside of a room
    DOOR, // passage to another room
    ENTRANCE, // entrance to dungeon level
    EXIT, // exit of dungeon level
    PATH,
    // - very low chance to spawn decoration
    // - no chance to spawn gizmos
    // - med chance to spawn monsters
    FILLED,
    // - solid block that can't be walked through but can be walked on if there isn't another block on top of it
    RAMP,
    // - a ramp can either lead to another ramp or an open area
    // - ex: if a ramp has reached the desired height, it needs to have  filled walkable section in front of it, left of it, and right of it, 2 rows deep
    // - this is to ensure that ramps are useful and make sense
    SPAWNABLE,
    // - high chance to spawn decoration
    // - med chance to spawn gizmos
    // - med chance to spawn monsters
    EMPTY, // empty space
  }
}
