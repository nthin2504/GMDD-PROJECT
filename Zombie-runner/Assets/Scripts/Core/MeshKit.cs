using UnityEngine;
using UnityEngine.Rendering;

namespace ZombieRunner
{
    /// <summary>
    /// Tiny helper for assembling procedural props out of primitives.
    /// All renderers share the engine's built-in meshes, so dozens of props cost
    /// almost nothing, and no colliders are created (the game is 100% kinematic).
    /// </summary>
    public static class MeshKit
    {
        static Mesh s_cube;
        static Mesh s_sphere;
        static Mesh s_cylinder;

        public static Mesh CubeMesh
        {
            get
            {
                if (s_cube == null) s_cube = GrabPrimitiveMesh(PrimitiveType.Cube);
                return s_cube;
            }
        }

        public static Mesh SphereMesh
        {
            get
            {
                if (s_sphere == null) s_sphere = GrabPrimitiveMesh(PrimitiveType.Sphere);
                return s_sphere;
            }
        }

        public static Mesh CylinderMesh
        {
            get
            {
                if (s_cylinder == null) s_cylinder = GrabPrimitiveMesh(PrimitiveType.Cylinder);
                return s_cylinder;
            }
        }

        static Mesh GrabPrimitiveMesh(PrimitiveType type)
        {
            GameObject temp = GameObject.CreatePrimitive(type);
            MeshFilter filter = temp.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            // hide immediately so the temp object never pops into view for a frame
            temp.SetActive(false);
            Object.Destroy(temp);
            return mesh;
        }

        /// <summary>Creates a coloured box. Size is in metres, position is local.</summary>
        public static GameObject Box(Transform parent, string name, Vector3 size, Vector3 localPos,
                                     Material mat, bool castShadow = true, bool receiveShadow = true)
        {
            return Primitive(parent, name, CubeMesh, size, localPos, Quaternion.identity, mat, castShadow, receiveShadow);
        }

        public static GameObject Box(Transform parent, string name, Vector3 size, Vector3 localPos,
                                     Quaternion localRot, Material mat, bool castShadow = true, bool receiveShadow = true)
        {
            return Primitive(parent, name, CubeMesh, size, localPos, localRot, mat, castShadow, receiveShadow);
        }

        public static GameObject Sphere(Transform parent, string name, float diameter, Vector3 localPos, Material mat,
                                        bool castShadow = false, bool receiveShadow = false)
        {
            return Primitive(parent, name, SphereMesh, Vector3.one * diameter, localPos, Quaternion.identity, mat, castShadow, receiveShadow);
        }

        public static GameObject Cylinder(Transform parent, string name, float diameter, float height,
                                          Vector3 localPos, Quaternion localRot, Material mat,
                                          bool castShadow = true, bool receiveShadow = true)
        {
            return Primitive(parent, name, CylinderMesh, new Vector3(diameter, height, diameter), localPos, localRot, mat, castShadow, receiveShadow);
        }

        static GameObject Primitive(Transform parent, string name, Mesh mesh, Vector3 size, Vector3 localPos,
                                    Quaternion localRot, Material mat, bool castShadow, bool receiveShadow)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = size;

            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = castShadow ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadow;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        /// <summary>An empty transform (used as an animation pivot).</summary>
        public static Transform Pivot(Transform parent, string name, Vector3 localPos)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go.transform;
        }
    }
}
