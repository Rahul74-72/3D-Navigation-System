using UnityEngine;

namespace Campus.Navigation
{
    [RequireComponent(typeof(Camera))]
    public sealed class CampusFollowCamera : MonoBehaviour
    {
        public Transform target;
        public Transform obstacles;
        public float distance = 2.4f;
        private Vector3 velocity, heading, position;
        private bool initialized;
        private readonly RaycastHit[] hits = new RaycastHit[64];

        public void Snap() { initialized = false; velocity = Vector3.zero; }

        public static Vector3 DesiredPosition(Vector3 feet, Vector3 facing, float distance)
        {
            facing = Vector3.ProjectOnPlane(facing, Vector3.up).normalized;
            if (facing.sqrMagnitude < .001f) facing = Vector3.forward;
            return feet - facing * distance + Vector3.up * 1.5f;
        }

        private Vector3 Constrain(Vector3 focus, Vector3 desired)
        {
            Vector3 delta = desired - focus;
            float length = delta.magnitude;
            if (length < .001f || obstacles == null) return desired;
            int count = Physics.SphereCastNonAlloc(focus, .12f, delta / length, hits, length, ~0, QueryTriggerInteraction.Ignore);
            float allowed = length;
            for (int i = 0; i < count; i++)
                if (hits[i].collider.transform.IsChildOf(obstacles)) allowed = Mathf.Min(allowed, Mathf.Max(.15f, hits[i].distance - .06f));
            return focus + delta / length * allowed;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            // Follow the stable navigation root, never the animated head or limbs.
            Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
            heading = initialized ? Vector3.Slerp(heading, forward, 1 - Mathf.Exp(-6 * Time.deltaTime)) : forward;
            Vector3 focus = target.position + Vector3.up * 1.12f;
            Vector3 desired = Constrain(focus, DesiredPosition(target.position, heading, distance));
            position = initialized ? Vector3.SmoothDamp(position, desired, ref velocity, .16f) : desired;
            // Check again after smoothing so interpolation cannot pass through a corner.
            position = Constrain(focus, position);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(heading - Vector3.up * .16f, Vector3.up));
            initialized = true;
        }
    }
}
