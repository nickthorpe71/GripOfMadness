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
      5, // max door width // TODO: add support for doors with width > 2
      3, // min door height
      6, // max door height
      10, // chance of giant door
      2 * Random.Range(2, 20), // max path width
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
    // TODO: THIS GenerateRoom function should optionally take in a "face" of another room which will determine:
    //  - min height so it can match the face or be larger
    //  - min width so it can match the face or be larger
    //  - starting doors which must match the face exactly
    //  - starting position which will be determined after the new rooms depth is determined
    //      (new room pos = old room pos +or- old room depth/width (depending on side) +or- new room depth/width (depending on side))

    // determine room size
    int roomWidth = Random.Range(levelSchema.minRoomSize.x, levelSchema.maxRoomSize.x + 1);
    int roomDepth = (int)Mathf.Max(Mathf.Floor(roomWidth / 2), Random.Range(levelSchema.minRoomSize.z, levelSchema.maxRoomSize.z + 1));
    int smallest = Mathf.Min(roomWidth, roomDepth);
    int roomHeight = (int)Mathf.Min(Mathf.Floor(smallest), Random.Range(levelSchema.minRoomSize.y, levelSchema.maxRoomSize.y + 1));
    Vector3Int roomSize = new Vector3Int(roomWidth, roomHeight, roomDepth);

    Block[][][] blocks = new Block[roomSize.x][][];
    GenerateRoomBorder(blocks, roomSize);
    CarveOutDoors(blocks, levelSchema, roomSize, currentRoomNum, numRooms);
    TrimUnusedBottomOfRoom(blocks, roomSize, levelSchema);
    AddPlatformsInFrontOfDoors(blocks, levelSchema, roomSize);
    GeneratePaths(blocks, roomSize, levelSchema);

    ApplySmoothing(blocks);

    // TODO: (later)
    // - add skylights

    // TODO: Add path option to loop around room to get to other door
    // roll before creating a path to see if it should go around outside or through the room

    // TODO: add structures
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



    // TODO: SLOPES
    // - add slopes on L shapes and corner slopes on corner shapes

    // TODO: Grab groupings of blocks and replace them with a structure of the same size 
    // - floors, walls, pillars, stairs replace slopes, etc

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
    int side = -1;  // 0 = North, 1 = South, 2 = East, 3 = West

    levelSchema.doors = new Block[doorsToPlace][][];

    for (int i = 0; i < doorsToPlace; i++)
    {
      // if the new side is the same as the last, try again 90% of the time
      int newSide = Random.Range(0, 4);
      while (newSide == side)
        newSide = Random.Range(0, 4);
      side = newSide;

      // roll to see if we should generate a giant door
      bool giantDoor = Random.Range(0, 100) < levelSchema.chanceOfGiantDoor;
      int maxWidth = giantDoor ? (side == 0 || side == 1) ? roomSize.x - 1 : roomSize.z - 1 : levelSchema.MaxDoorWidth;
      int doorWidth = Random.Range(levelSchema.MinDoorWidth, maxWidth);
      int doorHeight = Mathf.Min(roomSize.y - 2, Random.Range(levelSchema.MinDoorHeight, giantDoor ? roomSize.y - 1 : levelSchema.MaxDoorHeight));
      levelSchema.doors[i] = new Block[doorHeight][];

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
      {
        levelSchema.doors[i][j] = new Block[doorWidth];
        for (int k = 0; k < doorWidth; k++)
        {
          // set the bottom of the door to DOOR/ENTRANCE/EXIT blocks and the rest to EMPTY blocks
          BlockType type = BlockType.EMPTY;
          if (j == 0)
            type = (i == 0 && currentRoomNum == 0) ? BlockType.ENTRANCE
            : (i == doorsToPlace - 1 && currentRoomNum == numRooms - 1) ? BlockType.EXIT
            : BlockType.DOOR;

          // TODO: Turn on later once we are generating multiple rooms
          // Control Door shape for entrance and exit
          // if (type == BlockType.ENTRANCE || type == BlockType.EXIT)
          // {
          //   doorWidth = 2;
          //   doorHeight = 3;
          // }

          int adjustedX = (side == 0 || side == 1) ? randomStartX + k : randomStartX;
          int adjustedY = randomStartY + j;
          int adjustedZ = (side == 2 || side == 3) ? randomStartZ + k : randomStartZ;
          Vector3Int adjustedPos = new Vector3Int(adjustedX, adjustedY, adjustedZ);

          Block doorBlock = new Block(adjustedPos, type, Quaternion.identity);
          blocks[adjustedPos.x][adjustedPos.y][adjustedPos.z] = doorBlock;
          levelSchema.doors[i][j][k] = doorBlock;
        }
      }
    }

    return blocks;
  }

  private Block[][][] TrimUnusedBottomOfRoom(Block[][][] blocks, Vector3Int roomSize, LevelSchema levelSchema)
  {
    // find the lowest door position
    int lowestDoorY = levelSchema.doors.Aggregate(levelSchema.doors[0],
      (lowest, next) => next[0][0].relativePosition.y < lowest[0][0].relativePosition.y ? next : lowest)[0][0].relativePosition.y;

    // trim the bottom of the room up to 2 below the lowest door
    int trimY = lowestDoorY - 1;
    if (trimY <= 0) return blocks;

    // fill blocks below the trim level with FILLED blocks
    for (int x = 0; x < roomSize.x; x++)
      for (int y = 0; y < trimY; y++)
        for (int z = 0; z < roomSize.z; z++)
          blocks[x][y][z] = new Block(new Vector3Int(x, y, z), BlockType.FILLED, Quaternion.identity);

    return blocks;
  }

  private Block[][][] AddPlatformsInFrontOfDoors(Block[][][] blocks, LevelSchema levelSchema, Vector3Int roomSize)
  {
    int platformDepth = Random.Range(4, 8);

    foreach (Block[][] door in levelSchema.doors)
      for (int i = 0; i < door.Length; i++)
        for (int j = 0; j < door[i].Length; j++)
          for (int k = 1; k <= platformDepth; k++)
          {
            Block doorBlock = door[i][j];
            Vector3Int dirToCenter = DirTowardCenter(doorBlock.relativePosition, roomSize);

            Vector3Int platformPathPos = doorBlock.relativePosition + (dirToCenter * k);

            blocks[platformPathPos.x][platformPathPos.y][platformPathPos.z] = new Block(platformPathPos, BlockType.PATH, Quaternion.identity);
          }

    return blocks;
  }

  private Block[][][] GeneratePaths(Block[][][] blocks, Vector3Int roomSize, LevelSchema levelSchema)
  {
    // get all doors
    // doors are represented by the group of blocks at the bottom of the door opening
    // the doors array is a list of these groups of blocks
    Block[][][] doors = levelSchema.doors;
    List<Block[][][]> paths = new List<Block[][][]>();

    foreach (Block[][] startDoor in doors)
      foreach (Block[][] endDoor in doors)
      {
        // if the start and end doors are the same, skip this iteration
        if (startDoor[0][0].relativePosition == endDoor[0][0].relativePosition && startDoor[0][1].relativePosition == endDoor[0][1].relativePosition)
          continue;

        // if there is already a path between the start and end door
        if (PathExists(paths, startDoor, endDoor))
          continue;

        if (startDoor[0].Length > 10 || endDoor[0].Length > 10)
          continue;

        // get start and end door center blocks
        Block startDoorCenter = startDoor[0][(int)Mathf.Floor(startDoor[0].Length / 2)];
        Block endDoorCenter = endDoor[0][(int)Mathf.Floor(endDoor[0].Length / 2)];

        // choose direct path or perimeter path
        bool directPath = Random.Range(0, 2) == 0;
        List<Vector3Int> path = directPath
          ? GenerateDirectPath(startDoorCenter, endDoorCenter, roomSize)
          : GenerateDirectPath(startDoorCenter, endDoorCenter, roomSize);

        // GeneratePerimeterPath(startDoorCenter, endDoorCenter, roomSize);

        foreach (Vector3Int pathPos in path)
        {
          // choose random path width
          int minWidth = Mathf.Max(startDoor[0].Length, endDoor[0].Length) + 2;
          int maxWidth = Mathf.Max(minWidth + 1, levelSchema.pathWidthMax);
          int pathWidth = Random.Range(minWidth, maxWidth);

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

        blocks = AddPathSupports(blocks);

        // add start and end points to list of paths that have been generated
        paths.Add(new Block[2][][] { startDoor, endDoor });
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
    Vector3Int endPos = end.relativePosition + DirTowardCenter(end.relativePosition, bounds);
    path.Add(currentPos);

    // step along the path until the end position is reached
    while (currentPos != endPos)
    {
      direction = StepTowardEnd(currentPos, endPos, bounds);
      currentPos = currentPos + direction;
      path.Add(currentPos);
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
    Vector3Int endPos = end.relativePosition + DirTowardCenter(end.relativePosition, bounds);
    path.Add(currentPos);

    // step along the path until the end position is reached
    while (currentPos != endPos)
    {
      direction = StepTowardEnd(currentPos, endPos, bounds);
      currentPos = currentPos + direction;
      path.Add(currentPos);
    }

    return path;
  }

  private Block[][][] AddPathSupports(Block[][][] blocks)
  {
    int currHeight = Random.Range(2, 6);
    bool reachBottom = false;

    for (int x = 0; x < blocks.Length; x++)
    {
      for (int y = 0; y < blocks[x].Length; y++)
      {
        for (int z = 0; z < blocks[x][y].Length; z++)
        {
          int chance = Random.Range(0, 500000);
          if (!reachBottom)
            reachBottom = chance < 1;

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
            for (int i = 1; i <= currHeight; i++)
              if (blocks[x][y - i][z] == null)
                blocks[x][y - i][z] = new Block(new Vector3Int(x, y - i, z), BlockType.FILLED, Quaternion.identity);
              else
                break;
        }
      }
    }

    return blocks;
  }

  private Block[][][] ApplySmoothing(Block[][][] blocks)
  {
    int numChanges = 10000000;
    int round = 0;

    while (numChanges > 0)
    {
      round++;
      numChanges = 0;
      for (int x = 0; x < blocks.Length; x++)
        for (int y = 0; y < blocks[x].Length; y++)
          for (int z = 0; z < blocks[x][y].Length; z++)
          {
            Block block = blocks[x][y][z];
            if (block != null && block.type == BlockType.BORDER) continue;

            bool isFilled = block != null && (block.type == BlockType.FILLED || block.type == BlockType.BORDER);
            bool isEmpty = block == null || block.type == BlockType.EMPTY || block.type == BlockType.PATH;

            Block[] neighbors = GetNeighborsByPos(blocks, new Vector3Int(x, y, z));

            int filledNeighbors = 0;
            foreach (Block neighbor in neighbors)
              if (neighbor != null && (neighbor.type == BlockType.FILLED || neighbor.type == BlockType.BORDER))
                filledNeighbors++;

            // if a null/EMPTY block has > 4 FILLED neighbors, fill it
            if (isEmpty && filledNeighbors >= 4)
            {
              blocks[x][y][z] = new Block(new Vector3Int(x, y, z), BlockType.FILLED, Quaternion.identity);
              numChanges++;
              Debug.Log("Round " + round + ": 1");
            }

            // if a null/EMPTY block's y neighbors are both FILLED, fill it
            int yFilledNeighborsCount = neighbors.Where(neighbor => neighbor != null && neighbor.relativePosition.y != y && (neighbor.type == BlockType.FILLED || neighbor.type == BlockType.BORDER)).Count();

            if (isEmpty && yFilledNeighborsCount == 2)
            {
              blocks[x][y][z] = new Block(new Vector3Int(x, y, z), BlockType.FILLED, Quaternion.identity);
              numChanges++;
              Debug.Log("Round " + round + ": 2");
            }

            // to fill vertical gaps of 2:
            // if a null/EMPTY block has 1 FILLED y neighbor and a gap(null/EMPTY) followed by a FILLED in the opposite y direction,
            // fill the block and the null/EMPTY block in the opposite y direction
            if (isEmpty && yFilledNeighborsCount == 1)
            {
              Block yNeighbor = neighbors.Where(neighbor => neighbor != null && neighbor.relativePosition.y != y).First();
              int oppositeDirOfYNeighbor = yNeighbor.relativePosition.y > y ? -1 : 1;

              // if the block in the opposite y direction is in bounds
              if (oppositeDirOfYNeighbor >= 0 && oppositeDirOfYNeighbor < blocks[x].Length)
              {
                Block yNeighborOpposite = blocks[x][y + oppositeDirOfYNeighbor][z];
                // get y neighbors y neighbor in the opposite y direction
                if (y + oppositeDirOfYNeighbor * 2 >= 0 && y + oppositeDirOfYNeighbor * 2 < blocks[x].Length)
                {
                  Block yNeighborOppositeYNeighbor = blocks[x][y + oppositeDirOfYNeighbor * 2][z];
                  // if the block in the opposite y direction is null/EMPTY and the y neighbor in the opposite y direction is FILLED
                  if ((yNeighborOpposite == null || yNeighborOpposite.type == BlockType.EMPTY || yNeighborOpposite.type == BlockType.PATH) && yNeighborOppositeYNeighbor != null && (yNeighborOppositeYNeighbor.type == BlockType.FILLED || yNeighborOppositeYNeighbor.type == BlockType.BORDER))
                  {
                    blocks[x][y][z] = new Block(new Vector3Int(x, y, z), BlockType.FILLED, Quaternion.identity);
                    blocks[x][y + oppositeDirOfYNeighbor][z] = new Block(new Vector3Int(x, y + oppositeDirOfYNeighbor, z), BlockType.FILLED, Quaternion.identity);
                    numChanges++;
                    Debug.Log("Round " + round + ": 3");
                  }
                }
              }
            }

            // if a FILLED block as no filled neighbors, remove it
            if (isFilled && filledNeighbors == 0)
            {
              blocks[x][y][z] = null;
              numChanges++;
              Debug.Log("Round " + round + ": 4");
            }

            // get filled neighbors on x
            int xFilledNeighborsCount = neighbors
              .Where(neighbor => neighbor != null
                && neighbor.relativePosition.x != x
                && (neighbor.type == BlockType.FILLED || neighbor.type == BlockType.BORDER))
              .Count();

            // get filled neighbors on z
            int zFilledNeighborsCount = neighbors
              .Where(neighbor => neighbor != null
                && neighbor.relativePosition.z != z
                && (neighbor.type == BlockType.FILLED || neighbor.type == BlockType.BORDER))
              .Count();

            if (isEmpty && (xFilledNeighborsCount == 2 || zFilledNeighborsCount == 2))
            {
              blocks[x][y][z] = new Block(new Vector3Int(x, y, z), BlockType.FILLED, Quaternion.identity);
              numChanges++;
              Debug.Log("Round " + round + ": 5");
            }

            // // if a FILLED block has 6 FILLED or BORDER neighbors, remove it
            // if (block != null && block.type == BlockType.FILLED && filledNeighbors == 6)
            // {
            //   blocks[x][y][z] = new Block(new Vector3Int(x, y, z), BlockType.EMPTY, Quaternion.identity);
            //   numChanges++;
            // }

            if (round > 15) break;
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

  private Vector3Int StepTowardEnd(Vector3Int currentPos, Vector3Int endPos, Vector3Int bounds)
  {
    // find direction of endPos
    Vector3Int direction = new Vector3Int(0, 0, 0);

    int xDirection = endPos.x > currentPos.x ? 1 : endPos.x < currentPos.x ? -1 : 0;
    int xStep = Mathf.Min(Mathf.Abs(endPos.x - currentPos.x), 3) * xDirection;
    int zDirection = endPos.z > currentPos.z ? 1 : endPos.z < currentPos.z ? -1 : 0;
    int zStep = Mathf.Min(Mathf.Abs(endPos.z - currentPos.z), 3) * zDirection;

    direction.x = xStep;
    direction.z = zStep;

    if (direction.x <= 1 && direction.z <= 1)
    {
      if (currentPos.y < endPos.y)
        direction.y = 1;
      else if (currentPos.y > endPos.y)
        direction.y = -1;
    }

    if (direction.x == 0 && direction.z == 0 && direction.y == 0)
      direction = DirTowardCenter(currentPos, bounds);

    return direction;
  }

  // TODO: Once doors are being saved, adjust to check all door blocks in door
  private bool PathExists(List<Block[][][]> paths, Block[][] start, Block[][] end)
  {
    foreach (Block[][][] path in paths)
      if (
          (
            path[0][0][0].relativePosition == start[0][0].relativePosition
            && path[0][0][1].relativePosition == start[0][1].relativePosition
            && path[0][1][0].relativePosition == end[0][0].relativePosition
            && path[1][1][1].relativePosition == end[0][1].relativePosition
          ) || (
            path[0][0][0].relativePosition == end[0][0].relativePosition
            && path[0][0][1].relativePosition == end[0][1].relativePosition
            && path[0][1][0].relativePosition == start[0][0].relativePosition
            && path[1][1][1].relativePosition == start[0][1].relativePosition
          )
      )
        return true;
    return false;
  }

  public Block[] GetNeighborsByPos(Block[][][] blocks, Vector3Int target)
  {
    List<Block> neighbors = new List<Block>();

    // check all 6 directions
    // - if the block is not null and is not the target block then add it to the list of neighbors
    // - if the block is null then add it to the list of neighbors

    // check north
    if (target.z < blocks[0][0].Length - 1)
      neighbors.Add(blocks[target.x][target.y][target.z + 1]);

    // check south
    if (target.z > 0)
      neighbors.Add(blocks[target.x][target.y][target.z - 1]);

    // check east
    if (target.x < blocks.Length - 1)
      neighbors.Add(blocks[target.x + 1][target.y][target.z]);

    // check west
    if (target.x > 0)
      neighbors.Add(blocks[target.x - 1][target.y][target.z]);

    // check up
    if (target.y < blocks[0].Length - 1)
      neighbors.Add(blocks[target.x][target.y + 1][target.z]);

    // check down
    if (target.y > 0)
      neighbors.Add(blocks[target.x][target.y - 1][target.z]);

    return neighbors.ToArray();
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
