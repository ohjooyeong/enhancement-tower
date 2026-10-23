using EnhancementTower.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CombatPrototypeSceneBuilder
{
    [MenuItem("Enhancement Tower/Create Combat Prototype Scene")]
    public static void Create()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateComboAsset();
        var prototype = new GameObject("Combat Prototype").AddComponent<CombatPrototype>();
        var serialized = new SerializedObject(prototype);
        serialized.FindProperty("comboDefinition").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.ComboDefinition>("Assets/Data/LongswordCombo.asset");
        serialized.FindProperty("tacticsDefinition").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.CombatTacticsDefinition>("Assets/Data/CombatTactics.asset");
        serialized.FindProperty("damageDefinition").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.CombatDamageDefinition>("Assets/Data/CombatDamage.asset");
        serialized.FindProperty("fastMonsterDefinition").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.FastMonsterDefinition>("Assets/Data/FastMonster.asset");
        serialized.FindProperty("stageDefinition").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.StageDefinition>("Assets/Data/PrototypeStage.asset");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CombatPrototype.unity");
    }
    [MenuItem("Enhancement Tower/Create View Comparison Scene")]
    public static void CreateViewComparison()
    {
        const string path = "Assets/Scenes/CombatViewComparison.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            if (!Application.isBatchMode && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null) throw new System.InvalidOperationException("The view comparison requires the current Universal RP asset.");
        const string rendererPath = "Assets/Settings/ViewComparisonRenderer.asset";
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, rendererPath);
        }
        Material CreateMaterial(string materialPath, string shaderName)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null) return material;
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.InvalidOperationException("Missing comparison shader: " + shaderName);
            material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); return material;
        }
        var spriteMaterial = CreateMaterial("Assets/Settings/ViewComparisonSprite.mat", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
        var stoneMaterial = CreateMaterial("Assets/Settings/ViewComparisonStone.mat", "Universal Render Pipeline/Unlit");
        var settings = new SerializedObject(pipeline);
        var list = settings.FindProperty("m_RendererDataList");
        int index = -1;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == renderer) index = i;
        if (index < 0)
        {
            index = list.arraySize; list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = renderer;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        // Only append a camera-selectable renderer; never change the default renderer or quality levels.
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatPrototype.unity");
        CombatPrototype prototype = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            prototype = root.GetComponentInChildren<CombatPrototype>();
            if (prototype != null) break;
        }
        if (prototype == null) throw new System.InvalidOperationException("CombatPrototype component missing from source scene.");
        var serialized = new SerializedObject(prototype);
        serialized.FindProperty("enableViewComparison").boolValue = true;
        serialized.FindProperty("comparisonRendererIndex").intValue = index;
        serialized.FindProperty("comparisonSpriteMaterial").objectReferenceValue = spriteMaterial;
        serialized.FindProperty("comparisonStoneMaterial").objectReferenceValue = stoneMaterial;
        serialized.FindProperty("randomizeSeedEachRun").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, path, true);
        EditorSceneManager.OpenScene(path);
        AssetDatabase.SaveAssets();
        Debug.Log("View comparison scene created; original scene and default renderer preserved.");
    }
    [MenuItem("Enhancement Tower/Create Equipment Prototype Scene")]
    public static void CreateEquipmentPrototype()
    {
        const string path = "Assets/Scenes/EquipmentPrototype.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        {
            if (!Application.isBatchMode && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/CombatViewComparison.unity") == null) CreateViewComparison();
        const string dataPath = "Assets/Data/PrototypeEquipment.asset";
        var definition = AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.EquipmentPrototypeDefinition>(dataPath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<EnhancementTower.Data.EquipmentPrototypeDefinition>();
            AssetDatabase.CreateAsset(definition, dataPath);
        }
        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatViewComparison.unity");
        definition = AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.EquipmentPrototypeDefinition>(dataPath);
        if (definition == null) throw new System.InvalidOperationException("Equipment definition could not be loaded after scene switch.");
        CombatPrototype prototype = null;
        foreach (GameObject root in scene.GetRootGameObjects()) { prototype = root.GetComponentInChildren<CombatPrototype>(); if (prototype != null) break; }
        if (prototype == null) throw new System.InvalidOperationException("Source comparison component missing.");
        var serialized = new SerializedObject(prototype);
        serialized.FindProperty("enableEquipmentPrototype").boolValue = true;
        serialized.FindProperty("equipmentDefinition").objectReferenceValue = definition;
        serialized.FindProperty("randomizeSeedEachRun").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, path, true); EditorSceneManager.OpenScene(path); AssetDatabase.SaveAssets();
    }
    [MenuItem("Enhancement Tower/Create Default Combo Asset")]
    public static void CreateComboAsset()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        const string path = "Assets/Data/LongswordCombo.asset";
        if (AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.ComboDefinition>(path) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EnhancementTower.Data.ComboDefinition>(), path);
        const string tacticsPath = "Assets/Data/CombatTactics.asset";
        if (AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.CombatTacticsDefinition>(tacticsPath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EnhancementTower.Data.CombatTacticsDefinition>(), tacticsPath);
        const string damagePath = "Assets/Data/CombatDamage.asset";
        if (AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.CombatDamageDefinition>(damagePath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EnhancementTower.Data.CombatDamageDefinition>(), damagePath);
        const string fastPath = "Assets/Data/FastMonster.asset";
        if (AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.FastMonsterDefinition>(fastPath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EnhancementTower.Data.FastMonsterDefinition>(), fastPath);
        const string stagePath = "Assets/Data/PrototypeStage.asset";
        if (AssetDatabase.LoadAssetAtPath<EnhancementTower.Data.StageDefinition>(stagePath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<EnhancementTower.Data.StageDefinition>(), stagePath);
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Enhancement Tower/Create Journey Prototype Scene")]
    public static void CreateJourneyPrototype()
    {
        const string path = "Assets/Scenes/JourneyPrototype.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
        { EditorSceneManager.OpenScene(path); return; }
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/EquipmentPrototype.unity") == null)
            CreateEquipmentPrototype();
        AssetDatabase.SaveAssets();
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/EquipmentPrototype.unity");
        CombatPrototype prototype = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        { prototype = root.GetComponentInChildren<CombatPrototype>(); if (prototype != null) break; }
        if (prototype == null) throw new System.InvalidOperationException("Equipment scene has no combat prototype.");
        var serialized = new SerializedObject(prototype);
        serialized.FindProperty("enableJourneyPrototype").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, path, true);
        EditorSceneManager.OpenScene(path);
        Debug.Log("Journey prototype created; original scenes preserved.");
    }

}
