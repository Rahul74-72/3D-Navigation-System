using UnityEngine;
using UnityEngine.AI;

public sealed class CampusFirstFloorWalk : MonoBehaviour
{
    public NavMeshData walkableCampus;
    public Camera followCamera;
    public Vector3 start = new Vector3(-11.5f, .82f, -23.4f);
    public Vector3 destination = new Vector3(-4.4f, 3.3f, -3.15f);
    public float walkingSpeed = 1.15f;
    public bool Arrived { get; private set; }
    public NavMeshAgent Agent { get; private set; }
    public float MaximumHeight { get; private set; }
    public float DistanceWalked { get; private set; }
    private NavMeshDataInstance instance;
    private Transform person, body, head;
    private readonly Transform[] limbs = new Transform[4];
    private readonly Vector3[] rests = new Vector3[4];
    private Vector3 lastPosition, heading = Vector3.forward, cameraVelocity, cameraPosition;
    private float phase;
    private bool ready, cameraReady;
    private Material routeMaterial;

    private Transform Joint(string name, Vector3 position)
    {
        var joint = new GameObject("ANIM_Companion_" + name).transform;
        joint.SetParent(person, false);
        joint.localPosition = position;
        return joint;
    }

    private void Start()
    {
        Application.runInBackground = true;
        if (walkableCampus == null || followCamera == null)
        {
            Debug.LogError("Classroom walk needs a navigation surface and camera.");
            return;
        }
        instance = NavMesh.AddNavMeshData(walkableCampus);
        var path = new NavMeshPath();
        if (!NavMesh.SamplePosition(start, out var a, 1.5f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(destination, out var b, .6f, NavMesh.AllAreas) ||
            !NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError("No complete walking route to first-floor Classroom 4. Movement stopped.");
            return;
        }
        person = new GameObject("Student - Gate to Floor 1 Classroom 4").transform;
        person.SetParent(transform, false);
        person.position = a.position;
        person.localScale = Vector3.one * .85f;
        body = Joint("body", new Vector3(0, 1.12f, 0));
        head = Joint("head", new Vector3(0, 1.68f, 0));
        limbs[0] = Joint("arm_L", new Vector3(-.25f, 1.12f, 0));
        limbs[1] = Joint("arm_R", new Vector3(.25f, 1.12f, 0));
        limbs[2] = Joint("leg_L", new Vector3(-.095f, .36f, 0));
        limbs[3] = Joint("leg_R", new Vector3(.095f, .36f, 0));
        for (int i = 0; i < 4; i++) rests[i] = limbs[i].localPosition;
        person.gameObject.AddComponent<CampusManAppearance>().Build();
        Agent = person.gameObject.AddComponent<NavMeshAgent>();
        Agent.radius = .18f;
        Agent.height = 1.65f;
        Agent.speed = walkingSpeed;
        Agent.acceleration = 2f;
        Agent.angularSpeed = 180f;
        Agent.stoppingDistance = .1f;
        Agent.updateRotation = false;
        Agent.autoBraking = true;
        if (!Agent.Warp(a.position) || !Agent.SetPath(path)) return;
        lastPosition = person.position;
        heading = Vector3.ProjectOnPlane(path.corners[1] - path.corners[0], Vector3.up).normalized;
        var line = new GameObject("Route - Floor 1 Classroom 4").AddComponent<LineRenderer>();
        line.transform.SetParent(transform, false);
        line.useWorldSpace = true;
        line.widthMultiplier = .035f;
        routeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        routeMaterial.SetColor("_BaseColor", new Color(.04f, .65f, .4f));
        line.sharedMaterial = routeMaterial;
        line.positionCount = path.corners.Length;
        for (int i = 0; i < path.corners.Length; i++) line.SetPosition(i, path.corners[i] + Vector3.up * .045f);
        ready = true;
    }

    private void Update()
    {
        if (!ready || !Agent.isOnNavMesh) return;
        Agent.speed = walkingSpeed;
        Vector3 delta = person.position - lastPosition;
        lastPosition = person.position;
        DistanceWalked += delta.magnitude;
        MaximumHeight = Mathf.Max(MaximumHeight, person.position.y);
        var horizontal = Vector3.ProjectOnPlane(delta, Vector3.up);
        bool moving = horizontal.sqrMagnitude > .0000001f;
        if (moving)
        {
            var direction = horizontal.normalized;
            person.rotation = Quaternion.Slerp(person.rotation, Quaternion.LookRotation(direction), 1 - Mathf.Exp(-9 * Time.deltaTime));
            heading = Vector3.Slerp(heading, direction, 1 - Mathf.Exp(-5 * Time.deltaTime)).normalized;
            phase += horizontal.magnitude / .95f * Mathf.PI * 2;
        }
        float swing = moving ? Mathf.Sin(phase) : 0;
        for (int i = 0; i < 4; i++)
        {
            var rotation = Quaternion.Euler(swing * ((i == 0 || i == 3) ? 1 : -1) * (i < 2 ? 20 : 25), 0, 0);
            var pivot = rests[i] + Vector3.up * .32f;
            limbs[i].localPosition = pivot + rotation * (rests[i] - pivot);
            limbs[i].localRotation = rotation;
        }
        body.localRotation = Quaternion.Euler(moving ? 2 : 0, swing * 2, -swing);
        if (!Arrived && !Agent.pathPending && Agent.remainingDistance <= Agent.stoppingDistance + .02f)
        {
            Arrived = true;
            Agent.isStopped = true;
            Debug.Log("Arrived at Classroom 4, first floor (3.3 m).");
        }
    }

    private void LateUpdate()
    {
        if (!ready) return;
        // Follow the stable navigation root, not the animated head or limbs.
        Vector3 target = person.position + Vector3.up * 1.12f;
        Vector3 desired = target - heading * 1.5f + Vector3.up * .3f;
        // The building colliders keep the follow camera on the person's side of walls.
        if (Physics.SphereCast(target, .08f, (desired - target).normalized, out var hit,
            Vector3.Distance(target, desired), ~0, QueryTriggerInteraction.Ignore))
            desired = target + (desired - target).normalized * Mathf.Max(.15f, hit.distance - .08f);
        cameraPosition = cameraReady ? Vector3.SmoothDamp(cameraPosition, desired, ref cameraVelocity, .14f) : desired;
        cameraReady = true;
        followCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(heading - Vector3.up * .2f, Vector3.up));
    }

    private void OnDestroy()
    {
        if (instance.valid) NavMesh.RemoveNavMeshData(instance);
        if (routeMaterial != null) Destroy(routeMaterial);
    }
}
