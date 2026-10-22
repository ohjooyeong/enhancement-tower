using EnhancementTower.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CombatPrototype.unity");
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
        AssetDatabase.SaveAssets();
    }
}
