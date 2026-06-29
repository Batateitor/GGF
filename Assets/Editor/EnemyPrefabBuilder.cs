using UnityEditor;
using UnityEngine;

public static class EnemyPrefabBuilder
{
    private const string PrefabFolder = "Assets/Prefabs/Enemies";
    private const string MaterialFolder = "Assets/Materials/Enemies";

    [MenuItem("GGF/Create Enemy Prefabs")]
    public static void CreateEnemyPrefabs()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Enemies");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Enemies");

        Material fastMaterial = CreateMaterial("FastEnemy_Red", new Color(0.82f, 0.12f, 0.09f));
        Material listenerMaterial = CreateMaterial("ListenerEnemy_Blue", new Color(0.1f, 0.36f, 0.88f));
        Material balancedMaterial = CreateMaterial("BalancedEnemy_Green", new Color(0.18f, 0.62f, 0.34f));
        Material darkMaterial = CreateMaterial("Enemy_DarkTrim", new Color(0.06f, 0.05f, 0.05f));
        Material eyeMaterial = CreateMaterial("Enemy_EyeGlow", new Color(1f, 0.78f, 0.18f));

        CreateEnemy(
            "Enemy_FastRunner_LowHearing",
            AdvancedEnemyAgent.EnemyStyle.FastLowHearing,
            fastMaterial,
            darkMaterial,
            eyeMaterial,
            hearingRadius: 2.75f,
            viewDistance: 6.5f,
            patrolSpeed: 3.8f,
            investigateSpeed: 5.3f,
            chaseSpeed: 8.2f,
            evadeSpeed: 7.2f,
            maxForce: 26f,
            viewAngle: 85f,
            usesPursue: true,
            evadesWhenCornered: false,
            scale: new Vector3(0.82f, 1.08f, 0.82f));

        CreateEnemy(
            "Enemy_SlowListener",
            AdvancedEnemyAgent.EnemyStyle.SlowListener,
            listenerMaterial,
            darkMaterial,
            eyeMaterial,
            hearingRadius: 9.5f,
            viewDistance: 5.5f,
            patrolSpeed: 1.7f,
            investigateSpeed: 2.25f,
            chaseSpeed: 3.1f,
            evadeSpeed: 2.7f,
            maxForce: 12f,
            viewAngle: 115f,
            usesPursue: false,
            evadesWhenCornered: false,
            scale: new Vector3(1.12f, 1f, 1.12f));

        CreateEnemy(
            "Enemy_BalancedScout",
            AdvancedEnemyAgent.EnemyStyle.Balanced,
            balancedMaterial,
            darkMaterial,
            eyeMaterial,
            hearingRadius: 6f,
            viewDistance: 7f,
            patrolSpeed: 2.6f,
            investigateSpeed: 3.4f,
            chaseSpeed: 5.2f,
            evadeSpeed: 4.8f,
            maxForce: 18f,
            viewAngle: 100f,
            usesPursue: true,
            evadesWhenCornered: true,
            scale: Vector3.one);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("GGF enemy prefabs created in " + PrefabFolder);
    }

    private static void CreateEnemy(
        string prefabName,
        AdvancedEnemyAgent.EnemyStyle style,
        Material bodyMaterial,
        Material darkMaterial,
        Material eyeMaterial,
        float hearingRadius,
        float viewDistance,
        float patrolSpeed,
        float investigateSpeed,
        float chaseSpeed,
        float evadeSpeed,
        float maxForce,
        float viewAngle,
        bool usesPursue,
        bool evadesWhenCornered,
        Vector3 scale)
    {
        GameObject root = new GameObject(prefabName);
        root.name = prefabName;
        root.transform.localScale = scale;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1f, 0f);
        collider.radius = 0.45f;
        collider.height = 2f;

        Rigidbody rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        SteeringAgent steering = root.AddComponent<SteeringAgent>();
        steering.maxSpeed = chaseSpeed;
        steering.maxForce = maxForce;
        steering.arriveRadius = style == AdvancedEnemyAgent.EnemyStyle.SlowListener ? 2.8f : 1.8f;
        steering.predictionTime = style == AdvancedEnemyAgent.EnemyStyle.FastLowHearing ? 0.75f : 0.45f;
        steering.wanderRadius = style == AdvancedEnemyAgent.EnemyStyle.SlowListener ? 1.2f : 1.9f;
        steering.wanderDistance = style == AdvancedEnemyAgent.EnemyStyle.FastLowHearing ? 3.1f : 2.2f;

        AdvancedEnemyAgent agent = root.AddComponent<AdvancedEnemyAgent>();
        agent.style = style;
        agent.displayName = prefabName;
        agent.hearingRadius = hearingRadius;
        agent.viewDistance = viewDistance;
        agent.viewAngle = viewAngle;
        agent.patrolSpeed = patrolSpeed;
        agent.investigateSpeed = investigateSpeed;
        agent.chaseSpeed = chaseSpeed;
        agent.evadeSpeed = evadeSpeed;
        agent.usesPursueInChase = usesPursue;
        agent.evadesWhenCornered = evadesWhenCornered;
        agent.searchDuration = style == AdvancedEnemyAgent.EnemyStyle.SlowListener ? 3.5f : 2f;
        agent.pathRefreshInterval = style == AdvancedEnemyAgent.EnemyStyle.FastLowHearing ? 0.28f : 0.45f;
        agent.attackRange = 1f;
        agent.evadeDistance = 1.55f;
        agent.pathObstacleMask = LayerMask.GetMask("Default", "Obstacle");
        agent.visionBlockMask = ~0;

        root.AddComponent<EnemyCollision>();

        CreateBody(root.transform, bodyMaterial);
        CreateHead(root.transform, darkMaterial, eyeMaterial, style);

        string path = PrefabFolder + "/" + prefabName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    private static void CreateBody(Transform parent, Material bodyMaterial)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Visual_Body";
        body.transform.SetParent(parent);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localRotation = Quaternion.identity;
        body.transform.localScale = Vector3.one;
        body.GetComponent<MeshRenderer>().sharedMaterial = bodyMaterial;
        Object.DestroyImmediate(body.GetComponent<Collider>());
    }

    private static void CreateHead(Transform parent, Material darkMaterial, Material eyeMaterial, AdvancedEnemyAgent.EnemyStyle style)
    {
        GameObject hood = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hood.name = "Visual_Hood";
        hood.transform.SetParent(parent);
        hood.transform.localPosition = new Vector3(0f, 1.25f, 0.05f);
        hood.transform.localScale = new Vector3(0.58f, 0.42f, 0.58f);
        hood.GetComponent<MeshRenderer>().sharedMaterial = darkMaterial;
        Object.DestroyImmediate(hood.GetComponent<Collider>());

        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eye.name = "Visual_SightMarker";
        eye.transform.SetParent(parent);
        eye.transform.localPosition = new Vector3(0f, 1.25f, 0.48f);
        eye.transform.localScale = style == AdvancedEnemyAgent.EnemyStyle.SlowListener
            ? new Vector3(0.45f, 0.08f, 0.08f)
            : new Vector3(0.6f, 0.08f, 0.08f);
        eye.GetComponent<MeshRenderer>().sharedMaterial = eyeMaterial;
        Object.DestroyImmediate(eye.GetComponent<Collider>());

        if (style == AdvancedEnemyAgent.EnemyStyle.SlowListener || style == AdvancedEnemyAgent.EnemyStyle.Balanced)
        {
            CreateEar(parent, darkMaterial, -0.42f);
            CreateEar(parent, darkMaterial, 0.42f);
        }
    }

    private static void CreateEar(Transform parent, Material material, float x)
    {
        GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ear.name = x < 0f ? "Visual_LeftListenerMark" : "Visual_RightListenerMark";
        ear.transform.SetParent(parent);
        ear.transform.localPosition = new Vector3(x, 1.17f, 0.05f);
        ear.transform.localScale = new Vector3(0.16f, 0.28f, 0.16f);
        ear.GetComponent<MeshRenderer>().sharedMaterial = material;
        Object.DestroyImmediate(ear.GetComponent<Collider>());
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        return material;
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
