using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using PlasticGui.WorkspaceWindow;
using UnityEngine;
using UnityEditor;

public class StateSnapshooter : EditorWindow
{
    private const string FOLDER_NAME = "_StateSnapshots";
    private const string SUBFOLDER_NAME = "StateSnapshot_";
    private const string CONTEXT_FILE_NAME = "SnapshotContext.txt";
    private const string PLAYER_PREFS_BACKUP_FILE = "PlayerPrefsBackup.json";
    private const string DATA_FOLDER = "Data";

    private bool showCreationBox = false;
    private string newSnapshotName = string.Empty;
    private string newSnapshotContext = string.Empty;

    private string[] snapshotPaths = null;
    private Dictionary<string, SnapshotContextData> contextData = new Dictionary<string, SnapshotContextData>();

    private GUIStyle createSnapshotLabelStyle;

    private Vector2 statesScroll = Vector2.zero;

    private string SNAPSHOT_TOOL_DIRECTORY
    {
        get
        {
            var file = Directory.GetParent(Application.persistentDataPath);
            string projectName = PlayerSettings.productName.Replace(" ", "");
            return Path.Combine(file.FullName, projectName + FOLDER_NAME);
        }
    }
    
    private char FoldOutCharacter => showCreationBox ? '▼' : '►';

    private void OnEnable()
    {
        RefreshSnapshots();
    }
    
    [MenuItem("Noar Utils/State Snapshooter")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow(typeof(StateSnapshooter));
        window.minSize = new Vector2(650, 400);
        window.maxSize = new Vector2(800, 400);
        window.titleContent = new GUIContent("State Snapshooter");
        window.Show();
    }
    private void OnGUI()
    {
        DrawToolbar();
        DrawCreationBox();
        EditorGUILayout.Space();
        DrawSnapshotList();
    }
    
    private void RefreshSnapshots()
    {
        snapshotPaths = Directory.GetDirectories(SNAPSHOT_TOOL_DIRECTORY);
        
        contextData.Clear();
        for (int i = 0; i < snapshotPaths.Length; i++)
        {
            contextData[snapshotPaths[i]] = SnapshotContextData.Load(Path.Combine(snapshotPaths[i], CONTEXT_FILE_NAME));
        }
    }

    #region Drawers

    private void DrawSnapshotList()
    {
        GUI.backgroundColor = Color.gray;
        EditorGUILayout.BeginScrollView(statesScroll, "HelpBox");
        GUI.backgroundColor = Color.white;


        if (snapshotPaths == null || snapshotPaths.Length == 0)
        {
            EditorGUILayout.LabelField("There's no snapshots available.");
            EditorGUILayout.EndScrollView();
            return;
        }

        for (int i = snapshotPaths.Length - 1; i >= 0; i--)
        {
            string dir = snapshotPaths[i];

            SnapshotContextData context = contextData[dir];

            GUI.backgroundColor = i % 2 == 0 ? Color.gray : Color.white;
            EditorGUILayout.BeginHorizontal("Box");
            GUI.backgroundColor = Color.white;
            EditorGUILayout.LabelField(context.Name, GUILayout.MinWidth(200f));
            EditorGUILayout.LabelField(context.DateString, GUILayout.Width(150f));

            if (GUILayout.Button("Context", EditorStyles.miniButtonLeft, GUILayout.MaxWidth(100f)))
            {
                EditorUtility.DisplayDialog(context.Name, $"Creation: {context.DateString} \n Context: {context.Context}",
                    "Close");
            }

            if (GUILayout.Button("Delete", EditorStyles.miniButtonMid, GUILayout.MaxWidth(100f)))
            {
                TryDelete(dir);
            }

            if (GUILayout.Button("Folder", EditorStyles.miniButtonRight, GUILayout.MaxWidth(100f)))
            {
                EditorUtility.RevealInFinder(dir);
            }
            
            if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.MaxWidth(100f)))
            {
                TryApply(dir);
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Menu", EditorStyles.toolbarButton, GUILayout.MaxWidth(120f)))
        {
            ShowMenu();
        }

        if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton, GUILayout.MaxWidth(120f)))
        {
            EditorUtility.RevealInFinder(SNAPSHOT_TOOL_DIRECTORY);
        }

        if (GUILayout.Button("Delete States", EditorStyles.toolbarButton, GUILayout.MaxWidth(120f)))
        {
            TryDeleteAll();
        }

        if (GUILayout.Button("Help", EditorStyles.toolbarButton, GUILayout.MaxWidth(120f)))
        {
            //Help
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawCreationBox()
    {
        EditorGUILayout.BeginVertical("HelpBox");

        if (GUILayout.Button(FoldOutCharacter + " Create Snapshot", CreateSnapshotLabelStyle))
        {
            showCreationBox = !showCreationBox;
        }

        if (showCreationBox)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(60));
            EditorGUILayout.LabelField("Name:", GUILayout.MaxWidth(60));
            EditorGUILayout.LabelField("Context:", GUILayout.MaxWidth(60));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            newSnapshotName = EditorGUILayout.TextField(newSnapshotName);
            newSnapshotContext = EditorGUILayout.TextArea(newSnapshotContext, GUILayout.Height(50));

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Snapshot"))
            {
                CreateSnapshot();
            }
        }

        EditorGUILayout.EndVertical();
    }
    
    private void ShowMenu()
    {
        GenericMenu menu = new GenericMenu();
        
        menu.AddItem(new GUIContent("Delete Save Data"), false, StateEditorUtils.DeleteSavedData);
        menu.AddItem(new GUIContent("Delete PlayerPrefs"), false, StateEditorUtils.DeletePlayerPrefs);
        menu.AddItem(new GUIContent("Delete All Data"), false, StateEditorUtils.DeleteAllData);
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Open Persistant Data Folder"), false, StateEditorUtils.OpenPersistantDataPath);
        menu.AddItem(new GUIContent("Open Data Folder"), false, StateEditorUtils.OpenDataPath);
        
        menu.ShowAsContext();
    }

    #endregion

    #region Dialog Methods

    private void TryApply(string snapshotPath)
    {
        int option = EditorUtility.DisplayDialogComplex("Do you want to apply this state?",
            "Your current state will be erased and restored with this one. \n If the game is playing, it will be stopped.",
            "Restore State",
            "Cancel",
            "");

        if (option == 0)
        {
            ApplyState(snapshotPath);
        }
    }
    
    private void TryDelete(string snapshotPath)
    {
        int option = EditorUtility.DisplayDialogComplex("Delete State?",
            "Do you want to delete the state snapshot?",
            "Delete",
            "Cancel",
            "");

        if (option == 0)
        {
            Directory.Delete(snapshotPath, true);
            EditorUtility.DisplayDialog("State Deleted", $"The state was deleted successfully.", "Okay");
            RefreshSnapshots();
        }
    }
    
    private void TryDeleteAll()
    {
        int option = EditorUtility.DisplayDialogComplex("Delete All States?",
            "This will erase all the state snapshot stored by this tool. To you want to continue?",
            "Delete",
            "Cancel",
            "");

        if (option == 0)
        {
            Directory.Delete(SNAPSHOT_TOOL_DIRECTORY, true);
            EditorUtility.DisplayDialog("All States Deleted.", $"There's no stored state snapshots.", "Okay");
            RefreshSnapshots();
        }
    }

    #endregion


    #region Creation/Application

    private void CreateSnapshot()
    {
        if (!Directory.Exists(SNAPSHOT_TOOL_DIRECTORY))
        {
            Directory.CreateDirectory(SNAPSHOT_TOOL_DIRECTORY);
        }

        if (Directory.GetFiles(Application.persistentDataPath, "*.*", SearchOption.AllDirectories).Length == 0)
        {
            EditorUtility.DisplayDialog("State not found", $"There's no files to back up at {Application.persistentDataPath}", "Okay");
            return;
        }

        string newStateFolder = Path.Combine(SNAPSHOT_TOOL_DIRECTORY, SUBFOLDER_NAME + CurrentMilliseconds);
        Directory.CreateDirectory(newStateFolder);
        
        string newDataFolder = Path.Combine(newStateFolder, DATA_FOLDER);
        Directory.CreateDirectory(newDataFolder);
        
        CopyDirectory(Application.persistentDataPath, newDataFolder);
        SnapshotContextData.CreateContextFile(newStateFolder, newSnapshotName, newSnapshotContext);
        CreatePrefsFile(newStateFolder);
        
        newSnapshotName = string.Empty;
        newSnapshotContext = string.Empty;

        EditorUtility.DisplayDialog("Snapshot Created", $"New state created! Path: {newStateFolder}", "Okay");
        RefreshSnapshots();
    }
    
    private void CreatePrefsFile(string path)
    {
        string contextPath = Path.Combine(path, PLAYER_PREFS_BACKUP_FILE);
        string jsonData = JsonConvert.SerializeObject(PlayerPrefEditorUtils.GetAllPrefs());
        TextWriter writer = new StreamWriter(contextPath);
        writer.WriteLine(jsonData);
        writer.Close();
    }
    

    private void CopyDirectory(string from, string to)
    {
        foreach (string dirPath in Directory.GetDirectories(from, "*",
            SearchOption.AllDirectories))
        {
            string newDirPath = dirPath.Replace(from, to);
            if (!Directory.Exists(newDirPath))
            {
                Directory.CreateDirectory(newDirPath);
            }
        }
        
        string[] allFiles = Directory.GetFiles(from, "*.*", SearchOption.AllDirectories);
        foreach (string file in allFiles)
        {
            File.Copy(file, file.Replace(from, to), true);
        }
    }

    #endregion
    

    #region State Application

    private void ApplyState(string stateFolder)
    {
        EditorApplication.isPlaying = false;
        
        PlayerPrefs.DeleteAll();

        string dataFolder = Path.Combine(stateFolder, DATA_FOLDER);
        CopyDirectory(dataFolder, Application.persistentDataPath);
        ApplyPlayerPrefs(stateFolder);

        EditorUtility.DisplayDialog("State Restored!", "Snapshot was restored!", "Okay");
    }
    
    private void ApplyPlayerPrefs(string dataFolder)
    {
        string prefsPath = Path.Combine(dataFolder, PLAYER_PREFS_BACKUP_FILE);

        if (!File.Exists(prefsPath))
        {
            Debug.LogError($"{GetType()} :: There's no Player Prefs Backup file");
            return;
        }
        
        PlayerPrefs.DeleteAll();
        string allData = File.ReadAllText(prefsPath);
    
        Dictionary<string, object> savedPrefs = JsonConvert.DeserializeObject<Dictionary<string, object>>(allData);

        foreach (KeyValuePair<string, object> playerPref in savedPrefs)
        {
            switch (playerPref.Value)
            {
                case int i:
                    PlayerPrefs.SetInt(playerPref.Key, i);
                    break;
                case string s:
                    PlayerPrefs.SetString(playerPref.Key, s);
                    break;
                case float f:
                    PlayerPrefs.SetFloat(playerPref.Key, f);
                    break;
            }
        }

        PlayerPrefs.Save();
    }

    #endregion


    private long CurrentMilliseconds =>
        (long)DateTime.Now.ToUniversalTime().Subtract(
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        ).TotalMilliseconds;

    private GUIStyle CreateSnapshotLabelStyle
    {
        get
        {
            if (createSnapshotLabelStyle == null)
            {
                createSnapshotLabelStyle = new GUIStyle(GUI.skin.label);
                createSnapshotLabelStyle.fontStyle = FontStyle.Bold;
                createSnapshotLabelStyle.alignment = TextAnchor.MiddleLeft;
                createSnapshotLabelStyle.stretchHeight = false;
                createSnapshotLabelStyle.padding.left = 5;
            }

            return createSnapshotLabelStyle;
        }
    }

    private class SnapshotContextData
    {
        [JsonProperty] private string context;
        [JsonProperty] private string name;
        [JsonProperty] private string date;
        [JsonProperty] private string path;
        
        private const string TXT_EMPTY = "<No Data>";
        
        public string Name => name;
        public string Context => context;
        public string DateString => date;
        
        public static SnapshotContextData Empty => new SnapshotContextData(null, DateTime.MinValue.ToString("dd/MM/yyyy HH:mm"));

        public SnapshotContextData(string path, string dateTime, string name = null, string context = null)
        {
            this.name = string.IsNullOrEmpty(name) ? TXT_EMPTY : name;
            this.path = string.IsNullOrEmpty(path) ? TXT_EMPTY : path;
            this.date = string.IsNullOrEmpty(dateTime) ? TXT_EMPTY : dateTime;
            this.context = string.IsNullOrEmpty(context) ? TXT_EMPTY : context;
        }
        
        public static SnapshotContextData Load(string contextFilePath)
        {
            try
            {
                return JsonConvert.DeserializeObject<SnapshotContextData>(File.ReadAllText(contextFilePath));
            }
            catch
            {
                return Empty;
            }
        }
        
        public static void CreateContextFile(string path, string name = null, string context = null)
        {
            SnapshotContextData newContext = new SnapshotContextData(path, DateTime.Now.ToString("dd/MM/yyyy HH:mm"), name, context);
            string contextPath = Path.Combine(path, CONTEXT_FILE_NAME);
            string jsonData = JsonConvert.SerializeObject(newContext);
            TextWriter writer = new StreamWriter(contextPath);
            writer.WriteLine(jsonData);
            writer.Close();
        }
    }
}
