using UnityEngine;

namespace DungeonGen
{
  public class LevelSchema
  {
    public const int sectionSize = 3;
    public int roomCount { get; }
    public Vector3 roomChunkMin { get; }
    public Vector3 roomChunkMax { get; }
    public int sectionsPerChunk { get; }

    public LevelSchema(
      int _roomCount,
      int _roomChunkMin,
      int _roomChunkMax,
      int _sectionsPerChunk
    )
    {
      roomCount = _roomCount;
      roomChunkMin = new Vector3(_roomChunkMin, _roomChunkMin, _roomChunkMin);
      roomChunkMax = new Vector3(_roomChunkMax, _roomChunkMax, _roomChunkMax);
      sectionsPerChunk = _sectionsPerChunk;
    }
  }
}
