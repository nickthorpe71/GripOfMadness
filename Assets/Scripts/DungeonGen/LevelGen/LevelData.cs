using UnityEngine;

namespace DungeonGen
{
  public class LevelData
  {
    public Room[] rooms { get; }
    public Room startRoom { get; }

    public LevelData(Room[] rooms)
    {
      this.rooms = rooms;
      this.startRoom = rooms[0];
    }
  }

  public class Room
  {
    public Block[][][] blocks { get; }
    public Vector3Int size { get; }
    // center position of room in global space
    public Vector3Int position { get; }

    public Room(Block[][][] blocks, Vector3Int size, Vector3Int position)
    {
      this.blocks = blocks;
      this.size = size;
      this.position = position;
    }
  }

  public class Block
  {
    // position relative to room
    public Vector3Int relativePosition { get; }
    public BlockType type { get; }
    public Quaternion rotation { get; }

    public Block(Vector3Int relativePosition, BlockType type, Quaternion rotation)
    {
      this.relativePosition = relativePosition;
      this.type = type;
      this.rotation = rotation;
    }
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

  public class LevelSchema
  {

    public int minRooms { get; }
    public int maxRooms { get; }
    public Vector3Int minRoomSize { get; }
    public Vector3Int maxRoomSize { get; }
    public int MinDoorWidth { get; }
    public int MaxDoorWidth { get; }
    public int MinDoorHeight { get; }
    public int MaxDoorHeight { get; }
    public int chanceOfGiantDoor { get; }
    public int pathWidthMax { get; }
    public float doorChanceOnRoomOverlap { get; }
    public Block[][][] doors { get; set; }

    public LevelSchema(int minRooms, int maxRooms, Vector3Int minRoomSize, Vector3Int maxRoomSize, int minDoorWidth, int maxDoorWidth, int minDoorHeight, int maxDoorHeight, int chanceOfGiantDoor, int pathWidthMax, float doorChanceOnRoomOverlap)
    {
      this.minRooms = minRooms;
      this.maxRooms = maxRooms;
      this.minRoomSize = minRoomSize;
      this.maxRoomSize = maxRoomSize;
      this.MinDoorWidth = minDoorWidth;
      this.MaxDoorWidth = maxDoorWidth;
      this.MinDoorHeight = minDoorHeight;
      this.MaxDoorHeight = maxDoorHeight;
      this.chanceOfGiantDoor = chanceOfGiantDoor;
      this.pathWidthMax = pathWidthMax;
      this.doorChanceOnRoomOverlap = doorChanceOnRoomOverlap;
    }
  }
}
