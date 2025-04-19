using Rocket.API;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace lockedCrate
{
    public class LockedCrateConfiguration : IRocketPluginConfiguration
    {
        [XmlElement("CrateId")]
        public ushort CrateId { get; set; } = 366;

        [XmlElement("SpawnTable")]
        public ushort SpawnTable { get; set; } = 1;

        [XmlElement("UnlockTimer")]
        public uint UnlockTimer { get; set; } = 15;

        [XmlElement("RespawnTimer")]
        public uint RespawnTimer { get; set; } = 30;

        [XmlArray("SpawnLocations")]
        [XmlArrayItem("Location")]
        public List<Vector3Serializable> SpawnLocations { get; set; }

        public void LoadDefaults()
        {
            SpawnLocations = new List<Vector3Serializable>
            {
                new Vector3Serializable(1, 1, 1),
                new Vector3Serializable(2, 2, 2)
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
