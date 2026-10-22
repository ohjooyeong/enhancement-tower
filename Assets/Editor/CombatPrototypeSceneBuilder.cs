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
        new GameObject("Combat Prototype").AddComponent<CombatPrototype>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/CombatPrototype.unity");
    }
}
