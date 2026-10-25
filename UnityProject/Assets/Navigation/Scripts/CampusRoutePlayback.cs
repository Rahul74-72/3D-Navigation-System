using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class CampusRoutePlayback : MonoBehaviour
{
    [SerializeField] private AnimationClip[] routeClips;
    [SerializeField, Min(1f)] private float routeDuration = 45f;
    [SerializeField] private bool loop = true;
    [SerializeField] private NavMeshData walkableCampus;
    private NavMeshDataInstance navigationInstance;
    private NavMeshAgent walkingAgent;
    private Vector3 routeStart;
    private Vector3 routeEnd;
    private float arrivedAt = -1f;
    private bool routeReady;
    private Material routeMaterial;

    private float startedAt;
    private Transform person;
    private Camera followCamera;
    private float cameraSubjectHeight;
    private Vector3 previousPosition;
    private Vector3 travelDirection;
    private Transform[] limbs;
    private Vector3[] limbRestPositions;
    private Quaternion[] limbRestRotations;
    private float[] limbHalfLengths;
    private Transform torso;
    private Transform head;
    private Vector3 torsoRest;
    private Vector3 headRest;
    private Quaternion facing;
    private float gaitPhase;
    private Vector3 gaitPreviousPosition;
    private Vector3 cameraVelocity;
    private Vector3 smoothCameraPosition;
    private bool cameraReady;

    private void StartNavigation()
    {
        if (walkableCampus == null)
        {
            Debug.LogError("Campus walking surface is missing. Walking stopped.");
            return;
        }
        navigationInstance = NavMesh.AddNavMeshData(walkableCampus);
        var start = transform.Find("NAV_NODE_gate_north");
        var end = transform.Find("NAV_NODE_iot_lab_door");
        if (start == null || end == null ||
            !NavMesh.SamplePosition(start.position, out var startHit, 2f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(end.position, out var endHit, 2f, NavMesh.AllAreas))
        {
            Debug.LogError("Cannot locate gate or lab on the walking surface.");
            return;
        }
        routeStart = startHit.position;
        routeEnd = endHit.position;
        var path = new NavMeshPath();
        if (!NavMesh.CalculatePath(routeStart, routeEnd, NavMesh.AllAreas, path) ||
            path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError("No complete walkable route to the lab. Walking stopped.");
            return;
        }
        walkingAgent = person.GetComponent<NavMeshAgent>();
        if (walkingAgent == null) walkingAgent = person.gameObject.AddComponent<NavMeshAgent>();
        walkingAgent.radius = 0.22f;
        walkingAgent.height = 1.05f;
        walkingAgent.baseOffset = 0f;
        walkingAgent.speed = 0.95f;
        walkingAgent.acceleration = 2f;
        walkingAgent.angularSpeed = 180f;
        walkingAgent.stoppingDistance = 0.08f;
        walkingAgent.updateRotation = false;
        walkingAgent.autoBraking = true;
        if (!walkingAgent.Warp(routeStart)) return;
        routeReady = walkingAgent.SetPath(path);
        previousPosition = gaitPreviousPosition = person.position;
        if (path.corners.Length > 1)
            travelDirection = Vector3.ProjectOnPlane(path.corners[1] - path.corners[0], Vector3.up).normalized;
        facing = Quaternion.LookRotation(travelDirection);
        cameraReady = false;

        foreach (var renderer in GetComponentsInChildren<Renderer>())
            if (renderer.name.StartsWith("NavRouteSegment_") || renderer.name == "PATH_GateToIoT_route")
                renderer.enabled = false;
        var guide = new GameObject("Validated gate to IoT route").AddComponent<LineRenderer>();
        guide.transform.SetParent(transform, false);
        guide.useWorldSpace = true;
        guide.widthMultiplier = 0.035f;
        routeMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        routeMaterial.SetColor("_BaseColor", new Color(0.05f, 0.65f, 0.5f));
        guide.sharedMaterial = routeMaterial;
        guide.positionCount = path.corners.Length;
        for (int i = 0; i < path.corners.Length; i++)
            guide.SetPosition(i, path.corners[i] + Vector3.up * 0.04f);
    }

    private void OnDestroy()
    {
        if (navigationInstance.valid) NavMesh.RemoveNavMeshData(navigationInstance);
        if (routeMaterial != null) Destroy(routeMaterial);
    }

    private void InitializeGait()
    {
        var names = new[] { "arm_L", "arm_R", "leg_L", "leg_R" };
        limbs = new Transform[4];
        limbRestPositions = new Vector3[4];
        limbRestRotations = new Quaternion[4];
        limbHalfLengths = new float[4];
        for (var i = 0; i < names.Length; i++)
        {
            limbs[i] = person.Find("ANIM_Companion_" + names[i]);
            if (limbs[i] == null) continue;
            limbRestPositions[i] = limbs[i].localPosition;
            limbRestRotations[i] = limbs[i].localRotation;
            var mesh = limbs[i].GetComponent<MeshFilter>();
            limbHalfLengths[i] = mesh != null ? mesh.sharedMesh.bounds.extents.y * limbs[i].localScale.y : 0.3f;
        }
        torso = person.Find("ANIM_Companion_body");
        head = person.Find("ANIM_Companion_head");
        if (torso != null) torsoRest = torso.localPosition;
        if (head != null) headRest = head.localPosition;
        gaitPreviousPosition = person.position;
        facing = person.rotation;
    }

    private void AnimateWalk()
    {
        var movement = Vector3.ProjectOnPlane(person.position - gaitPreviousPosition, Vector3.up);
        gaitPreviousPosition = person.position;
        var distance = movement.magnitude;
        if (distance > 3f) { cameraReady = false; return; }
        var moving = distance > 0.00001f;
        var size = Mathf.Max(Mathf.Abs(person.lossyScale.y), 0.1f);
        // One full stride per two steps, scaled to the character's legs.
        gaitPhase += distance / (1.1f * size) * Mathf.PI * 2f;
        if (moving)
            facing = Quaternion.Slerp(facing, Quaternion.LookRotation(movement.normalized, Vector3.up),
                1f - Mathf.Exp(-9f * Time.deltaTime));
        person.rotation = facing;
        var swing = moving ? Mathf.Sin(gaitPhase) : 0f;
        var bounce = moving ? 0.018f * (1f - Mathf.Cos(gaitPhase * 2f)) : 0f;
        for (var i = 0; i < limbs.Length; i++)
        {
            if (limbs[i] == null) continue;
            var sign = (i == 0 || i == 3) ? 1f : -1f;
            var rotation = Quaternion.Euler(swing * sign * (i < 2 ? 22f : 27f), 0f, 0f);
            var pivot = limbRestPositions[i] + Vector3.up * limbHalfLengths[i];
            limbs[i].localPosition = pivot + rotation * (limbRestPositions[i] - pivot) + Vector3.up * bounce;
            limbs[i].localRotation = rotation * limbRestRotations[i];
        }
        if (torso != null)
        {
            torso.localPosition = torsoRest + new Vector3(swing * 0.008f, bounce, 0f);
            torso.localRotation = Quaternion.Euler(moving ? 3f : 0f, swing * 3f, -swing * 1.5f);
        }
        if (head != null) head.localPosition = headRest + Vector3.up * bounce * 0.5f;
    }

    private void InitializeFollowCamera()
    {
        person = transform.Find("ANIM_Companion_Gate_to_IoT");
        foreach (var camera in GetComponentsInChildren<Camera>(true))
            if (camera.name == "CAM_GateToIoT_Follow-Camera")
                followCamera = camera;
        if (person == null || followCamera == null)
            return;
        cameraSubjectHeight = 1.81f * Mathf.Abs(person.lossyScale.y);
        previousPosition = person.position;
        travelDirection = Vector3.ProjectOnPlane(person.position - followCamera.transform.position, Vector3.up).normalized;
        if (travelDirection.sqrMagnitude < 0.01f)
            travelDirection = transform.forward;
    }

    private void Awake()
    {
        var animator = GetComponentInChildren<Animator>();
        if (animator != null)
            animator.enabled = false;

        startedAt = Time.time;
        SampleRoute(0f);
        InitializeFollowCamera();
        if (person != null) InitializeGait();
        if (person != null)
        {
            var appearance = person.GetComponent<CampusManAppearance>();
            if (appearance == null) appearance = person.gameObject.AddComponent<CampusManAppearance>();
            appearance.Build();
            StartNavigation();
        }
    }

    private void LateUpdate()
    {
        if (person == null || followCamera == null)
            InitializeFollowCamera();
        if (person == null || followCamera == null)
            return;

        var movement = Vector3.ProjectOnPlane(person.position - previousPosition, Vector3.up);
        // Ignore the teleport back to the gate when the demo loops.
        if (movement.sqrMagnitude > 0.000001f && movement.magnitude < 3f)
            travelDirection = Vector3.Slerp(travelDirection, movement.normalized, 1f - Mathf.Exp(-10f * Time.deltaTime)).normalized;
        previousPosition = person.position;
        // The path root does not bob with the head, torso or feet.
        var height = Mathf.Max(cameraSubjectHeight, 0.5f);
        var target = person.position + Vector3.up * (height * 0.65f);
        var position = target - travelDirection * (height * 1.8f) + Vector3.up * (height * 0.25f);
        if (!cameraReady || !Application.isPlaying)
        {
            smoothCameraPosition = position;
            cameraVelocity = Vector3.zero;
            cameraReady = true;
        }
        else
            smoothCameraPosition = Vector3.SmoothDamp(smoothCameraPosition, position, ref cameraVelocity, 0.12f);
        // Fixed pitch and smoothed heading prevent gait-induced camera roll/pitch.
        var rotation = Quaternion.LookRotation(travelDirection - Vector3.up * (0.25f / 1.8f), Vector3.up);
        followCamera.transform.SetPositionAndRotation(smoothCameraPosition, rotation);
        followCamera.nearClipPlane = 0.03f;
    }

    private void Update()
    {
        if (!routeReady || walkingAgent == null || !walkingAgent.isOnNavMesh)
            return;
        if (!walkingAgent.pathPending && walkingAgent.remainingDistance <= walkingAgent.stoppingDistance)
        {
            if (arrivedAt < 0f) arrivedAt = Time.time;
            if (loop && Time.time - arrivedAt > 3f)
            {
                walkingAgent.Warp(routeStart);
                walkingAgent.SetDestination(routeEnd);
                previousPosition = gaitPreviousPosition = person.position;
                cameraReady = false;
                arrivedAt = -1f;
            }
        }
        if (person != null)
        {
            if (limbs == null) InitializeGait();
            AnimateWalk();
        }
    }

    private void SampleRoute(float routeTime)
    {
        if (routeClips == null)
            return;

        foreach (var clip in routeClips)
        {
            if (clip != null && clip.name != "CAM_GateToIoT_FollowAction"
                && !clip.name.StartsWith("ANIM_Companion_arm_")
                && !clip.name.StartsWith("ANIM_Companion_leg_"))
                clip.SampleAnimation(gameObject, Mathf.Min(routeTime, clip.length));
        }
    }
}
