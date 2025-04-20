using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace lockedCrate
{
    public class LockedCrateConfiguration : IRocketPluginConfiguration
    {
        [XmlElement("CrateId")]
        public ushort CrateId { get; set; } = 366; // the id of the crate that will be spawned in

        [XmlElement("SpawnTable")]
        public ushort SpawnTable { get; set; } = 1; // the id of the spawntable to be used for the loot

        [XmlElement("ItemCountMin")]
        public int ItemCountMin { get; set; } = 3; // minimum item count

        [XmlElement("ItemCountMax")]
        public int ItemCountMax { get; set; } = 5; // maximum item count

        [XmlElement("UnlockTimer")]
        public uint UnlockTimer { get; set; } = 15; // duration of the lock after the crate is first interacted with

        [XmlElement("RespawnTimerMin")]
        public uint RespawnTimerMin { get; set; } = 30; // min duration of how long it should take for the new crate to be spawned

        [XmlElement("RespawnTimerMax")]
        public uint RespawnTimerMax { get; set; } = 30; // same as above but max
        [XmlElement("DespawnTimer")]
        public uint DespawnTimer { get; set; } = 30; // how long the crate will take to despawn if not looted after its unlocked

        [XmlArray("SpawnLocations")]
        [XmlArrayItem("Location")]
        public List<Vector3Serializable> SpawnLocations { get; set; } // spawn locations for the crates

        public void LoadDefaults()
        {
            SpawnLocations = new List<Vector3Serializable>
            {
                new Vector3Serializable(0, 46, 0),
                new Vector3Serializable(10, 46, 10)
            };
        }
    }

    public class Vector3Serializable
    {
        [XmlElement("x")]
        public float X { get; set; }

        [XmlElement("y")]
        public float Y { get; set; }

        [XmlElement("z")]
        public float Z { get; set; }

        public Vector3Serializable() { }

        public Vector3Serializable(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3 ToVector3() => new Vector3(X, Y, Z);

        public static Vector3Serializable FromVector3(Vector3 vector) =>
            new Vector3Serializable(vector.x, vector.y, vector.z);
    }
}
