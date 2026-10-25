using System;
using System.Linq;
using UnityEngine;

namespace Campus.Navigation
{
    [Serializable] public sealed class Room
    {
        public string id, building, name;
        public int floor;
        public Vector3 position;
        public bool surveyed;
        public string Label => building + " / " + (floor == 0 ? "Ground" : "Floor " + floor) + " / " + name;
    }

    [Serializable] public sealed class Checkpoint
    {
        public string id, label;
        public int floor;
        public Vector3 position;
        public float facingYaw;
        public bool surveyed;
        public string Payload => "campus:v1:" + id;
    }

    [Serializable] public sealed class CampusData
    {
        public int schemaVersion = 1;
        public string revision = "provisional-2026-10-25";
        public Room[] rooms;
        public Checkpoint[] checkpoints;

        public Room[] Search(string query, int floor = -1, string building = "All")
        {
            query = (query ?? "").Trim();
            return rooms.Where(r => (floor < 0 || r.floor == floor) &&
                (building == "All" || r.building == building) &&
                (query.Length == 0 || r.Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 r.id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
        }

        public Checkpoint Decode(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload) || payload.Length > 128 || !payload.StartsWith("campus:v1:", StringComparison.Ordinal)) return null;
            return checkpoints.FirstOrDefault(c => c.Payload == payload.Trim());
        }

        public string Validate()
        {
            if (schemaVersion != 1 || rooms == null || checkpoints == null) return "Unsupported or incomplete campus data.";
            if (rooms.Any(r => r == null || string.IsNullOrEmpty(r.id) || string.IsNullOrEmpty(r.name) || (r.building != "A" && r.building != "B") || r.floor < 0 || r.floor > 3 || !Finite(r.position))) return "Invalid room.";
            if (rooms.Select(r => r.id).Distinct().Count() != rooms.Length) return "Duplicate room ID.";
            if (checkpoints.Any(c => c == null || string.IsNullOrEmpty(c.id) || c.floor < 0 || c.floor > 3 || !Finite(c.position) || float.IsNaN(c.facingYaw) || float.IsInfinity(c.facingYaw))) return "Invalid checkpoint.";
            if (checkpoints.Select(c => c.id).Distinct().Count() != checkpoints.Length) return "Duplicate checkpoint ID.";
            return null;
        }

        public static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y) && !float.IsInfinity(v.z);

        public static CampusData Provisional()
        {
            var list = new System.Collections.Generic.List<Room>();
            float[] z = { -4.5f, .5f, 15, 20, 25, 30 };
            for (int b = 0; b < 2; b++) for (int f = 0; f < 4; f++)
            {
                string building = b == 0 ? "A" : "B";
                for (int i = 0; i < 6; i++)
                {
                    int number = f == 0 ? i + 1 : 4 + (f - 1) * 6 + i;
                    string name = "Classroom " + number;
                    if (f == 0 && i >= 3) name = new[] { "Computer Lab", "Language Lab", "IoT Lab" }[i - 3];
                    list.Add(new Room { id = building + "-F" + f + "-R" + (i + 1), building = building, floor = f, name = name,
                        position = new Vector3(b == 0 ? -4.4f : 11.8f, f * 3.3f + .01f, z[i] + 1.35f) });
                }
                list.Add(new Room { id = building + "-F" + f + "-OFFICE", building = building, floor = f,
                    name = f == 1 ? "Environment Lab" : f == 2 ? "Library" : "Office",
                    position = f == 0 ? new Vector3(b == 0 ? -2.5f : 9.9f,.07f,-7.5f) : new Vector3(b == 0 ? -5.75f : 13.65f, f * 3.3f + .01f, -10) });
            }
            var cp = new System.Collections.Generic.List<Checkpoint> {
                new Checkpoint {id="gate-a",label="Main gate A",position=new Vector3(-11.5f,.785f,-23.4f),facingYaw=0},
                new Checkpoint {id="gate-b",label="Main gate B",position=new Vector3(21.5f,.785f,-23.4f),facingYaw=0}
            };
            for (int b = 0; b < 2; b++) for (int f = 0; f < 4; f++)
                cp.Add(new Checkpoint { id = (b == 0 ? "a" : "b") + "-f" + f, label = "Building " + (b == 0 ? "A" : "B") + " floor " + f,
                    floor = f, position = new Vector3(b == 0 ? -2.5f : 9.9f, f * 3.3f + .07f, 2), facingYaw = 0 });
            return new CampusData { rooms = list.ToArray(), checkpoints = cp.ToArray() };
        }
    }

    // ARCore already fuses camera and inertial measurements. Do not integrate acceleration again.
    public sealed class PoseAlignment
    {
        public bool Calibrated { get; private set; }
        public Quaternion Rotation { get; private set; } = Quaternion.identity;
        public Vector3 Offset { get; private set; }
        public int ConfirmedFloor { get; private set; }
        public float CameraHeight { get; private set; }
        public float Travel { get; private set; }
        private Vector3 previous;
        public void Calibrate(Checkpoint checkpoint, Vector3 cameraPosition, float cameraYaw, float cameraHeight)
        {
            if (checkpoint == null || !CampusData.Finite(checkpoint.position) || float.IsNaN(checkpoint.facingYaw) || float.IsInfinity(checkpoint.facingYaw) || !CampusData.Finite(cameraPosition) || float.IsNaN(cameraYaw) || float.IsInfinity(cameraYaw) || float.IsNaN(cameraHeight) || cameraHeight < .7f || cameraHeight > 2.2f)
                throw new ArgumentException("Invalid calibration pose or phone height.");
            Rotation = Quaternion.Euler(0, Mathf.DeltaAngle(cameraYaw, checkpoint.facingYaw), 0);
            Offset = checkpoint.position + Vector3.up * cameraHeight - Rotation * cameraPosition;
            CameraHeight = cameraHeight; ConfirmedFloor = checkpoint.floor; Calibrated = true; Travel = 0;
            previous = checkpoint.position;
        }
        public Vector3 MapPosition(Vector3 sessionPosition) => Rotation * sessionPosition + Offset - Vector3.up * CameraHeight;
        public Vector3 SessionPosition(Vector3 mapPosition) => Quaternion.Inverse(Rotation) * (mapPosition - Offset);
        public bool Update(Vector3 pose, out Vector3 position)
        {
            position = MapPosition(pose);
            if (!Calibrated || !CampusData.Finite(position) || Vector3.Distance(previous, position) > 2f) { Invalidate(); return false; }
            Travel += Vector3.Distance(previous, position); previous = position;
            if (Travel > 35) { Invalidate(); return false; }
            return true;
        }
        public void Invalidate() { Calibrated = false; }
    }
}
