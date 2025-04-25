using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace lockedCrate
{
    public class LockedCrateConfiguration : IRocketPluginConfiguration
    {
        [XmlElement("DebugLogs")]
        public bool DebugLogs { get; set; } = true;

        [XmlElement("AreaMessageDistance")]
        public float AreaMessageDistance { get; set; } = 100f;

        [XmlElement("CrateId")]
        public ushort CrateId { get; set; } = 366;

        [XmlElement("SpawnTable")]
        public ushort SpawnTable { get; set; } = 1;

        [XmlElement("ItemCountMin")]
        public int ItemCountMin { get; set; } = 3;
        
        [XmlElement("ItemCountMax")]
        public int ItemCountMax { get; set; } = 5;

        [XmlElement("UnlockTimer")]
        public uint UnlockTimer { get; set; } = 15;

        [XmlElement("RespawnTimerMin")]
        public uint RespawnTimerMin { get; set; } = 30;

        [XmlElement("RespawnTimerMax")]
        public uint RespawnTimerMax { get; set; } = 30;

        [XmlElement("DespawnTimer")]
        public uint DespawnTimer { get; set; } = 30;

        [XmlArray("SpawnLocations")]
        [XmlArrayItem("Location")]
        public List<SpawnLocation> SpawnLocations { get; set; }

        public void LoadDefaults()
        {
            SpawnLocations = new List<SpawnLocation>
            {
                new SpawnLocation("First Location", 0, 46, 0),
                new SpawnLocation("Second Location", 10, 46, 10)
            };
        }
    }

    public class SpawnLocation
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlElement("x")]
        public float X { get; set; }

        [XmlElement("y")]
        public float Y { get; set; }

        [XmlElement("z")]
        public float Z { get; set; }

        public SpawnLocation() { }

        public SpawnLocation(string name, float x, float y, float z)
        {
            Name = name;
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3 ToVector3() => new Vector3(X, Y, Z);
    }
}
