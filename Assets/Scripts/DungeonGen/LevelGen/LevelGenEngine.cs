using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using DungeonGen;


public class LevelGenEngine : MonoBehaviour
{
  public GameObject borderBlockPrefab;
  public GameObject filledBlockPrefab;
  public GameObject pathBlockPrefab;
  public GameObject rampBlockPrefab;


  void Start()
  {

    LevelSchema levelSchema = new LevelSchema(
      8,
      15,
      new Vector3Int(8, 5, 8),
      new Vector3Int(62, 52, 62),
      2,
      3,
      3,
      2 * Random.Range(2, 4),
      3,
      3 * Random.Range(2, 5),
      0.5f
    );
    LevelData levelData = GenerateLevelData(levelSchema);
    InstantiateRoom(levelData.startRoom);
  }

  private LevelData GenerateLevelData(LevelSchema levelSchema)
  {
    // int numRooms = Random.Range(levelSchema.minRooms, levelSchema.maxRooms + 1);
    Room[] rooms = PositionRooms(GenerateRooms(1, levelSchema));
    return new LevelData(rooms);
  }

  private Room[] GenerateRooms(int numRooms, LevelSchema levelSchema)
  {
    Room previousRoom = null;
    Room[] rooms = new Room[numRooms].Select((_, currentRoomNum) =>
    {
      Room room = GenerateRoom(levelSchema, currentRoomNum, numRooms);
      previousRoom = room;
      return room;
    }).ToArray();

    return rooms;
  }

  private Room GenerateRoom(LevelSchema levelSchema, int currentRoomNum, int numRooms)
  {
    // determine room size
    Vector3Int roomSize = new Vector3Int(
      Random.Range(levelSchema.minRoomSize.x, levelSchema.maxRoomSize.x + 1),
      Random.Range(levelSchema.minRoomSize.y, levelSchema.maxRoomSize.y + 1),
      Random.Range(levelSchema.minRoomSize.z, levelSchema.maxRoomSize.z + 1)
    );

    Block[][][] blocks = new Block[roomSize.x][][];
    blocks = GenerateRoomBorder(blocks, roomSize);
    blocks = CarveOutDoors(blocks, levelSchema, roomSize, currentRoomNum, numRooms);
    blocks = GeneratePaths(blocks, roomSize, levelSchema);


    // Place FILLED and RAMP sections under the PATH sections
    // 	- will need to detect where to put RAMP sections

    // NOTE: This part should actually not be random,
    // it should be as if an artist were adding beautiful sculptures to the dungeon
    // look up pattern algorithms, investigate how humans create sculptures
    // look up how humans create architecture
    // Randomly fill other sections of the room with FILLED sections
    // 	- could potentially have prebuilt shapes, things coming from the ground, etc.

    // --- Themes ---
    // INFO:
    // A function will take in a number of theme elements, each with a probability, and the existing blocks, and return new blocks with the themes applied.
    // Themes provide a pattern consisting of filled and empty (non fillable) spaces.
    // Themes should take a size (small, med, large) to specify how large the pattern should be.
    //  - This size will dictate the random range of the size of each application of the pattern and that range will also take into account the size of the room.
    // Some can be placed near a path and have path blocks in them to ensure the player can traverse them.

    // *- stonehenge type structures
    // *- towers
    // *- pavilions
    // *- pyramids
    // *- arches
    // *- square structures
    // *- domes
    // *- layered structures with rooms at the top
    // *- spherical structures
    // *- triangular prism roof structures with pillars
    // *- diagonal vertical beams
    // *- diagonal horizontal beams
    // *- bridges (should connect to a path and have path above them)
    // *- horizontal beams
    // *- flat square surfaces (attached to the ground)
    // *- flat circular surfaces (attached to the ground)
    // *- flat square floating platforms
    // *- balconies from walls
    // *- lots of open space (goes in an puts a bunch of empty space)


    // Check for any hollow/inaccessible sections (surrounded by other sections) and remove them

    // Look for areas that the player could fall into and not be able to get out of or that are too tall to reach and add a series of RAMP and FILLED sections to prevent this
    // 	- this will likely consist of looking for any sections that are FILLED with nothing above them and making sure that a ramp leads to it or any other FILLED sections around them have a ramp

    // Create a reference map to all open areas with a filled section below them and mark them SPAWNABLE

    // Fill all remaining sections with EMPTY


    return new Room(blocks, roomSize, Vector3Int.zero);
  }

  private bool IsInRoomBounds(Vector3Int roomSize, Vector3Int position, int roomMin = 0)
  {
    return position.x >= roomMin
      && position.x < roomSize.x
      && position.y >= roomMin
      && position.y < roomSize.y
      && position.z >= roomMin
      && position.z < roomSize.z;
  }

  private Block[][][] GenerateRoomBorder(Block[][][] blocks, Vector3Int roomSize)
  {
    // build rpp, outer box with BORDER blocks
    for (int x = 0; x < roomSize.x; x++)
    {
      blocks[x] = new Block[roomSize.y][];
      for (int y = 0; y < roomSize.y; y++)
      {
        blocks[x][y] = new Block[roomSize.z];
        for (int z = 0; z < roomSize.z; z++)
        {
          if (IsBorder(new Vector3Int(x, y, z), roomSize))
            blocks[x][y][z] = new Block(new Vector3Int(x, y, z), BlockType.BORDER, Quaternion.identity);
        }
      }
    }
    return blocks;
  }

  private bool IsBorder(Vector3Int position, Vector3Int roomSize)
  {
    return position.x == 0 || position.x == roomSize.x - 1 || position.y == 0 || position.y == roomSize.y - 1 || position.z == 0 || position.z == roomSize.z - 1;
  }

  private Block[][][] CarveOutDoors(Block[][][] blocks, LevelSchema levelSchema, Vector3Int roomSize, int currentRoomNum, int numRooms)
  {
    // Place doors along the border of the room
    // 	- start with min 2 doors per room
    //  - after half the rooms are are created, min doors is 1
    int minDoorsToPlace = (currentRoomNum <= numRooms / 2) ? 2 : 1;
    int maxDoorsToPlace = Mathf.Min(8, (int)Mathf.Floor((roomSize.x + roomSize.y + roomSize.z) / 10));
    int doorsToPlace = Random.Range(minDoorsToPlace, maxDoorsToPlace);
    int distanceFromTop = levelSchema.doorHeight + 1;

    for (int i = 0; i < doorsToPlace; i++)
    {
      // choose a random side of the room to place the door
      int side = Random.Range(0, 4);

      // choose a random position along the side
      // - ensure that the door is not too close to the top or bottom of the room
      // NOTE: 0 = North, 1 = South, 2 = East, 3 = West
      int randomX = (side == 0 || side == 1) ? Random.Range(levelSchema.doorWidth, roomSize.x - levelSchema.doorWidth) : (side == 2) ? 0 : roomSize.x - 1;
      int randomY = Random.Range(1, roomSize.y - distanceFromTop);
      int randomZ = (side == 2 || side == 3) ? Random.Range(levelSchema.doorWidth, roomSize.z - levelSchema.doorWidth) : (side == 0) ? 0 : roomSize.z - 1;

      // place the door blocks
      // 	- if this is the first room make one of the doors the entrance to the level
      // 	-	if this is the last room make one of the doors the level exit
      for (int j = 0; j < levelSchema.doorHeight; j++)
      {
        // set the bottom of the door to DOOR/ENTRANCE/EXIT blocks and the rest to EMPTY blocks
        BlockType type = BlockType.EMPTY;
        if (j == 0)
          type = (i == 0 && currentRoomNum == 0) ? BlockType.ENTRANCE : (i == doorsToPlace - 1 && currentRoomNum == numRooms - 1) ? BlockType.EXIT : BlockType.DOOR;

        int adjustedX = (side == 0 || side == 1) ? randomX + 1 : randomX;
        int adjustedZ = (side == 2 || side == 3) ? randomZ + 1 : randomZ;

        Vector3Int block1Pos = new Vector3Int(randomX, randomY + j, randomZ);
        Vector3Int block2Pos = new Vector3Int(adjustedX, randomY + j, adjustedZ);

        blocks[randomX][randomY + j][randomZ] = new Block(block1Pos, type, Quaternion.identity);
        blocks[adjustedX][randomY + j][adjustedZ] = new Block(block2Pos, type, Quaternion.identity);
      }
    }

    return blocks;
  }

  private Block[][][] GeneratePaths(Block[][][] blocks, Vector3Int roomSize, LevelSchema levelSchema)
  {
    // get all doors
    // doors are represented by the group of blocks at the bottom of the door opening
    // the doors array is a list of these groups of blocks
    Block[][] doors = GetDoors(blocks);
    List<Block[][]> paths = new List<Block[][]>();

    foreach (Block[] startDoorPair in doors)
      foreach (Block[] endDoorPair in doors)
      {
        // if the start and end doors are the same, skip this iteration
        if (startDoorPair[0].relativePosition == endDoorPair[0].relativePosition && startDoorPair[1].relativePosition == endDoorPair[1].relativePosition)
          continue;

        // if there is already a path between the start and end door
        if (PathExists(paths, startDoorPair, endDoorPair))
          continue;

        // choose random path start height
        int pathHeight = levelSchema.doorHeight;

        List<Vector3Int> path = GenerateRandomPath(startDoorPair[0], endDoorPair[0], roomSize);
        foreach (Vector3Int pathPos in path)
        {
          // choose random path width
          int pathWidth = Random.Range(levelSchema.pathWidthMin, levelSchema.pathWidthMax);
          // 20% chance to vary path height
          int chance = Random.Range(0, 100);
          if (chance < 20)
            pathHeight = Random.Range(levelSchema.pathHeightMin, levelSchema.pathHeightMax);

          Vector3Int pathSectionStart = new Vector3Int(pathPos.x - pathWidth / 2, pathPos.y, pathPos.z - pathWidth / 2);
          for (int i = 0; i < pathWidth; i++)
            for (int j = 0; j < pathWidth; j++)
              for (int k = 0; k < levelSchema.doorHeight; k++)
              {
                int adjustedX = pathSectionStart.x + i;
                int adjustedY = pathSectionStart.y + k;
                int adjustedZ = pathSectionStart.z + j;

                Vector3Int targetPos = new Vector3Int(adjustedX, adjustedY, adjustedZ);
                if (!IsInRoomBounds(roomSize, targetPos)) continue;

                Block targetBlock = blocks[adjustedX][adjustedY][adjustedZ];
                if (targetBlock == null || targetBlock.type == BlockType.EMPTY)
                  blocks[adjustedX][adjustedY][adjustedZ] = new Block(targetPos, BlockType.PATH, Quaternion.identity);
              }
        }

        // add start and end points to list of paths that have been generated
        paths.Add(new Block[2][] { startDoorPair, endDoorPair });
      }

    return blocks;
  }

  private Vector3Int DirTowardCenter(Vector3Int position, Vector3Int bounds)
  {
    // NOTE: x runs east to west, z runs north to south
    Vector3Int direction =
      // we are on the west wall so the direction is east
      position.x == 0 ? new Vector3Int(1, 0, 0)
      // we are on the east wall so the direction is west
      : position.x == bounds.x - 1 ? new Vector3Int(-1, 0, 0)
      // we are on the south wall so the direction is north
      : position.z == 0 ? new Vector3Int(0, 0, 1)
      // we are on the north wall so the direction is south
      : new Vector3Int(0, 0, -1);

    return direction;
  }

  private List<Vector3Int> GenerateRandomPath(Block start, Block end, Vector3Int bounds)
  {
    // Create random series of Vector3Ints leading start to end

    // adjust room bounds to exclude border
    Vector3Int adjustedBounds = new Vector3Int(bounds.x - 1, bounds.y - 1, bounds.z - 1);

    List<Vector3Int> path = new List<Vector3Int>();

    // get the direction of the door
    Vector3Int direction = DirTowardCenter(start.relativePosition, bounds);

    // step into center of the room 3 blocks
    Vector3Int currentPos = start.relativePosition;
    for (int i = 0; i < 3; i++)
    {
      currentPos = currentPos + direction;
      path.Add(currentPos);
    }

    int maxCount = 300;
    int currentCount = 0;

    // then move in the direction of the end
    Vector3Int endPos = end.relativePosition + DirTowardCenter(end.relativePosition, bounds);

    while (currentPos != endPos)
    {
      currentCount++;
      if (currentCount > maxCount)
        break;

      // roll to see if we should continue or take a random step or take a random walk
      int chance = Random.Range(0, 100);
      if (chance < 3)
      {
        // then move randomly for a random number of blocks
        int randomWalkMax = Mathf.Min(adjustedBounds.x + adjustedBounds.y + adjustedBounds.z, Random.Range(5, 20));
        int randomWalkMin = 3;
        int randomWalkSteps = Random.Range(randomWalkMin, randomWalkMax);

        // choose a random direction
        direction = RandomDirection();

        int stepCount = 0;
        while (stepCount < randomWalkSteps)
        {
          // roll to see if we should changeDirection
          int dirSwitchChance = Random.Range(0, 100);
          if (dirSwitchChance < 5)
            direction = RandomDirection();

          if (IsInRoomBounds(adjustedBounds, currentPos + direction, 1))
          {
            currentPos = currentPos + direction;
            path.Add(currentPos);
            stepCount++;
          }
        }
      }
      else
      {
        direction = (chance < 6) ? RandomDirection() : DirTowardEnd(currentPos, endPos);
        if (IsInRoomBounds(adjustedBounds, currentPos + direction, 1))
        {
          currentPos = currentPos + direction;
          path.Add(currentPos);
        }
      }
    }

    return path;
  }

  private Vector3Int DirTowardEnd(Vector3Int currentPos, Vector3Int endPos)
  {
    Vector3 directionVector = endPos - currentPos;
    Vector3Int direction = Vector3Int.CeilToInt(new Vector3(directionVector.x, directionVector.y, directionVector.z).normalized);
    return direction;
  }

  private Vector3Int RandomDirection()
  {
    // choose a random direction
    int randomDirection = Random.Range(0, 14);
    switch (randomDirection)
    {
      case 0:
        return new Vector3Int(1, 0, 0);
      case 1:
        return new Vector3Int(-1, 0, 0);
      case 2:
        return new Vector3Int(0, 1, 0);
      case 3:
        return new Vector3Int(0, -1, 0);
      case 4:
        return new Vector3Int(0, 0, 1);
      case 5:
        return new Vector3Int(0, 0, -1);
      case 6:
        return new Vector3Int(1, 1, 0);
      case 7:
        return new Vector3Int(1, -1, 0);
      case 8:
        return new Vector3Int(-1, 1, 0);
      case 9:
        return new Vector3Int(-1, -1, 0);
      case 10:
        return new Vector3Int(0, 1, -1);
      case 11:
        return new Vector3Int(0, -1, -1);
      case 12:
        return new Vector3Int(0, 1, 1);
      case 13:
        return new Vector3Int(0, -1, 1);
      default:
        return new Vector3Int(0, 0, 0);
    }
  }

  private bool PathExists(List<Block[][]> paths, Block[] startPair, Block[] endPair)
  {
    foreach (Block[][] path in paths)
      if (
          (
            path[0][0].relativePosition == startPair[0].relativePosition
            && path[0][1].relativePosition == startPair[1].relativePosition
            && path[1][0].relativePosition == endPair[0].relativePosition
            && path[1][1].relativePosition == endPair[1].relativePosition
          ) || (
            path[0][0].relativePosition == endPair[0].relativePosition
            && path[0][1].relativePosition == endPair[1].relativePosition
            && path[1][0].relativePosition == startPair[0].relativePosition
            && path[1][1].relativePosition == startPair[1].relativePosition
          )
      )
        return true;
    return false;
  }

  private Block[][] GetDoors(Block[][][] blocks)
  {
    // get all doors
    return blocks.Select(
      x => x.Select(
        y => y.Where(
          z => z != null &&
            (z.type == BlockType.DOOR
            || z.type == BlockType.ENTRANCE
            || z.type == BlockType.EXIT)
        ).ToArray()
      ).ToArray()
    ).ToArray()
    // flatten 3d array to 1d array
    .Aggregate((x, y) => x.Concat(y).ToArray())
    .Aggregate((y, z) => y.Concat(z).ToArray())
    // chunk the array into groups of 2
    .Select((door, doorIndex) => new { door = door, Index = doorIndex })
    .GroupBy(x => x.Index / 2)
    .Select(grp => grp.Select(x => x.door).ToArray())
    .ToArray();
  }

  private Room[] PositionRooms(Room[] rooms)
  {
    //  - if it's the first room then just build it from scratch, else build it so that it's dimensions are adjacent to a previous room's door
    // 	- make sure the center of the room is placed correctly so that an edge matches up and overlaps on an edge and over a door of the previous room
    // 	- SHOULD NOT create a BORDER section over previous rooms DOOR sections
    // 	- can overlap the edges of other rooms but cannot overlap INTO other rooms

    // determine room position
    // - if first room, position at origin
    // - if not first room, position relative to previous room
    // - make sure it doesn't overlap any other rooms

    // 	- if overlapping with another room roll to see if we should create a door (parameterize chance)

    return rooms;
  }

  private GameObject InstantiateRoom(Room room)
  {
    GameObject roomObject = new GameObject("Room");
    roomObject.transform.position = room.position;

    for (int x = 0; x < room.size.x; x++)
      for (int y = 0; y < room.size.y; y++)
        for (int z = 0; z < room.size.z; z++)
        {
          Block block = room.blocks[x][y][z];
          if (block != null)
          {
            GameObject blockObject = InstantiateBlock(block, room, roomObject);
            if (blockObject != null)
              blockObject.transform.parent = roomObject.transform;
          }
        }

    return roomObject;
  }

  private GameObject InstantiateBlock(Block block, Room room, GameObject roomObject)
  {
    GameObject blockPrefab = null;
    switch (block.type)
    {
      case BlockType.BORDER:
        blockPrefab = borderBlockPrefab;
        break;
      case BlockType.FILLED:
        blockPrefab = filledBlockPrefab;
        break;
      case BlockType.RAMP:
        blockPrefab = rampBlockPrefab;
        break;
      case BlockType.PATH:
        blockPrefab = pathBlockPrefab;
        break;
      case BlockType.DOOR:
      case BlockType.ENTRANCE:
      case BlockType.EXIT:
      case BlockType.EMPTY:
      case BlockType.SPAWNABLE:
        break;
    }
    if (blockPrefab == null)
      return null;
    Vector3 actualBlockPosition = room.position - (room.size / 2) + block.relativePosition;
    GameObject blockObject = Instantiate(blockPrefab, actualBlockPosition, block.rotation);
    return blockObject;
  }

  // Utils
  private void PrintBlocks(Block[][][] blocks)
  {
    for (int x = 0; x < blocks.Length; x++)
    {
      for (int y = 0; y < blocks[x].Length; y++)
      {
        for (int z = 0; z < blocks[x][y].Length; z++)
        {
          Block block = blocks[x][y][z];
          if (block != null)
            Debug.Log(block.type);
        }
      }
    }
  }
}
