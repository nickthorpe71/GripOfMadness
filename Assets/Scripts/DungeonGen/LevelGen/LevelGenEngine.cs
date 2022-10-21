using System.Linq;
using UnityEngine;
using DungeonGen;


public class LevelGenEngine : MonoBehaviour
{
  public GameObject borderBlockPrefab;
  public GameObject doorBlockPrefab;
  public GameObject entranceBlockPrefab;
  public GameObject exitBlockPrefab;
  public GameObject pathBlockPrefab;
  public GameObject filledBlockPrefab;
  public GameObject rampBlockPrefab;


  void Start()
  {
    LevelSchema levelSchema = new LevelSchema(8, 15, new Vector3Int(8, 5, 8), new Vector3Int(52, 52, 52), 6, 0.5f);
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
    Room[] rooms = new Room[numRooms].Select((_) =>
    {
      Room room = GenerateRoom(previousRoom, levelSchema);
      previousRoom = room;
      return room;
    }).ToArray();

    return rooms;
  }

  private Room GenerateRoom(Room previousRoom, LevelSchema levelSchema)
  {
    // determine room size
    Vector3Int roomSize = new Vector3Int(
      Random.Range(levelSchema.minRoomSize.x, levelSchema.maxRoomSize.x + 1),
      Random.Range(levelSchema.minRoomSize.y, levelSchema.maxRoomSize.y + 1),
      Random.Range(levelSchema.minRoomSize.z, levelSchema.maxRoomSize.z + 1)
    );

    Block[][][] blocks = new Block[roomSize.x][][];

    // build its outer box with BORDER sections
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

    // Place doors in the BORDER sections
    // 	- if overlapping with another room roll to see if we should create a door (parameterize chance)
    // 	- start with min 2 doors per room
    // 	- each door after 2 has less of a chance
    // 	- after (x) rooms / (maybe 50%?) are created min doors is 1
    // 	- if this is the first room make one of the doors the entrance to the level
    // 	-	if this is the last room make one of the doors the level exit

    // 3. create random series of PATH sections leading from each door to the others
    // 	- these sections cannot be filled
    // 	- https://medium.com/i-math/the-drunkards-walk-explained-48a0205d304
    // 4. place FILLED and RAMP sections under the PATH sections
    // 	- will need to detect where to put RAMP sections
    // 5. randomly fill other sections of the room with FILLED sections
    // 	- could potentially have prebuilt shapes, things coming from the ground, etc.
    // 6. check for any hollow/inaccessible sections (surrounded by other sections) and fill them
    // 7. look for areas that the player could fall into and not be able to get out of or that are too tall to reach and add a series of RAMP and FILLED sections to prevent this
    // 	- this will likely consist of looking for any sections that are FILLED with nothing above them and making sure that a ramp leads to it or any other FILLED sections around them have a ramp
    // 8. create a reference map to all open areas with a filled section below them and mark them SPAWNABLE
    // 9. fill all remaining sections with EMPTY


    return new Room(blocks, roomSize, Vector3Int.zero);
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
      case BlockType.DOOR:
        blockPrefab = doorBlockPrefab;
        break;
      case BlockType.ENTRANCE:
        blockPrefab = entranceBlockPrefab;
        break;
      case BlockType.EXIT:
        blockPrefab = exitBlockPrefab;
        break;
      case BlockType.PATH:
        blockPrefab = pathBlockPrefab;
        break;
      case BlockType.FILLED:
        blockPrefab = filledBlockPrefab;
        break;
      case BlockType.RAMP:
        blockPrefab = rampBlockPrefab;
        break;
    }

    Vector3 actualBlockPosition = new Vector3(
      room.position.x - (room.size.x / 2) + block.relativePosition.x,
      room.position.y - (room.size.y / 2) + block.relativePosition.y,
      room.position.z - (room.size.z / 2) + block.relativePosition.z
    );

    GameObject blockObject = Instantiate(blockPrefab, actualBlockPosition, block.rotation);
    return blockObject;
  }
}
