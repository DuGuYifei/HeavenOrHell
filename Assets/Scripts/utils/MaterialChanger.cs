#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class MaterialChanger : MonoBehaviour
{
    [SerializeField] private Material materialToApply;
    [SerializeField] private GameObject objectToChange;


    public void ChangeMaterial()
    {
        var renderers = objectToChange.GetComponentsInChildren<Renderer>();
        foreach (var objRenderer in renderers) objRenderer.sharedMaterial = materialToApply;
        #if UNITY_EDITOR
        // PrefabUtility.ApplyPrefabInstance(objectToChange, InteractionMode.UserAction);
        #endif
    }

    public void SetZ()
    {
        var spriteRenderers = objectToChange.GetComponentsInChildren<SpriteRenderer>();
        foreach (var spriteRenderer in spriteRenderers)
        {
            if (!spriteRenderer.enabled) continue;
            var currentPosition = spriteRenderer.transform.position;
            spriteRenderer.transform.position = new Vector3(currentPosition.x, currentPosition.y, -(float)((double)spriteRenderer.sortingOrder / 600.0));
            Debug.Log($"{spriteRenderer.gameObject.name}: {spriteRenderer.sortingOrder}, new Z: {(float)((double)spriteRenderer.sortingOrder / 600.0)}, {spriteRenderer.transform.position.z}, {spriteRenderer.transform.localPosition.z} ", spriteRenderer.gameObject);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MaterialChanger))]
public class MaterialChangerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var materialChanger = (MaterialChanger)target;
        if (GUILayout.Button("Change Material")) materialChanger.ChangeMaterial();
        if (GUILayout.Button("Set Z")) materialChanger.SetZ();
    }
}
#endif