using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CircularStairReveal : MonoBehaviour
{
    private const string GeneratedRootName = "GeneratedCircularStairs";

    [Header("Ksztalt")]
    public int stepCount = 28;
    public int arcSegmentsPerStep = 4;
    public float innerRadius = 1.6f;
    public float outerRadius = 5.5f;
    public float stepThickness = 0.22f;
    public float angularGap = 1f;
    public float startAngle = 0f;

    [Header("Schody")]
    public float totalDepth = 4.5f;
    public float firstStepDrop = 0.3f;
    public bool clockwiseDescent = true;

    [Header("Animacja")]
    public bool closeOnPlay = true;
    public float openDuration = 3f;
    public float stepDelay = 0.035f;
    public AnimationCurve openCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Wyglad")]
    public Material stepMaterial;
    public bool addMeshColliders = true;
    public bool rebuildAutomatically = true;
    public bool rebuildOnPlay = true;
    public bool saveGeneratedObjectsInScene = false;

    [Header("Podglad w edytorze")]
    [Range(0f, 1f)]
    public float previewOpenAmount;

    [Header("Stara pokrywa do schowania")]
    public GameObject[] coverObjectsToHideWhenOpen;
    public bool hideCoverAtStartOfOpening = true;

    private readonly List<Transform> steps = new List<Transform>();
    private Coroutine animationRoutine;
    private Transform generatedRoot;
    private bool isOpen;
#if UNITY_EDITOR
    private bool editorRefreshQueued;
#endif

    private void Reset()
    {
        ClampSettings();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            QueueEditorRefresh();
        else
            Rebuild();
#else
        Rebuild();
#endif
    }

    private void Awake()
    {
        if (!generatedRoot)
            generatedRoot = transform.Find(GeneratedRootName);

        CacheSteps();

        if (steps.Count == 0)
            Rebuild();
    }

    private void Start()
    {
        if (!Application.isPlaying)
            return;

        CacheSteps();

        if (rebuildOnPlay || steps.Count == 0)
            Rebuild();

        if (closeOnPlay)
        {
            isOpen = false;
            ApplyPose(0f);
            SetCoverObjectsVisible(true);
        }
    }

    private void OnValidate()
    {
        ClampSettings();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            QueueEditorRefresh();
#endif
    }

#if UNITY_EDITOR
    private void QueueEditorRefresh()
    {
        if (editorRefreshQueued)
            return;

        editorRefreshQueued = true;
        UnityEditor.EditorApplication.delayCall += RefreshInEditor;
    }

    private void RefreshInEditor()
    {
        if (!this)
            return;

        editorRefreshQueued = false;

        if (Application.isPlaying)
            return;

        if (rebuildAutomatically)
            Rebuild();
        else
            ApplyPose(previewOpenAmount);
    }
#endif

    [ContextMenu("Rebuild Stairs")]
    public void Rebuild()
    {
        ClampSettings();
        ClearGeneratedRoot();
        CreateGeneratedRoot();

        float stepAngle = 360f / stepCount;

        for (int i = 0; i < stepCount; i++)
        {
            float fromAngle = startAngle + i * stepAngle + angularGap * 0.5f;
            float toAngle = startAngle + (i + 1) * stepAngle - angularGap * 0.5f;
            CreateStep(i, fromAngle, toAngle);
        }

        CacheSteps();
        ApplyPose(Application.isPlaying ? (isOpen ? 1f : 0f) : previewOpenAmount);
    }

    [ContextMenu("Remove Generated Stairs")]
    public void RemoveGeneratedStairs()
    {
        ClearGeneratedRoot();
        steps.Clear();
    }

    [ContextMenu("Open")]
    public void Open()
    {
        SetOpen(true);
    }

    [ContextMenu("Close")]
    public void Close()
    {
        SetOpen(false);
    }

    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;

        if (open && hideCoverAtStartOfOpening)
            SetCoverObjectsVisible(false);

        if (!open)
            SetCoverObjectsVisible(true);

        if (!Application.isPlaying)
        {
            previewOpenAmount = open ? 1f : 0f;
            ApplyPose(previewOpenAmount);

            if (open && !hideCoverAtStartOfOpening)
                SetCoverObjectsVisible(false);

            return;
        }

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        animationRoutine = StartCoroutine(AnimateOpen(open));
    }

    private IEnumerator AnimateOpen(bool open)
    {
        float timer = 0f;
        float fullDuration = Mathf.Max(0.01f, openDuration + Mathf.Max(0f, stepDelay) * (stepCount - 1));

        while (timer < fullDuration)
        {
            timer += Time.deltaTime;

            for (int i = 0; i < steps.Count; i++)
            {
                float delay = stepDelay * i;
                float rawStepProgress = Mathf.Clamp01((timer - delay) / Mathf.Max(0.01f, openDuration));
                float curvedProgress = openCurve.Evaluate(rawStepProgress);
                SetStepPose(i, open ? curvedProgress : 1f - curvedProgress);
            }

            yield return null;
        }

        ApplyPose(open ? 1f : 0f);

        if (open && !hideCoverAtStartOfOpening)
            SetCoverObjectsVisible(false);

        animationRoutine = null;
    }

    private void ApplyPose(float progress)
    {
        CacheSteps();

        for (int i = 0; i < steps.Count; i++)
            SetStepPose(i, progress);
    }

    private void SetStepPose(int index, float progress)
    {
        if (index < 0 || index >= steps.Count || !steps[index])
            return;

        int totalSteps = Mathf.Max(1, steps.Count);
        int descentIndex = clockwiseDescent ? index : totalSteps - 1 - index;
        float normalizedDepth = (float)(descentIndex + 1) / totalSteps;
        float depth = Mathf.Max(firstStepDrop, totalDepth * normalizedDepth);
        float y = -depth * progress;

        steps[index].localPosition = new Vector3(0f, y, 0f);
        steps[index].localRotation = Quaternion.identity;
    }

    private void CreateGeneratedRoot()
    {
        GameObject rootObject = new GameObject(GeneratedRootName);
        rootObject.hideFlags = GetGeneratedHideFlags();
        rootObject.transform.SetParent(transform, false);
        generatedRoot = rootObject.transform;
    }

    private void ClearGeneratedRoot()
    {
        List<Transform> oldRoots = new List<Transform>();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == GeneratedRootName)
                oldRoots.Add(child);
        }

        foreach (Transform oldRoot in oldRoots)
        {
            if (Application.isPlaying)
            {
                oldRoot.gameObject.SetActive(false);
                Destroy(oldRoot.gameObject);
            }
            else
            {
                DestroyImmediate(oldRoot.gameObject);
            }
        }

        generatedRoot = null;
    }

    private void CreateStep(int index, float fromAngle, float toAngle)
    {
        GameObject stepObject = new GameObject("Step_" + index.ToString("00"));
        stepObject.hideFlags = GetGeneratedHideFlags();
        stepObject.transform.SetParent(generatedRoot, false);

        MeshFilter meshFilter = stepObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = stepObject.AddComponent<MeshRenderer>();

        Mesh mesh = BuildStepMesh(fromAngle, toAngle);
        mesh.name = "CircularStairStep_" + index.ToString("00");
        mesh.hideFlags = GetGeneratedHideFlags();
        meshFilter.sharedMesh = mesh;

        if (stepMaterial)
            meshRenderer.sharedMaterial = stepMaterial;

        if (addMeshColliders)
        {
            MeshCollider meshCollider = stepObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }
    }

    private Mesh BuildStepMesh(float fromAngle, float toAngle)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int arcSteps = Mathf.Max(1, arcSegmentsPerStep);
        Vector3[] topInner = new Vector3[arcSteps + 1];
        Vector3[] topOuter = new Vector3[arcSteps + 1];
        Vector3[] bottomInner = new Vector3[arcSteps + 1];
        Vector3[] bottomOuter = new Vector3[arcSteps + 1];

        for (int i = 0; i <= arcSteps; i++)
        {
            float t = (float)i / arcSteps;
            float angle = Mathf.Lerp(fromAngle, toAngle, t) * Mathf.Deg2Rad;
            Vector3 innerPoint = new Vector3(Mathf.Cos(angle) * innerRadius, 0f, Mathf.Sin(angle) * innerRadius);
            Vector3 outerPoint = new Vector3(Mathf.Cos(angle) * outerRadius, 0f, Mathf.Sin(angle) * outerRadius);

            topInner[i] = innerPoint;
            topOuter[i] = outerPoint;
            bottomInner[i] = innerPoint + Vector3.down * stepThickness;
            bottomOuter[i] = outerPoint + Vector3.down * stepThickness;
        }

        for (int i = 0; i < arcSteps; i++)
        {
            AddQuad(vertices, triangles, topInner[i], topInner[i + 1], topOuter[i + 1], topOuter[i]);
            AddQuad(vertices, triangles, bottomInner[i], bottomOuter[i], bottomOuter[i + 1], bottomInner[i + 1]);
            AddQuad(vertices, triangles, topOuter[i], topOuter[i + 1], bottomOuter[i + 1], bottomOuter[i]);
            AddQuad(vertices, triangles, topInner[i + 1], topInner[i], bottomInner[i], bottomInner[i + 1]);
        }

        AddQuad(vertices, triangles, topInner[0], topOuter[0], bottomOuter[0], bottomInner[0]);
        AddQuad(vertices, triangles, topOuter[arcSteps], topInner[arcSteps], bottomInner[arcSteps], bottomOuter[arcSteps]);

        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        int first = vertices.Count;
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        vertices.Add(d);

        triangles.Add(first);
        triangles.Add(first + 1);
        triangles.Add(first + 2);
        triangles.Add(first);
        triangles.Add(first + 2);
        triangles.Add(first + 3);
    }

    private void CacheSteps()
    {
        steps.Clear();

        if (!generatedRoot)
            generatedRoot = transform.Find(GeneratedRootName);

        if (!generatedRoot)
            return;

        for (int i = 0; i < generatedRoot.childCount; i++)
            steps.Add(generatedRoot.GetChild(i));
    }

    private void ClampSettings()
    {
        stepCount = Mathf.Max(2, stepCount);
        arcSegmentsPerStep = Mathf.Max(1, arcSegmentsPerStep);
        innerRadius = Mathf.Max(0f, innerRadius);
        outerRadius = Mathf.Max(innerRadius + 0.1f, outerRadius);
        stepThickness = Mathf.Max(0.01f, stepThickness);
        angularGap = Mathf.Clamp(angularGap, 0f, 360f / stepCount - 0.1f);
        totalDepth = Mathf.Max(0f, totalDepth);
        openDuration = Mathf.Max(0.01f, openDuration);
        stepDelay = Mathf.Max(0f, stepDelay);
        previewOpenAmount = Mathf.Clamp01(previewOpenAmount);
        firstStepDrop = Mathf.Max(0f, firstStepDrop);
    }

    private void SetCoverObjectsVisible(bool visible)
    {
        if (coverObjectsToHideWhenOpen == null)
            return;

        foreach (GameObject coverObject in coverObjectsToHideWhenOpen)
        {
            if (coverObject)
                coverObject.SetActive(visible);
        }
    }

    private HideFlags GetGeneratedHideFlags()
    {
        if (saveGeneratedObjectsInScene)
            return HideFlags.None;

        return HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
    }
}
