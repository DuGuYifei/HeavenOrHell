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
    }
}
#endif