using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ENDURE;

namespace ENDURE
{
    public class Room : MonoBehaviour
    {
        public Corridor CorridorPrefab;
        public IntVector2 Size;
        public IntVector2 Coordinates;
        public int Num;

        private GameObject _tilesObject;
        private GameObject _wallsObject;
        private GameObject _enemiesObject;
        private GameObject _itemsObject;
        public Tile TilePrefab;
        private Tile[,] _tiles;
        public GameObject WallPrefab;
        public RoomSetting Setting;

        public Dictionary<Room, Corridor> RoomCorridor = new Dictionary<Room, Corridor>();

        private Map _map;

        public GameObject PlayerPrefab;

        [Header("Enemy Spawning")]
        public GameObject[] enemyPrefabs;
        public int minEnemiesPerRoom = 0;
        public int maxEnemiesPerRoom = 2;

        [Header("Item Spawning")]
        public GameObject[] itemPrefabs;
        public int minItemsPerRoom = 1;
        public int maxItemsPerRoom = 3;

        public void Init(Map map)
        {
            _map = map;
        }

        public IEnumerator Generate()
        {
            _tilesObject = new GameObject("Tiles");
            _tilesObject.transform.parent = transform;
            _tilesObject.transform.localPosition = Vector3.zero;

            _tiles = new Tile[Size.x, Size.z];
            for (int x = 0; x < Size.x; x++)
            {
                for (int z = 0; z < Size.z; z++)
                {
                    _tiles[x, z] = CreateTile(new IntVector2((Coordinates.x + x), Coordinates.z + z));
                }
            }
            yield return null;
        }

        private Tile CreateTile(IntVector2 coordinates)
        {
            if (_map.GetTileType(coordinates) == TileType.Empty)
            {
                _map.SetTileType(coordinates, TileType.Room);
            }
            else
            {
                Debug.LogError("Tile Conflict!");
            }
            Tile newTile = Instantiate(TilePrefab);
            newTile.Coordinates = coordinates;
            newTile.name = "Tile " + coordinates.x + ", " + coordinates.z;
            newTile.transform.parent = _tilesObject.transform;

            Debug.Log($"Created individual tile: {newTile.name} at position {newTile.transform.position}");
            newTile.transform.localPosition = RoomMapManager.TileSize * new Vector3(coordinates.x - Coordinates.x - Size.x * 0.5f + 0.5f, 0f, coordinates.z - Coordinates.z - Size.z * 0.5f + 0.5f);

            if (Setting != null && Setting.floor != null)
            {
                Material tileMaterial = new Material(Setting.floor);
                Renderer tileRenderer = newTile.transform.GetChild(0).GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material = tileMaterial;
                    Debug.Log($"Applied material {tileMaterial.name} to tile {coordinates} renderer");
                }
                else
                {
                    Debug.LogError($"No renderer found on tile {coordinates} child object!");
                }
            }
            else
            {
                Debug.LogError("Floor material not set!");
            }

            return newTile;
        }

        public Corridor CreateCorridor(Room otherRoom)
        {
            if (RoomCorridor.ContainsKey(otherRoom))
            {
                return RoomCorridor[otherRoom];
            }

            Corridor newCorridor = Instantiate(CorridorPrefab);
            newCorridor.name = "Corridor (" + otherRoom.Num + ", " + Num + ")";
            newCorridor.transform.parent = transform.parent;
            newCorridor.Coordinates = new IntVector2(Coordinates.x + Size.x / 2, otherRoom.Coordinates.z + otherRoom.Size.z / 2);
            newCorridor.transform.localPosition = new Vector3(newCorridor.Coordinates.x - _map.MapSize.x / 2, 0, newCorridor.Coordinates.z - _map.MapSize.z / 2);
            newCorridor.Rooms[0] = otherRoom;
            newCorridor.Rooms[1] = this;
            newCorridor.Length = Vector3.Distance(otherRoom.transform.localPosition, transform.localPosition);
            newCorridor.Init(_map);
            otherRoom.RoomCorridor.Add(this, newCorridor);
            RoomCorridor.Add(otherRoom, newCorridor);

            return newCorridor;
        }

        public IEnumerator CreateWalls()
        {
            _wallsObject = new GameObject("Walls");
            _wallsObject.transform.parent = transform;
            _wallsObject.transform.localPosition = Vector3.zero;

            IntVector2 leftBottom = new IntVector2(Coordinates.x - 1, Coordinates.z - 1);
            IntVector2 rightTop = new IntVector2(Coordinates.x + Size.x, Coordinates.z + Size.z);
            for (int x = leftBottom.x; x <= rightTop.x; x++)
            {
                for (int z = leftBottom.z; z <= rightTop.z; z++)
                {
                    if ((x != leftBottom.x && x != rightTop.x && z != leftBottom.z && z != rightTop.z) ||
                        ((x == leftBottom.x || x == rightTop.x) && (z == leftBottom.z || z == rightTop.z)) ||
                        (_map.GetTileType(new IntVector2(x, z)) != TileType.Wall))
                    {
                        continue;
                    }
                    Quaternion rotation = Quaternion.identity;
                    if (x == leftBottom.x)
                    {
                        rotation = MapDirection.West.ToRotation();
                    }
                    else if (x == rightTop.x)
                    {
                        rotation = MapDirection.East.ToRotation();
                    }
                    else if (z == leftBottom.z)
                    {
                        rotation = MapDirection.South.ToRotation();
                    }
                    else if (z == rightTop.z)
                    {
                        rotation = MapDirection.North.ToRotation();
                    }
                    else
                    {
                        Debug.LogError("Wall is not on appropriate location!!");
                    }

                    GameObject newWall = Instantiate(WallPrefab);
                    newWall.name = "Wall (" + x + ", " + z + ")";
                    newWall.transform.parent = _wallsObject.transform;
                    newWall.transform.localPosition = RoomMapManager.TileSize * new Vector3(x - Coordinates.x - Size.x * 0.5f + 0.5f, 0f, z - Coordinates.z - Size.z * 0.5f + 0.5f);
                    newWall.transform.localRotation = rotation;
                    newWall.transform.localScale *= RoomMapManager.TileSize;

                    if (Setting != null && Setting.wall != null)
                    {
                        newWall.transform.GetChild(0).GetComponent<Renderer>().material = Setting.wall;
                    }
                    else
                    {
                        Debug.LogError("Wall material not set!");
                    }
                }
            }
            yield return null;
        }

        public IEnumerator CreateMonsters()
        {
            if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            {
                _enemiesObject = new GameObject("Enemies");
                _enemiesObject.transform.parent = transform;
                _enemiesObject.transform.localPosition = Vector3.zero;

                int enemyCount = Random.Range(minEnemiesPerRoom, maxEnemiesPerRoom + 1);

                for (int i = 0; i < enemyCount; i++)
                {
                    GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

                    Vector3 randomPosition = new Vector3(
                        Random.Range(-Size.x * 0.4f, Size.x * 0.4f),
                        0f,
                        Random.Range(-Size.z * 0.4f, Size.z * 0.4f)
                    );

                    GameObject newEnemy = Instantiate(enemyPrefab);
                    newEnemy.name = $"Enemy {i + 1}";
                    newEnemy.transform.parent = _enemiesObject.transform;
                    newEnemy.transform.localPosition = randomPosition;
                }
            }

            yield return CreateItems();
        }

        private IEnumerator CreateItems()
        {
            if (itemPrefabs != null && itemPrefabs.Length > 0)
            {
                _itemsObject = new GameObject("Items");
                _itemsObject.transform.parent = transform;
                _itemsObject.transform.localPosition = Vector3.zero;

                int itemCount = Random.Range(minItemsPerRoom, maxItemsPerRoom + 1);

                for (int i = 0; i < itemCount; i++)
                {
                    GameObject itemPrefab = itemPrefabs[Random.Range(0, itemPrefabs.Length)];

                    Vector3 randomPosition = new Vector3(
                        Random.Range(-Size.x * 0.4f, Size.x * 0.4f),
                        1f,
                        Random.Range(-Size.z * 0.4f, Size.z * 0.4f)
                    );

                    GameObject newItem = Instantiate(itemPrefab);
                    newItem.name = $"Item {i + 1}";
                    newItem.transform.parent = _itemsObject.transform;
                    newItem.transform.localPosition = randomPosition;
                }
            }

            yield return null;
        }

        public IEnumerator CreatePlayer()
        {
            GameObject player = Instantiate((PlayerPrefab));
            player.name = "Player";
            player.transform.parent = transform.parent;
            player.transform.localPosition = transform.localPosition;
            yield return null;
        }
    }
}
