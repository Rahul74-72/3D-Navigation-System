using UnityEngine;
using UnityEngine.AI;

namespace Campus.Navigation
{
    public static class CampusRouting
    {
        public static bool TryRoute(Vector3 start, Vector3 end, out NavMeshPath path, out string error)
        {
            path = new NavMeshPath(); error = null;
            if (!CampusData.Finite(start) || !CampusData.Finite(end)) { error = "Invalid route coordinates."; return false; }
            if (!NavMesh.SamplePosition(start, out var a, .65f, NavMesh.AllAreas) || Mathf.Abs(a.position.y - start.y) > .4f)
            { error = "Position is outside a verified walking surface. Scan a nearby checkpoint."; return false; }
            if (!NavMesh.SamplePosition(end, out var b, .65f, NavMesh.AllAreas) || Mathf.Abs(b.position.y - end.y) > .4f)
            { error = "Destination has no mapped walking surface."; return false; }
            if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
            { error = "No connected route. A stair or doorway needs verification."; return false; }
            return true;
        }

        public static float Length(Vector3[] corners)
        {
            float length = 0; for (int i = 1; i < corners.Length; i++) length += Vector3.Distance(corners[i - 1], corners[i]); return length;
        }

        public static Vector3 Ahead(Vector3[] corners, float distance)
        {
            if (corners == null || corners.Length == 0) return Vector3.zero;
            for (int i = 1; i < corners.Length; i++)
            {
                float segment = Vector3.Distance(corners[i - 1], corners[i]);
                if (distance <= segment) return Vector3.Lerp(corners[i - 1], corners[i], segment == 0 ? 0 : distance / segment);
                distance -= segment;
            }
            return corners[corners.Length - 1];
        }
    }
}
