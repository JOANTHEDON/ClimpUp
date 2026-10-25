using UnityEngine;
using UnityEditor;

public static class CanvasHierarchyMenu
{
    [MenuItem("GameObject/UI/Game UI Canvas (Ready with Image Slot)", false, 10)]
    public static void CreateGameCanvas()
    {
        GameObject canvasObj = new GameObject("MainCanvas");
        canvasObj.AddComponent<GameCanvasSetup>();
        Selection.activeGameObject = canvasObj;
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create MainCanvas");
    }
}
