using System.Linq;
using UnityEngine;
using DungeonGen;


public class LevelGenEngine : MonoBehaviour
{
  public GameObject borderBlockPrefab;
  public GameObject filledBlockPrefab;
  public GameObject rampBlockPrefab;


  void Start()
  {
    LevelSchema levelSchema = new LevelSchema(8, 15, new Vector3Int(8, 5, 8), new Vector3Int(62, 52, 62), 0.5f);
    LevelData levelData = GenerateLevelData(levelSchema);
    InstantiateRoom(levelData.startRoom);
  }

  private LevelData GenerateLevelData(LevelSchema levelSchema)
  {
    int numRooms = Random.Range(levelSchema.minRooms, levelSchema.maxRooms + 1);
    Room[] rooms = PositionRooms(GenerateRooms(numRooms, levelSchema));
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

    // Create random series of PATH sections leading from each door to the others
    // 	- these sections cannot be filled
    // 	- https://medium.com/i-math/the-drunkards-walk-explained-48a0205d304

    // Place FILLED and RAMP sections under the PATH sections
    // 	- will need to detect where to put RAMP sections

    // Randomly fill other sections of the room with FILLED sections
    // 	- could potentially have prebuilt shapes, things coming from the ground, etc.


    // Check for any hollow/inaccessible sections (surrounded by other sections) and fill them

    // Look for areas that the player could fall into and not be able to get out of or that are too tall to reach and add a series of RAMP and FILLED sections to prevent this
    // 	- this will likely consist of looking for any sections that are FILLED with nothing above them and making sure that a ramp leads to it or any other FILLED sections around them have a ramp

    // Create a reference map to all open areas with a filled section below them and mark them SPAWNABLE

    // Fill all remaining sections with EMPTY


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
          if (isBorder(new Vector3Int(x, y, z), roomSize))
            blocks[x][y][z] = new Block(new Vector3(x, y, z), BlockType.BORDER, Quaternion.identity);
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
    int maxDoorsToPlace = (int)Mathf.Floor((roomSize.x + roomSize.y + roomSize.z) / 10);
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
      Vector3Int doorPosition = new Vector3Int(randomX, randomY, randomZ);
      for (int j = 0; j < levelSchema.doorHeight; j++)
      {
        BlockType type = (i == 0 && currentRoomNum == 0) ? BlockType.ENTRANCE : (i == doorsToPlace - 1 && currentRoomNum == numRooms - 1) ? BlockType.EXIT : BlockType.DOOR;

        int adjustedX = (side == 0 || side == 1) ? randomX + 1 : randomX;
        int adjustedZ = (side == 2 || side == 3) ? randomZ + 1 : randomZ;
        blocks[randomX][randomY + j][randomZ] = new Block(doorPosition, type, Quaternion.identity);
        blocks[adjustedX][randomY + j][adjustedZ] = new Block(doorPosition, type, Quaternion.identity);
      }
    }

    return blocks;
  }

  private bool isBorder(Vector3Int position, Vector3Int roomSize)
  {
    return position.x == 0 || position.x == roomSize.x - 1 || position.y == 0 || position.y == roomSize.y - 1 || position.z == 0 || position.z == roomSize.z - 1;
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
      case BlockType.DOOR:
      case BlockType.ENTRANCE:
      case BlockType.EXIT:
      case BlockType.PATH:
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
