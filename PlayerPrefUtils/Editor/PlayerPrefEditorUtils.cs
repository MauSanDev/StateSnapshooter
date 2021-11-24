using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class PlayerPrefEditorUtils
{
    private const string UNITY_PLIST_FILE_NAME = "unity.{0}.{1}.plist";
    private const string WINDOWS_REGISTRY_PATH = "Software\\Unity\\UnityEditor\\{0}\\{1}";
    
    private static string PListParsedName => UNITY_PLIST_FILE_NAME.Replace("{0}", PlayerSettings.companyName).Replace("{1}", PlayerSettings.productName);
    private static string PListFullPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Preferences", PListParsedName);

    public static Dictionary<string, object> GetAllPrefs()
    {
        if (Application.platform == RuntimePlatform.OSXEditor)
        {
            return (Dictionary<string, object>) PlistCS.Plist.readPlist(PListFullPath);
        }

        if (Application.platform == RuntimePlatform.WindowsEditor)
        {
            return GetPlayerPrefsFromWindowsRegistry();
        }

        return null;
    }

    private static Dictionary<string, object> GetPlayerPrefsFromWindowsRegistry()
    {
        string subkeyPath = WINDOWS_REGISTRY_PATH.Replace("{0}", PlayerSettings.companyName).Replace("{1}", PlayerSettings.productName);
        
        Microsoft.Win32.RegistryKey registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subkeyPath);

        if (registryKey == null)
        {
            return null;
        }
        
        Dictionary<string, object> allPlayerPrefs = new Dictionary<string, object>();
        string[] registryValueKeys = registryKey.GetValueNames();

        for (int i = 0; i < registryValueKeys.Length; i++)
        {
            string parsedKey = registryValueKeys[i];
            int index = parsedKey.LastIndexOf("_");
            parsedKey = parsedKey.Remove(index, parsedKey.Length - index);

            object registryValue = registryKey.GetValue(registryValueKeys[i]);

            switch (registryValue)
            {
                case int integer:
                    bool isFloat = PlayerPrefs.GetInt(parsedKey, -1) == -1 && PlayerPrefs.GetInt(parsedKey, 0) == 0;
                    if (isFloat)
                    {
                        registryValue = PlayerPrefs.GetFloat(parsedKey);
                    }
                    break;
                case byte[] str:
                    System.Text.Encoding encoding = new System.Text.UTF8Encoding();
                    registryValue = encoding.GetString(str).TrimEnd('\0');
                    break;
            }

            allPlayerPrefs[parsedKey] = registryValue;
        }
        
        return allPlayerPrefs;
    }
}
