using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Environment : MonoBehaviour
{
    public IntVector3 dimension = new IntVector3(10, 3, 10);

    public Transform tilePrefab;
    public Text CurrentTileText;
    public Text DebugText;
    public IntVector3 selected;
    public float precision = 0.0001f;
    public float maxUpdateFrequency = 0.1f;
    private float lastUpdated = 0f;

    public bool Halted = false;

    private Tile[,,] tiles;

    private int dimensionLength;

    public float dissipationRate = 1000f;

    [System.Serializable]
    public struct IntVector3
    {
        public int x;
        public int y;
        public int z;

        public IntVector3(int _x, int _y, int _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", x, y, z);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is IntVector3))
            {
                return false;
            }
            IntVector3 item = (IntVector3)obj;
            return x == item.x && y == item.y && z == item.z;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    private void Awake()
    {
        Transform thisTransform = this.transform;
        dimension.x = Math.Max(dimension.x, 2);
        dimension.y = Math.Max(dimension.y, 2);
        dimension.z = Math.Max(dimension.z, 2);
        tiles = new Tile[dimension.x, dimension.y, dimension.z];
        for (int x = 0; x < dimension.x; x++)
        {
            for (int y = 0; y < dimension.y; y++)
            {
                for (int z = 0; z < dimension.z; z++)
                {
                    Transform newTileGO = Instantiate(tilePrefab, thisTransform);
                    Tile newTile = newTileGO.GetComponent<Tile>();
                    newTile.Init(this, new IntVector3(x, y, z));
                    tiles[x, y, z] = newTile;
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Halted = !Halted;
            if (!Halted)
            {
                lastUpdated = Time.time;
            }
        }

        if (!Halted && lastUpdated + maxUpdateFrequency < Time.time)
        {
            UpdateEnvironment();
            lastUpdated = Time.time;
        }
    }

    private struct TileChange
    {
        public IntVector3 coord;
        public Tile.MatterChange change;
        public TileChange(int x, int y, int z, Tile.MatterType type, float amount)
        {
            coord = new IntVector3(x, y, z);
            change = new Tile.MatterChange(type, amount);
        }
        public TileChange(IntVector3 _coord, Tile.MatterType type, float amount)
        {
            coord = _coord;
            change = new Tile.MatterChange(type, amount);
        }
    }

    private void UpdateEnvironment()
    {
        // Calculate changes
        List<TileChange> changeset = new List<TileChange>();
        float changeModifier = Math.Min(dissipationRate / Tile.capacity / (Time.time - lastUpdated), 1f) / 16f;

        IntVector3 thisCoord;
        int lastX = dimension.x - 1;
        int lastY = dimension.y - 1;
        int lastZ = dimension.z - 1;
        for (int x = 0; x < dimension.x - 1; x++)
        {
            for (int y = 0; y < dimension.y - 1; y++)
            {
                for (int z = 0; z < dimension.z - 1; z++)
                {
                    thisCoord = new IntVector3(x, y, z);
                    changeset = UpdateTilePair(thisCoord, new IntVector3(x + 1, y, z), changeModifier, changeset);
                    changeset = UpdateTilePair(thisCoord, new IntVector3(x, y + 1, z), changeModifier, changeset);
                    changeset = UpdateTilePair(thisCoord, new IntVector3(x, y, z + 1), changeModifier, changeset);
                }
                // North plane
                thisCoord = new IntVector3(x, y, lastZ);
                changeset = UpdateTilePair(thisCoord, new IntVector3(x + 1, y, lastZ), changeModifier, changeset);
                changeset = UpdateTilePair(thisCoord, new IntVector3(x, y + 1, lastZ), changeModifier, changeset);
            }

            // Top plane
            for (int z = 0; z < dimension.z - 1; z++)
            {
                thisCoord = new IntVector3(x, lastY, z);
                changeset = UpdateTilePair(thisCoord, new IntVector3(x + 1, lastY, z), changeModifier, changeset);
                changeset = UpdateTilePair(thisCoord, new IntVector3(x, lastY, z + 1), changeModifier, changeset);
            }
        }

        // East plane
        for (int z = 0; z < dimension.z - 1; z++)
        {
            for (int y = 0; y < dimension.y - 1; y++)
            {
                thisCoord = new IntVector3(lastX, y, z);
                changeset = UpdateTilePair(thisCoord, new IntVector3(lastX, y + 1, z), changeModifier, changeset);
                changeset = UpdateTilePair(thisCoord, new IntVector3(lastX, y, z + 1), changeModifier, changeset);
            }
        }

        // @TODO: need to back-calc edges?

        // Back-calc final corner
        thisCoord = new IntVector3(lastX, lastY, lastZ);
        changeset = UpdateTilePair(thisCoord, new IntVector3(lastX - 1, lastY, lastZ), changeModifier, changeset);
        changeset = UpdateTilePair(thisCoord, new IntVector3(lastX, lastY - 1, lastZ), changeModifier, changeset);
        changeset = UpdateTilePair(thisCoord, new IntVector3(lastX, lastY, lastZ - 1), changeModifier, changeset);



        // Make changes
        DebugText.text = string.Empty;
        changeset.ForEach(c =>
        {
            tiles[c.coord.x, c.coord.y, c.coord.z].AddMatter(c.change);
            if (c.coord.Equals(selected))
            {
                DebugText.text += "chg: " + c.change.ToString() + "\n";
            }
        });
    }

    private List<TileChange> UpdateTilePair(IntVector3 firstCoord, IntVector3 secondCoord, float changeModifier, List<TileChange> changeset)
    {
        Tile first = tiles[firstCoord.x, firstCoord.y, firstCoord.z];
        Tile second = tiles[secondCoord.x, secondCoord.y, secondCoord.z];
        float pressureDiff = first.filled - second.filled;
        if (Math.Abs(pressureDiff) < precision)
        {
            return changeset;
        }
        float changeAmount = pressureDiff * changeModifier;
        Tile source = changeAmount > 0 ? first : second;
        source.content.ForEach(m =>
        {
            float thisAmount = m.percent * changeAmount;
            changeset.Add(new TileChange(firstCoord, m.type, -thisAmount));
            changeset.Add(new TileChange(secondCoord, m.type, thisAmount));
        });
        return changeset;
    }
}
