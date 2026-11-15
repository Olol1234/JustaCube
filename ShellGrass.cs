using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds shell-texture grass on this object's MeshRenderer.
/// A renderer with more materials than submeshes draws the mesh once more per extra material,
/// so this adds one copy of the shell material per layer, each with its own _LayerHeight.
/// The materials already on the renderer (the plain grass ground) stay underneath as the base.
/// Play mode only: the layer materials are created at runtime and destroyed again on disable.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class ShellGrass : MonoBehaviour
{
    [Tooltip("Material using the GrassShell shader. Copied once per layer, never changed itself.")]
    [SerializeField] private Material shellMaterial;

    [Tooltip("How many layers to stack. More layers look denser but cost more fill rate.")]
    [SerializeField, Range(2, 32)] private int layerCount = 16;

    private static readonly int LayerHeightId = Shader.PropertyToID("_LayerHeight");

    private MeshRenderer meshRenderer;
    private Material[] originalMaterials;          // what the renderer had before the layers
    private readonly List<Material> layers = new List<Material>();

    private void OnEnable()
    {
        Build();
    }

    private void OnDisable()
    {
        Teardown();
    }

    private void Build()
    {
        if (shellMaterial == null)
        {
            Debug.LogWarning("ShellGrass: no shell material assigned.", this);
            return;
        }

        // Guard against building twice (the second call starts from a clean state).
        Teardown();

        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        originalMaterials = meshRenderer.sharedMaterials;
        var all = new Material[originalMaterials.Length + layerCount];
        originalMaterials.CopyTo(all, 0);

        for (int i = 0; i < layerCount; i++)
        {
            var layer = new Material(shellMaterial) { name = $"{shellMaterial.name} Layer {i}" };

            // From just above the ground (> 0) up to the tip (1).
            layer.SetFloat(LayerHeightId, (i + 1) / (float)layerCount);

            layers.Add(layer);
            all[originalMaterials.Length + i] = layer;
        }

        meshRenderer.sharedMaterials = all;
    }

    private void Teardown()
    {
        // The renderer can already be gone while the app quits.
        if (meshRenderer != null && originalMaterials != null)
            meshRenderer.sharedMaterials = originalMaterials;

        foreach (var layer in layers)
            if (layer != null) Destroy(layer);

        layers.Clear();
        originalMaterials = null;
    }
}
