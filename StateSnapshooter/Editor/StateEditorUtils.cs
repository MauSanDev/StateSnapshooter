using UnityEngine;
using UnityEditor;
using System.IO;

public static class StateEditorUtils
{
    private const string DELETESAVEFILE_PATH = "Noar Utils/Player State/Delete Save File";
    private const string DELETEPLAYERPREFS_PATH = "Noar Utils/Player State/Delete Player Prefs";
    private const string DELETEALLDATA_PATH = "Noar Utils/Player State/Delete All";
    private const string OPEN_DATA_PATH = "Noar Utils/Player State/Open Persistant Datapath";
    private const string OPEN_PERSISTANT_DATA_PATH = "Noar Utils/Player State/Open Datapath";

    [MenuItem(DELETESAVEFILE_PATH)]
    public static void DeleteSavedData()
    {
        if (Directory.Exists(Application.persistentDataPath))
        {
            Directory.Delete(Application.persistentDataPath, true);
            Debug.Log("Persistent data files were deleted.");
        }
        
        if (Directory.Exists(Application.dataPath))
        {
            Directory.Delete(Application.dataPath, true);
            Debug.Log("Data files were deleted.");
        }
    }

    [MenuItem(DELETEPLAYERPREFS_PATH)]
    public static void DeletePlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("PlayerPrefs were deleted.");

    }

    [MenuItem(DELETEALLDATA_PATH)]
    public static void DeleteAllData()
    {
        DeleteSavedData();
        DeletePlayerPrefs();
    }
    

    [MenuItem(OPEN_DATA_PATH)]
    public static void OpenDataPath()
    {
        EditorUtility.RevealInFinder(Application.dataPath);
    }
    
    [MenuItem(OPEN_PERSISTANT_DATA_PATH)]
    public static void OpenPersistantDataPath()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
