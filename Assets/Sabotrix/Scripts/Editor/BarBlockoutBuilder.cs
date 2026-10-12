using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sabotrix.Editor
{
    // Builds the gray-box bar (M2.0) out of primitives and saves it as the Bar prefab.
    // Rerunning overwrites the prefab in place, so scenes that use it pick up the changes.
    // Props are named so later code can find them by name: LightBox, BreakerBox, Beholder, Drink_P1, Seat_P1, ...
    // Units are meters. The table sits at the origin. Player 1 faces +Z, Player 2 faces -Z.
    public static class BarBlockoutBuilder
    {
        private const string PrefabPath = "Assets/Sabotrix/Prefabs/Bar.prefab";
        private const string MaterialFolder = "Assets/Sabotrix/Art/Blockout";
        private const string LitShader = "Universal Render Pipeline/Lit";

        private const float RoomWidth = 10f;
        private const float RoomDepth = 8f;
        private const float RoomHeight = 4f;
        private const float WallThickness = 0.2f;
        private const float TrussHeight = 3.6f;
        private const float TableHeight = 0.75f;
        private const float SeatedEyeHeight = 1.2f;
        private const float SeatDistance = 0.85f;
        private const int HazardStripes = 10;

        [MenuItem("Sabotrix/Build Bar Blockout")]
        public static void Build()
        {
            Directory.CreateDirectory(MaterialFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();

            GameObject root = new GameObject("Bar");
            BuildRoom(root.transform);
            BuildTable(root.transform);
            BuildPlayerSide(root.transform, 1, -1f);
            BuildPlayerSide(root.transform, 2, 1f);
            BuildLightBox(root.transform);
            BuildBeholder(root.transform);
            BuildWallProps(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"Bar blockout saved to {PrefabPath}");
        }

        private static void BuildRoom(Transform parent)
        {
            Transform room = Group("Room", parent);
            Material brick = GetMaterial("Brick", new Color(0.55f, 0.22f, 0.16f));
            Material steel = GetMaterial("RedSteel", new Color(0.7f, 0.1f, 0.08f));
            Material pipe = GetMaterial("Pipe", new Color(0.45f, 0.45f, 0.48f));

            Box("Floor", room, new Vector3(0, -0.05f, 0), new Vector3(RoomWidth, 0.1f, RoomDepth), GetMaterial("FloorWood", new Color(0.3f, 0.2f, 0.13f)));
            Box("Ceiling", room, new Vector3(0, RoomHeight, 0), new Vector3(RoomWidth, 0.1f, RoomDepth), GetMaterial("Ceiling", new Color(0.12f, 0.12f, 0.13f)));
            Box("Wall_Back", room, new Vector3(0, RoomHeight / 2, RoomDepth / 2), new Vector3(RoomWidth, RoomHeight, WallThickness), brick);
            Box("Wall_Front", room, new Vector3(0, RoomHeight / 2, -RoomDepth / 2), new Vector3(RoomWidth, RoomHeight, WallThickness), brick);
            Box("Wall_Left", room, new Vector3(-RoomWidth / 2, RoomHeight / 2, 0), new Vector3(WallThickness, RoomHeight, RoomDepth), brick);
            Box("Wall_Right", room, new Vector3(RoomWidth / 2, RoomHeight / 2, 0), new Vector3(WallThickness, RoomHeight, RoomDepth), brick);

            // Exposed red steel trusses across the ceiling, with pipes running the other way.
            foreach (float z in new[] { -2f, 0f, 2f })
            {
                Box($"Truss_{z:0}", room, new Vector3(0, TrussHeight, z), new Vector3(RoomWidth, 0.25f, 0.2f), steel);
            }

            foreach (float x in new[] { -3f, 3f })
            {
                Cylinder($"Pipe_{x:0}", room, new Vector3(x, TrussHeight + 0.2f, 0), new Vector3(0.15f, RoomDepth / 2, 0.15f), pipe, new Vector3(90, 0, 0));
            }
        }

        private static void BuildTable(Transform parent)
        {
            Transform table = Group("Table", parent);
            Material wood = GetMaterial("TableWood", new Color(0.45f, 0.28f, 0.15f));
            Box("Top", table, new Vector3(0, TableHeight, 0), new Vector3(1.2f, 0.06f, 1f), wood);
            Cylinder("Leg", table, new Vector3(0, TableHeight / 2, 0), new Vector3(0.12f, TableHeight / 2, 0.12f), wood);
        }

        // side: -1 puts this player on the -Z side of the table (facing +Z), 1 on the +Z side.
        private static void BuildPlayerSide(Transform parent, int player, float side)
        {
            Material chair = GetMaterial("Chair", new Color(0.1f, 0.1f, 0.1f));
            float facing = side < 0 ? 0 : 180;

            Transform chairRoot = Group($"Chair_P{player}", parent);
            chairRoot.localPosition = new Vector3(0, 0, side * SeatDistance);
            chairRoot.localEulerAngles = new Vector3(0, facing, 0);
            Box("Seat", chairRoot, new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.05f, 0.45f), chair);
            Box("Back", chairRoot, new Vector3(0, 0.72f, -0.22f), new Vector3(0.45f, 0.5f, 0.05f), chair);
            Box("Base", chairRoot, new Vector3(0, 0.22f, 0), new Vector3(0.08f, 0.44f, 0.08f), chair);

            // Where the seated player's eyes are. The first-person camera goes here in M2.1.
            Transform seat = Group($"Seat_P{player}", parent);
            seat.localPosition = new Vector3(0, SeatedEyeHeight, side * SeatDistance);
            seat.localEulerAngles = new Vector3(0, facing, 0);

            // Drink on the table, to this player's right.
            Cylinder($"Drink_P{player}", parent, new Vector3(-side * 0.4f, TableHeight + 0.1f, side * 0.35f), new Vector3(0.08f, 0.075f, 0.08f),
                GetMaterial("Drink", new Color(0.85f, 0.55f, 0.1f)));
        }

        // The pendant light box over the table. Blackout smashes it.
        private static void BuildLightBox(Transform parent)
        {
            Transform lightBox = Group("LightBox", parent);
            lightBox.localPosition = new Vector3(0, 2.4f, 0);
            Box("Box", lightBox, Vector3.zero, new Vector3(0.8f, 0.2f, 0.5f), GetMaterial("LightBox", new Color(1f, 0.85f, 0.55f), emissive: true));
            Cylinder("Chain", lightBox, new Vector3(0, (TrussHeight - 2.4f) / 2, 0), new Vector3(0.02f, (TrussHeight - 2.4f) / 2, 0.02f), GetMaterial("Chain", new Color(0.3f, 0.3f, 0.32f)));
        }

        // The beholder sculpture hanging on a chain. Beholder Drop swings it into the table.
        private static void BuildBeholder(Transform parent)
        {
            Transform beholder = Group("Beholder", parent);
            beholder.localPosition = new Vector3(-2.2f, 2.5f, 1.5f);
            Sphere("Body", beholder, Vector3.zero, Vector3.one, GetMaterial("Beholder", new Color(0.45f, 0.3f, 0.55f)));
            Sphere("Eye", beholder, new Vector3(0.2f, 0, -0.38f), Vector3.one * 0.35f, GetMaterial("EyeWhite", Color.white));
            Sphere("Pupil", beholder, new Vector3(0.27f, 0, -0.53f), Vector3.one * 0.12f, GetMaterial("Pupil", Color.black));
            Cylinder("Chain", beholder, new Vector3(0, 0.8f, 0), new Vector3(0.03f, 0.3f, 0.03f), GetMaterial("Chain", new Color(0.3f, 0.3f, 0.32f)));
        }

        private static void BuildWallProps(Transform parent)
        {
            Material screen = GetMaterial("TvScreen", new Color(0.25f, 0.55f, 1f), emissive: true);
            float backWall = RoomDepth / 2 - WallThickness;
            Box("TV_1", parent, new Vector3(-2f, 2.5f, backWall), new Vector3(1.4f, 0.8f, 0.08f), screen);
            Box("TV_2", parent, new Vector3(2f, 2.5f, backWall), new Vector3(1.4f, 0.8f, 0.08f), screen);
            Box("TV_3", parent, new Vector3(0, 2.5f, -backWall), new Vector3(1.4f, 0.8f, 0.08f), screen);

            // Breaker box on the right wall, near the table. Short Circuit sparks it.
            Box("BreakerBox", parent, new Vector3(RoomWidth / 2 - WallThickness, 1.5f, 0.8f), new Vector3(0.12f, 0.6f, 0.4f), GetMaterial("BreakerBox", new Color(0.35f, 0.38f, 0.35f)));

            BuildGameShelf(parent);
            BuildHazardStep(parent);
        }

        private static void BuildGameShelf(Transform parent)
        {
            Transform shelf = Group("GameShelf", parent);
            shelf.localPosition = new Vector3(-RoomWidth / 2 + 0.4f, 0, -1.2f);
            Box("Frame", shelf, new Vector3(0, 1f, 0), new Vector3(0.4f, 2f, 2.4f), GetMaterial("ShelfWood", new Color(0.25f, 0.15f, 0.08f)));

            Color[] boxColors = { new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.4f, 0.8f), new Color(0.2f, 0.7f, 0.3f), new Color(0.9f, 0.8f, 0.2f) };
            for (int i = 0; i < boxColors.Length; i++)
            {
                Box($"Game_{i + 1}", shelf, new Vector3(0.25f, 0.5f + i * 0.4f, -0.8f + i * 0.5f), new Vector3(0.1f, 0.3f, 0.4f), GetMaterial($"Game_{i + 1}", boxColors[i]));
            }
        }

        private static void BuildHazardStep(Transform parent)
        {
            Transform step = Group("HazardStep", parent);
            step.localPosition = new Vector3(3f, 0, -RoomDepth / 2 + 0.6f);
            const float stepWidth = 3f;
            Box("Step", step, new Vector3(0, 0.075f, 0), new Vector3(stepWidth, 0.15f, 0.8f), GetMaterial("Step", new Color(0.2f, 0.2f, 0.2f)));

            // Yellow and black stripes along the front edge.
            Material yellow = GetMaterial("HazardYellow", new Color(0.95f, 0.8f, 0.1f));
            Material black = GetMaterial("HazardBlack", new Color(0.05f, 0.05f, 0.05f));
            float stripeWidth = stepWidth / HazardStripes;
            for (int i = 0; i < HazardStripes; i++)
            {
                float x = -stepWidth / 2 + stripeWidth * (i + 0.5f);
                Box($"Stripe_{i + 1}", step, new Vector3(x, 0.152f, 0.32f), new Vector3(stripeWidth, 0.01f, 0.16f), i % 2 == 0 ? yellow : black);
            }
        }

        private static Transform Group(string name, Transform parent)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            Primitive(PrimitiveType.Cube, name, parent, position, scale, material, Vector3.zero);

        private static GameObject Sphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material) =>
            Primitive(PrimitiveType.Sphere, name, parent, position, scale, material, Vector3.zero);

        // A Unity cylinder is 2 units tall, so scale.y is half the height.
        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation = default) =>
            Primitive(PrimitiveType.Cylinder, name, parent, position, scale, material, rotation);

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 rotation)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = position;
            shape.transform.localEulerAngles = rotation;
            shape.transform.localScale = scale;
            shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            return shape;
        }

        // Loads the material asset, or creates it the first time. The color is set every run so tweaks here apply on rebuild.
        private static Material GetMaterial(string name, Color color, bool emissive = false)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(LitShader));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
