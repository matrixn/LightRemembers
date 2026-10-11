using UnityEngine;
using UnityEngine.Rendering;

namespace LightRemembers.Memory
{
    /// <summary>Low-cost visible cone and contact glow for the player's Memory Light.</summary>
    [DisallowMultipleComponent]
    public sealed class MemoryLightBeamVisual : MonoBehaviour
    {
        private const int ConeSegments = 32;
        private const float BeamEndRadiusScale = 0.3f;

        [SerializeField, Min(0.1f)] private float maximumRange = 19f;
        [SerializeField, Range(1f, 89f)] private float spotAngle = 28f;
        [SerializeField] private Transform lightOrigin;

        private Transform _beamTransform;
        private Mesh _beamMesh;
        private Material _beamMaterial;
        private Material _contactMaterial;
        private GameObject _contactSpot;
        private Light _contactLight;
        private bool _beamActive;
        private bool _contactActive;
        private float _beamLength;

        public bool BeamVisible => _beamActive;
        public bool ContactVisible => _contactActive;
        public float BeamLength => _beamLength;
        public Vector3 ContactPoint { get; private set; }

        public void Configure(Transform origin, float range, float angle)
        {
            lightOrigin = origin;
            maximumRange = Mathf.Max(0.1f, range);
            spotAngle = Mathf.Clamp(angle, 1f, 89f);
            if (_beamMesh != null)
                BuildConeMesh();
        }

        public void SetBeamState(bool active, float length, Vector3 contactPoint, Vector3 surfaceNormal, bool hasContact)
        {
            _beamActive = active;
            _beamLength = active ? Mathf.Clamp(length, 0.2f, maximumRange) : 0f;
            _contactActive = active && hasContact;
            ContactPoint = contactPoint;

            if (_beamTransform != null)
            {
                _beamTransform.gameObject.SetActive(active);
                if (active && lightOrigin != null)
                {
                    _beamTransform.SetPositionAndRotation(lightOrigin.position, lightOrigin.rotation);
                    _beamTransform.localScale = new Vector3(_beamLength, _beamLength, _beamLength);
                }
            }

            if (_contactSpot != null)
            {
                _contactSpot.SetActive(_contactActive);
                if (_contactActive)
                {
                    var normal = surfaceNormal.sqrMagnitude > 0.001f ? surfaceNormal.normalized : -transform.forward;
                    _contactSpot.transform.SetPositionAndRotation(contactPoint + normal * 0.015f,
                        Quaternion.LookRotation(normal));
                }
            }
        }

        private void Awake()
        {
            if (lightOrigin == null)
                lightOrigin = transform;
            CreateBeam();
            CreateContactSpot();
            SetBeamState(false, 0f, Vector3.zero, Vector3.up, false);
        }

        private void OnDestroy()
        {
            if (_beamMesh != null)
                Destroy(_beamMesh);
            if (_beamMaterial != null)
                Destroy(_beamMaterial);
            if (_contactMaterial != null)
                Destroy(_contactMaterial);
        }

        private void CreateBeam()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return;

            _beamMaterial = CreateTransparentMaterial(shader, "Memory Light Cone", new Color(0.42f, 0.8f, 1f, 0.22f));
            var beamObject = new GameObject("MemoryLight_VisibleCone", typeof(MeshFilter), typeof(MeshRenderer));
            _beamTransform = beamObject.transform;
            _beamTransform.SetParent(transform, false);
            var filter = beamObject.GetComponent<MeshFilter>();
            _beamMesh = new Mesh { name = "MemoryLightConeMesh" };
            _beamMesh.MarkDynamic();
            BuildConeMesh();
            filter.sharedMesh = _beamMesh;
            var renderer = beamObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _beamMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void CreateContactSpot()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader == null)
                return;

            _contactMaterial = CreateTransparentMaterial(shader, "Memory Light Contact Glow",
                new Color(0.55f, 0.92f, 1f, 0.78f));
            _contactSpot = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _contactSpot.name = "MemoryLight_ContactGlow";
            _contactSpot.transform.SetParent(transform, false);
            _contactSpot.transform.localScale = new Vector3(0.28f, 0.28f, 1f);
            var collider = _contactSpot.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            var renderer = _contactSpot.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _contactMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var lightObject = new GameObject("MemoryLight_ContactIllumination", typeof(Light));
            lightObject.transform.SetParent(_contactSpot.transform, false);
            lightObject.transform.localPosition = Vector3.zero;
            _contactLight = lightObject.GetComponent<Light>();
            _contactLight.type = LightType.Point;
            _contactLight.color = new Color(0.4f, 0.82f, 1f);
            _contactLight.intensity = 1.15f;
            _contactLight.range = 2.4f;
            _contactLight.shadows = LightShadows.None;
        }

        private void BuildConeMesh()
        {
            if (_beamMesh == null)
                return;

            // One smooth triangular fan avoids the visible concentric bands produced by
            // stacked transparent rings when the camera looks down the beam.
            var vertices = new Vector3[ConeSegments + 1];
            var colors = new Color[vertices.Length];
            var triangles = new int[ConeSegments * 3];
            vertices[0] = Vector3.zero;
            colors[0] = new Color(1f, 1f, 1f, 0.42f);
            var baseRadius = Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * BeamEndRadiusScale;
            for (var i = 0; i < ConeSegments; i++)
            {
                var angle = i * Mathf.PI * 2f / ConeSegments;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var outer = 1 + i;
                vertices[outer] = direction * baseRadius + Vector3.forward;
                colors[outer] = new Color(1f, 1f, 1f, 0.025f);

                var next = (i + 1) % ConeSegments;
                var triangle = i * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = 1 + next;
                triangles[triangle + 2] = outer;
            }

            _beamMesh.Clear();
            _beamMesh.vertices = vertices;
            _beamMesh.colors = colors;
            _beamMesh.triangles = triangles;
            _beamMesh.RecalculateNormals();
            _beamMesh.RecalculateBounds();
        }

        private static Material CreateTransparentMaterial(Shader shader, string materialName, Color tint)
        {
            var material = new Material(shader) { name = materialName, renderQueue = (int)RenderQueue.Transparent };
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            return material;
        }
    }
}
