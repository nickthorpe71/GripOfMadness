using UnityEngine;

namespace DungeonGen
{
  public class LevelData
  {
    public Room[] rooms { get; }

    public LevelData(Room[] _rooms)
    {
      rooms = _rooms;
    }
  }

  public class Room
  {
    public RoomChunk[][][] chunks { get; }

    public Room(RoomChunk[][][] _chunks)
    {
      chunks = _chunks;
    }
  }

  public class RoomChunk
  {
    public Vector3[][][] sections { get; }

    public RoomChunk(Vector3[][][] _sections)
    {
      sections = _sections;
    }
  }
}
