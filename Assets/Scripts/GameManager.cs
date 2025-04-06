using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool DebugMode = false;
    public bool isDragging = false;
    public bool menuShown = false;
    public bool isStarted = false;
    public bool CraftMode = false;

    [Header("Crafting Settings")]
    public GameObject craftModeColliderPrefab;


    [Header("Craft Recipes")]
    public List<CraftRecipe> craftRecipes;

    [Header("Cursor Settings")]
    public Texture2D craftCursor;
    public Vector2 craftCursorHotspot = new Vector2(50, 50);
    public Texture2D defaultCursor;

    [HideInInspector]
    public GameObject currentCraftModeCollider;

    [HideInInspector]
    public List<ICraftableBall> selectedBalls = new List<ICraftableBall>();

    private GameObject currentCraftPreview;

    private CameraController camControl;
    private bool craftOnCooldown = false;

    public bool isInTransition = false;
    public bool isFinalizingCraft = false;


    private LineRenderer craftLineRenderer;
    public float lineFadeDuration = 1.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        camControl = Camera.main.GetComponent<CameraController>();
        if (!DebugMode)
            camControl.enabled = false;
        else
            Camera.main.transform.position = new Vector3(0, 0, -10);



        GameObject lineObj = new GameObject("CraftLine");
        craftLineRenderer = lineObj.AddComponent<LineRenderer>();
        craftLineRenderer.positionCount = 0;
        craftLineRenderer.startWidth = 0.1f;
        craftLineRenderer.endWidth = 0.1f;
        craftLineRenderer.material = new Material(Shader.Find("Sprites/Default")); 
        craftLineRenderer.startColor = Color.white;
        craftLineRenderer.endColor = Color.white;
    }

    private void Update()
    {
        if (CraftMode || isInTransition) UpdateCraftLine();


        if (Input.GetKeyDown(KeyCode.Space))
        {

            if (isInTransition) return;

            if (craftOnCooldown)
            {
                Debug.Log("[GameManager] CraftMode is on cooldown.");
                return;
            }

            if (!CraftMode)
            {

                CraftMode = true;
                Debug.Log("[GameManager] CraftMode activated.");
                Cursor.SetCursor(craftCursor, craftCursorHotspot, CursorMode.Auto);
            }
            else
            {
                if (selectedBalls.Count == 0)
                {
                    Debug.Log("[GameManager] CraftMode cancel triggered because no balls are selected. Count: " + selectedBalls.Count);
                    Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
                    CraftMode = false;
                }
                else if (selectedBalls.Count < 2)
                {
                    Debug.Log("[GameManager] CraftMode cancel triggered because less than 2 balls are selected. Count: " + selectedBalls.Count);
                    CancelCraftMode();
                }

                else
                {
                    if (!isFinalizingCraft)
                    {
                        Debug.Log("[GameManager] Finalizing CraftMode with " + selectedBalls.Count + " balls.");
                        StartCoroutine(FinalizeCrafting());
                        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
                    }
                    else
                    {
                        Debug.Log("[GameManager] FinalizeCrafting is already running. Ignoring new request.");
                    }
                }
            }
        }


        if (DebugMode)
            return;
        if (menuShown && camControl.enabled)
            camControl.enabled = false;
        else if (!menuShown && !camControl.enabled)
            camControl.enabled = true;
    }

    private IEnumerator StartCraftCooldown()
    {
        craftOnCooldown = true;
        Debug.Log("[GameManager] CraftMode cooldown started.");
        yield return new WaitForSeconds(1.5f);
        craftOnCooldown = false;
        isInTransition = false;
        Debug.Log("[GameManager] CraftMode cooldown finished.");
    }

    public void UpdateCraftLine()
    {
        if (selectedBalls.Count < 2)
        {
            craftLineRenderer.positionCount = 0;
            return;
        }

        List<Vector3> points = new List<Vector3>();
        foreach (ICraftableBall ball in selectedBalls)
        {
            MonoBehaviour mb = ball as MonoBehaviour;
            if (mb != null)
                points.Add(mb.transform.position);
        }

        List<Vector3> hull = ComputeConvexHull(points);

        if (hull.Count > 0)
            hull.Add(hull[0]);

        craftLineRenderer.positionCount = hull.Count;
        for (int i = 0; i < hull.Count; i++)
        {
            craftLineRenderer.SetPosition(i, hull[i]);
        }
    }


    private CraftRecipe GetMatchingRecipe()
    {
        Dictionary<string, int> composition = new Dictionary<string, int>();
        foreach (ICraftableBall ball in selectedBalls)
        {
            string type = ball.CraftBallType;
            if (composition.ContainsKey(type))
                composition[type]++;
            else
                composition[type] = 1;
        }

        foreach (CraftRecipe recipe in craftRecipes)
        {
            if (Matches(recipe, composition))
            {
                Debug.Log("[GameManager] Matching recipe found: " + recipe.recipeName);
                return recipe;
            }
        }
        Debug.Log("[GameManager] No matching recipe found for composition:");
        foreach (var kvp in composition)
            Debug.Log("   " + kvp.Key + " : " + kvp.Value);
        return null;
    }

    private bool Matches(CraftRecipe recipe, Dictionary<string, int> composition)
    {
        if (composition.Count != recipe.requirements.Count)
            return false;
        foreach (BallRequirement req in recipe.requirements)
        {
            if (!composition.ContainsKey(req.ballType))
                return false;
            if (composition[req.ballType] != req.count)
                return false;
        }
        return true;
    }

    public void UpdateCraftPreview()
    {
        CraftRecipe matchingRecipe = GetMatchingRecipe();
        if (matchingRecipe == null)
        {
            if (currentCraftPreview != null)
            {
                Debug.Log("[GameManager] Removing craft preview as no recipe matches.");
                Destroy(currentCraftPreview);
                currentCraftPreview = null;
            }
            return;
        }

        Vector3 center = Vector3.zero;
        foreach (ICraftableBall ball in selectedBalls)
            center += ball.Transform.position;
        center /= selectedBalls.Count;
        Debug.Log("[GameManager] Craft preview center: " + center);

        if (currentCraftPreview != null)
        {
            if (currentCraftPreview.name != matchingRecipe.previewPrefab.name + "(Clone)")
            {
                Debug.Log("[GameManager] Craft preview type changed. Replacing preview.");
                Destroy(currentCraftPreview);
                currentCraftPreview = Instantiate(matchingRecipe.previewPrefab, center, Quaternion.identity);
            }
            else
            {
                currentCraftPreview.transform.position = center;
                Debug.Log("[GameManager] Updated craft preview position.");
            }
        }
        else
        {
            currentCraftPreview = Instantiate(matchingRecipe.previewPrefab, center, Quaternion.identity);
            Debug.Log("[GameManager] Instantiated new craft preview.");
        }
    }

    private IEnumerator FinalizeCrafting()
    {
        isFinalizingCraft = true;
        Debug.Log("[GameManager] Finalizing crafting.");
        isInTransition = true;
        Vector3 center = Vector3.zero;
        foreach (ICraftableBall ball in selectedBalls)
            center += ball.Transform.position;
        center /= selectedBalls.Count;
        Debug.Log("[GameManager] Calculated center for crafting: " + center);

        CraftRecipe matchingRecipe = GetMatchingRecipe();
        if (matchingRecipe == null)
        {
            Debug.Log("[GameManager] No valid recipe for selected balls. Canceling CraftMode.");
            CancelCraftMode();
            yield break;
        }

        if (currentCraftModeCollider != null)
        {
            Animator anim = currentCraftModeCollider.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger("Hide");
                Debug.Log("[GameManager] Triggered 'Hide' on CraftModeCollider.");
            }
        }
        foreach (ICraftableBall ball in selectedBalls)
        {
            ball.CancelCraftingVisual();
        }

        if (currentCraftPreview != null)
        {
            Destroy(currentCraftPreview);
            currentCraftPreview = null;
            Debug.Log("[GameManager] Removed craft preview before moving balls.");
        }
        StartCoroutine(FadeOutCraftLine());

        foreach (ICraftableBall ball in selectedBalls)
        {
            MonoBehaviour mb = ball as MonoBehaviour;
            if (mb != null)
            {
                if (mb is RedBall)
                    ((RedBall)mb).currentState = RedBall.RedBallState.CraftingMoving;

                Animator anim = mb.GetComponent<Animator>();
                if (anim != null)
                {
                    anim.SetTrigger("Craft");
                }
                mb.transform.DOMove(center, 1f).SetEase(DG.Tweening.Ease.OutQuad);
                Debug.Log("[GameManager] Moving ball " + mb.gameObject.name + " to center via tween.");
            }
        }

        yield return new WaitForSeconds(1.30f);

        GameObject finalObj = Instantiate(matchingRecipe.finalPrefab, center, Quaternion.identity);
        Debug.Log("[GameManager] Instantiated final crafted object at center: " + finalObj.name);

        foreach (ICraftableBall ball in selectedBalls)
        {
            MonoBehaviour mb = ball as MonoBehaviour;
            if (mb != null)
            {
                Debug.Log("[GameManager] Destroying ball: " + mb.gameObject.name);
                Destroy(mb.gameObject);
            }
        }

        selectedBalls.Clear();
        CraftMode = false;
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        Debug.Log("[GameManager] CraftMode finalized. Selected balls cleared.");

        if (currentCraftModeCollider != null)
        {
            Debug.Log("[GameManager] Destroying current CraftModeCollider.");
            Destroy(currentCraftModeCollider);
            currentCraftModeCollider = null;
        }
        isFinalizingCraft = false;
        StartCoroutine(StartCraftCooldown());
    }

    public void CancelCraftMode()
    {
        StartCoroutine(FadeOutCraftLine());

        Debug.Log("[GameManager] CancelCraftMode called.");
        foreach (ICraftableBall ball in selectedBalls)
        {
            Debug.Log("[GameManager] Cancelling crafting visual for ball: " + (ball as MonoBehaviour).gameObject.name);
            ball.CancelCraftingVisual();
        }
        selectedBalls.Clear();
        Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        CraftMode = false;
        Debug.Log("[GameManager] Selected balls cleared and CraftMode set to false.");

        if (currentCraftPreview != null)
        {
            Debug.Log("[GameManager] Removing craft preview.");
            Destroy(currentCraftPreview);
            currentCraftPreview = null;
        }

        if (currentCraftModeCollider != null)
        {
            Animator anim = currentCraftModeCollider.GetComponent<Animator>();
            if (anim != null)
            {
                Debug.Log("[GameManager] Triggering 'Cancel' animation on CraftModeCollider.");
                anim.SetTrigger("Cancel");
            }
            Destroy(currentCraftModeCollider, 1.3f);
            Debug.Log("[GameManager] CraftModeCollider will be destroyed after 1.3 seconds.");
            currentCraftModeCollider = null;
        }
        isFinalizingCraft = false;

        StartCoroutine(StartCraftCooldown());
    }

    public void RegisterSelectedBall(ICraftableBall ball)
    {
        if (!selectedBalls.Contains(ball))
        {
            selectedBalls.Add(ball);
            Debug.Log("[GameManager] Registered ball for crafting: " + (ball as MonoBehaviour).gameObject.name + ". Total selected: " + selectedBalls.Count);
            UpdateCraftPreview();
            UpdateCraftLine();
        }
    }

    public void UnregisterSelectedBall(ICraftableBall ball)
    {
        if (selectedBalls.Contains(ball))
        {
            selectedBalls.Remove(ball);
            Debug.Log("[GameManager] Unregistered ball for crafting: " + (ball as MonoBehaviour).gameObject.name + ". Total selected: " + selectedBalls.Count);
            UpdateCraftPreview();
            UpdateCraftLine();
        }
    }

    private IEnumerator FadeOutCraftLine()
    {
        if (craftLineRenderer == null)
            yield break;
        Material mat = craftLineRenderer.material;
        Color startColor = mat.color;
        float elapsed = 0f;
        while (elapsed < lineFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / lineFadeDuration);
            Color newColor = new Color(startColor.r, startColor.g, startColor.b, alpha);
            mat.color = newColor;
            yield return null;
        }
        craftLineRenderer.positionCount = 0;
        mat.color = startColor;
    }


    private List<Vector3> ComputeConvexHull(List<Vector3> points)
    {
        if (points.Count <= 1)
            return new List<Vector3>(points);

        List<Vector2> pts2D = points.Select(p => new Vector2(p.x, p.y)).ToList();

        pts2D.Sort((a, b) =>
        {
            if (a.x == b.x)
                return a.y.CompareTo(b.y);
            return a.x.CompareTo(b.x);
        });

        List<Vector2> lower = new List<Vector2>();
        foreach (var p in pts2D)
        {
            while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
            {
                lower.RemoveAt(lower.Count - 1);
            }
            lower.Add(p);
        }

        List<Vector2> upper = new List<Vector2>();
        for (int i = pts2D.Count - 1; i >= 0; i--)
        {
            Vector2 p = pts2D[i];
            while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
            {
                upper.RemoveAt(upper.Count - 1);
            }
            upper.Add(p);
        }

        lower.RemoveAt(lower.Count - 1);
        upper.RemoveAt(upper.Count - 1);

        // Combiner les listes
        List<Vector2> hull2D = new List<Vector2>();
        hull2D.AddRange(lower);
        hull2D.AddRange(upper);

        List<Vector3> hull3D = hull2D.Select(p => new Vector3(p.x, p.y, 0)).ToList();
        return hull3D;
    }

    private float Cross(Vector2 O, Vector2 A, Vector2 B)
    {
        return (A.x - O.x) * (B.y - O.y) - (A.y - O.y) * (B.x - O.x);
    }

}
