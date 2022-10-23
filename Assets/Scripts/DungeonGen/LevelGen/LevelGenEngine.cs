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

  // Debug tools
  private bool visualizePath = false;

  void Start()
  {
    LevelSchema levelSchema = new LevelSchema(
      8, // min rooms
      15, // max rooms
      new Vector3Int(8, 5, 8), // min room size
      new Vector3Int(62, 44, 62), // max room size
      2, // min door width
      2, // max door width // TODO: add support for doors with width > 2
      3, // min door height
      6, // max door height
      3, // min path width
      2 * Random.Range(2, 4), // max path width
      3, // min path height
      3 * Random.Range(2, 5), // max path height
      0.5f // door change on room overlap
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
    int roomWidth = Random.Range(levelSchema.minRoomSize.x, levelSchema.maxRoomSize.x + 1);
    int roomDepth = (int)Mathf.Max(Mathf.Floor(roomWidth / 2), Random.Range(levelSchema.minRoomSize.z, levelSchema.maxRoomSize.z + 1));
    int roomHeight = (int)Mathf.Min(Mathf.Floor(roomWidth * 2), Random.Range(levelSchema.minRoomSize.y, levelSchema.maxRoomSize.y + 1));
    Vector3Int roomSize = new Vector3Int(roomWidth, roomHeight, roomDepth);

    Block[][][] blocks = new Block[roomSize.x][][];
    blocks = GenerateRoomBorder(blocks, roomSize);
    blocks = CarveOutDoors(blocks, levelSchema, roomSize, currentRoomNum, numRooms);
    blocks = AddPlatformsInFrontOfDoors(blocks, levelSchema, roomSize);
    blocks = GeneratePaths(blocks, roomSize, levelSchema);
    blocks = AddPathSupports(blocks);

    // TODO: Add path option to loop around room to get to other door
    // roll before creating a path to see if it should go around outside or through the room

    // TODO: add support for doors with width > 2
    // - this will likely require tracking door sizes in the levelSchema (or level data or something)

    // TODO: SMOOTHING
    //  - if there is a space who's x and z neighbors contain > 2 FILLED blocks, fill it
    //  - if a block has > 5 FILLED neighbors, remove it
    //  - of a block's y neighbors are both FILLED, fill it
    //  - if a FILLED block as no filled neighbors or only diagonal filled neighbors, remove it

    // TODO: add structures
    // NOTE: This part should actually not be random,
    // it should be as if an artist were adding beautiful sculptures to the dungeon
    // look up pattern algorithms, investigate how humans create sculptures
    // look up how humans create architecture
    // Randomly fill other sections of the room with FILLED sections
    // 	- could potentially have prebuilt shapes, things coming from the ground, etc.
    // Check how much open floor space there is, if there's not much we probably need to create some

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


    // TODO: ACCESSABILITY:
    // - Look for areas that the player could fall into and not be able to get out of or that are too tall to reach and add a series of RAMP and FILLED sections to prevent this
    // 	  - this will likely consist of looking for any sections that are FILLED with nothing above them and making sure that a ramp leads to it or any other FILLED sections around them have a ramp
    // - look for paths that are too high to jump and add a ramp to them
    // - find "cracks" that are less than 3 blocks wide and fill them
    // - also look for towers of PATH blocks and build a ramp, stairs, climbable to them

    // TODO: Create a reference map to all open areas with a filled section below them and mark them SPAWNABLE

    // TODO: Fill all remaining sections with EMPTY

    // TODO: Decoration
    // - walls can be decorated
    // - decoration can also include making things look more natural
    // - Could go through and replace sets of FILLED blocks with decorative objects that take up the same amount of space

    // TODO: Gizmos
    // - add chests

    // TODO: Monsters
    // - check how much open space in an area and add monsters based on that


    return new Room(blocks, roomSize, Vector3Int.zero);
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

  private Block[][][] CarveOutDoors(Block[][][] blocks, LevelSchema levelSchema, Vector3Int roomSize, int currentRoomNum, int numRooms)
  {
    // Place doors along the border of the room
    // 	- start with min 2 doors per room
    //  - after half the rooms are are created, min doors is 1
    int minDoorsToPlace = (currentRoomNum <= numRooms / 2) ? 2 : 1;
    int maxDoorsToPlace = Mathf.Min(5, Mathf.Max(2, (int)Mathf.Floor((roomSize.x + roomSize.y + roomSize.z) / 10)));
    int doorsToPlace = Random.Range(minDoorsToPlace, maxDoorsToPlace);

    for (int i = 0; i < doorsToPlace; i++)
    {
      int doorWidth = Random.Range(levelSchema.MinDoorWidth, levelSchema.MaxDoorWidth + 1);
      int doorHeight = Mathf.Min(roomSize.y - 2, Random.Range(levelSchema.MinDoorHeight, levelSchema.MaxDoorHeight + 1));
      int side = Random.Range(0, 4); // 0 = North, 1 = South, 2 = East, 3 = West

      // choose a random position along the side
      // - ensure that the door is not too close to the top or bottom of the room
      int maxX = roomSize.x - doorWidth;
      int randomStartX =
        (side == 0 || side == 1) ? Random.Range(1, maxX)
        : (side == 2) ? 0
        : roomSize.x - 1;

      int randomStartY = Random.Range(1, roomSize.y - 1 - doorHeight);

      int maxZ = roomSize.z - doorWidth;
      int randomStartZ =
        (side == 2 || side == 3) ? Random.Range(1, maxZ)
        : (side == 0) ? 0
        : roomSize.z - 1;

      // place the door blocks
      // 	- if this is the first room make one of the doors the entrance to the level
      // 	-	if this is the last room make one of the doors the level exit
      for (int j = 0; j < doorHeight; j++)
        for (int k = 0; k < doorWidth; k++)
        {
          // set the bottom of the door to DOOR/ENTRANCE/EXIT blocks and the rest to EMPTY blocks
          BlockType type = BlockType.EMPTY;
          if (j == 0)
            type = (i == 0 && currentRoomNum == 0) ? BlockType.ENTRANCE
            : (i == doorsToPlace - 1 && currentRoomNum == numRooms - 1) ? BlockType.EXIT
            : BlockType.DOOR;

          int adjustedX = (side == 0 || side == 1) ? randomStartX + k : randomStartX;
          int adjustedY = randomStartY + j;
          int adjustedZ = (side == 2 || side == 3) ? randomStartZ + k : randomStartZ;
          Vector3Int adjustedPos = new Vector3Int(adjustedX, adjustedY, adjustedZ);

          if (blocks[adjustedPos.x][adjustedPos.y][adjustedPos.z].type == BlockType.BORDER)
            blocks[adjustedPos.x][adjustedPos.y][adjustedPos.z] = new Block(adjustedPos, type, Quaternion.identity);
        }
    }

    return blocks;
  }

  private Block[][][] AddPlatformsInFrontOfDoors(Block[][][] blocks, LevelSchema levelSchema, Vector3Int roomSize)
  {
    // add platforms in front of doors
    for (int x = 0; x < roomSize.x; x++)
      for (int y = 0; y < roomSize.y; y++)
        for (int z = 0; z < roomSize.z; z++)
        {
          Block targetBlock = blocks[x][y][z];
          if (targetBlock == null) continue;
          if (targetBlock.type == BlockType.DOOR || targetBlock.type == BlockType.ENTRANCE || targetBlock.type == BlockType.EXIT)
          {
            Vector3Int direction = DirTowardCenter(new Vector3Int(x, y, z), roomSize);
            for (int i = 1; i <= 2; i++)
            {
              Vector3Int platformPos = targetBlock.relativePosition + (direction * i) + new Vector3Int(0, -1, 0);
              if (IsInRoomBounds(roomSize, platformPos) && blocks[platformPos.x][platformPos.y][platformPos.z] == null)
                blocks[platformPos.x][platformPos.y][platformPos.z] = new Block(platformPos, BlockType.FILLED, Quaternion.identity);
            }
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

        // choose direct path or perimeter path
        bool directPath = Random.Range(0, 2) == 0;
        List<Vector3Int> path = directPath
          ? GenerateDirectPath(startDoorPair[0], endDoorPair[0], roomSize)
          : GenerateDirectPath(startDoorPair[0], endDoorPair[0], roomSize);

        // GeneratePerimeterPath(startDoorPair[0], endDoorPair[0], roomSize);

        foreach (Vector3Int pathPos in path)
        {
          // choose random path width
          int pathWidth = Random.Range(levelSchema.pathWidthMin, levelSchema.pathWidthMax);

          Vector3Int pathSectionStart = new Vector3Int(pathPos.x - pathWidth / 2, pathPos.y, pathPos.z - pathWidth / 2);
          for (int i = 0; i < pathWidth; i++)
            for (int j = 0; j < pathWidth; j++)
              for (int k = 0; k < levelSchema.MinDoorHeight; k++)
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

  private List<Vector3Int> GenerateDirectPath(Block start, Block end, Vector3Int bounds)
  {
    // adjust room bounds to exclude border
    Vector3Int adjustedBounds = new Vector3Int(bounds.x - 1, bounds.y - 1, bounds.z - 1);

    List<Vector3Int> path = new List<Vector3Int>();
    Vector3Int direction = DirTowardCenter(start.relativePosition, bounds);
    Vector3Int currentPos = start.relativePosition;
    Vector3Int prevPos = start.relativePosition;
    Vector3Int endPos = end.relativePosition + DirTowardCenter(end.relativePosition, bounds);
    int maxCount = 300;
    int currentCount = 0;

    // step into center of the room 3 blocks
    int stepsIntoCenter = Mathf.Min(adjustedBounds.x, adjustedBounds.z) - 2;
    for (int i = 0; i < Random.Range(2, stepsIntoCenter); i++)
    {
      prevPos = currentPos;
      currentPos = currentPos + direction;
      path.Add(currentPos);
    }

    // step along the path until the end position is reached
    while (currentPos != endPos)
    {
      currentCount++;
      if (currentCount > maxCount)
      {
        Debug.Log("---------------- Max count reached ----------------");
        break;
      }

      direction = DirTowardEnd(currentPos, endPos, bounds);
      if (IsInRoomBounds(adjustedBounds, currentPos + direction, 1))
      {
        prevPos = currentPos;
        currentPos = currentPos + direction;
        path.Add(currentPos);
      }
    }

    return path;
  }

  private List<Vector3Int> GeneratePerimeterPath(Block start, Block end, Vector3Int bounds)
  {
    // adjust room bounds to exclude border
    Vector3Int adjustedBounds = new Vector3Int(bounds.x - 1, bounds.y - 1, bounds.z - 1);

    List<Vector3Int> path = new List<Vector3Int>();
    Vector3Int direction = DirTowardCenter(start.relativePosition, bounds);
    Vector3Int currentPos = start.relativePosition;
    Vector3Int prevPos = start.relativePosition;
    Vector3Int endPos = end.relativePosition + DirTowardCenter(end.relativePosition, bounds);
    int maxCount = 300;
    int currentCount = 0;

    // step into center of the room 2 blocks
    for (int i = 0; i < 2; i++)
    {
      prevPos = currentPos;
      currentPos = currentPos + direction;
      path.Add(currentPos);
    }

    // move along the perimeter of the room until x or z is equal to the end position
    while (currentPos.x != endPos.x || currentPos.z != endPos.z)
    {
      currentCount++;
      if (currentCount > maxCount)
      {
        Debug.Log("---------------- Max count reached ----------------");
        break;
      }

      direction = DirTowardEnd(currentPos, endPos, bounds);
      if (IsInRoomBounds(adjustedBounds, currentPos + direction, 1))
      {
        prevPos = currentPos;
        currentPos = currentPos + direction;
        path.Add(currentPos);
      }
    }

    return path;
  }

  private Vector3Int DirTowardEnd(Vector3Int currentPos, Vector3Int endPos, Vector3Int bounds)
  {
    // find direction of endPos
    Vector3Int direction = new Vector3Int(0, 0, 0);

    if (currentPos.x < endPos.x)
      direction.x = 1;
    else if (currentPos.x > endPos.x)
      direction.x = -1;

    if (currentPos.z < endPos.z)
      direction.z = 1;
    else if (currentPos.z > endPos.z)
      direction.z = -1;

    if (currentPos.y < endPos.y)
      direction.y = 1;
    else if (currentPos.y > endPos.y)
      direction.y = -1;

    if (direction.x == 0 && direction.z == 0 && direction.y == 0)
      direction = DirTowardCenter(currentPos, bounds);

    return direction;
  }

  private Block[][][] AddPathSupports(Block[][][] blocks)
  {
    int nextHeightCheck = Random.Range(3, 6);
    bool reachBottom = false;

    for (int x = 0; x < blocks.Length; x++)
      for (int y = 0; y < blocks[x].Length; y++)
      {
        // every (nextHeightCheck) levels 
        // roll to see if paths on this level should reach the bottom
        // chances of reaching bottom increase as y decreases
        // once reachBottom is set to true, it will stay true
        if (x == 0 && (y == 0 || y % nextHeightCheck == 0) && !reachBottom)
        {
          int chance = Random.Range(0, 100);
          reachBottom = chance < 70 - y / blocks[x].Length * 65;
          nextHeightCheck = Random.Range(2, 5);
        }

        for (int z = 0; z < blocks[x][y].Length; z++)
        {
          Block block = blocks[x][y][z];
          if (block == null || block.type != BlockType.PATH) continue;

          // if there is no block below the path block then add support to the bottom
          if (reachBottom && (y > 0 && blocks[x][y - 1][z] == null))
            for (int i = y - 1; i >= 0; i--)
              if (blocks[x][i][z] == null)
                blocks[x][i][z] = new Block(new Vector3Int(x, i, z), BlockType.FILLED, Quaternion.identity);
              else
                break;
          // if there is no block below the path block then add random num of support blocks
          else if ((y > 0 && blocks[x][y - 1][z] == null))
            for (int i = 1; i <= Random.Range(2, 6); i++)
              if (blocks[x][y - i][z] == null)
                blocks[x][y - i][z] = new Block(new Vector3Int(x, y - i, z), BlockType.FILLED, Quaternion.identity);
              else
                break;
        }
      }

    return blocks;
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

  // utils
  private bool IsInRoomBounds(Vector3Int roomSize, Vector3Int position, int roomMin = 0)
  {
    return position.x >= roomMin
      && position.x < roomSize.x
      && position.y >= roomMin
      && position.y < roomSize.y
      && position.z >= roomMin
      && position.z < roomSize.z;
  }

  private bool IsBorder(Vector3Int position, Vector3Int roomSize)
  {
    return position.x == 0 || position.x == roomSize.x - 1 || position.y == 0 || position.y == roomSize.y - 1 || position.z == 0 || position.z == roomSize.z - 1;
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

  // TODO: Once doors are being saved, adjust to check all door blocks in door
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

  // TODO: Once doors are saved, we can pass in door width and replace (x.Index / 2) with (x.Index / doorWidth)
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

  // Instantiation
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
        if (visualizePath)
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

}
