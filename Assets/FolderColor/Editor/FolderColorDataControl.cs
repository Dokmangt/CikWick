using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FolderColor.Editor
{
	public class FolderColorDataControl
	{
		public enum FolderColorMode
		{
			Global,
			Personal
		}

		[Serializable]
		public class FolderColorEntry
		{
			public string Guid;
			public string IconPath;
			public string UserId;
		}

		[InitializeOnLoad]
		public static class FolderColorData
		{
			private static FolderColorDatabase _databaseGlobal;
			private static FolderColorDatabase _databasePersonal;

			private static Dictionary<string, IconColorMix> _guidWithIconGlobal = new();
			private static Dictionary<string, IconColorMix> _guidWithIconPersonal = new();
			
			private static List<(string Path, Color Color)> _highlightParentPathsGlobal = new();
			private static List<(string Path, Color Color)> _highlightParentPathsPersonal = new();

			private static readonly Dictionary<string, Texture2D> _textureCache = new();
			private static string _cachedAnchorFolder;
			
			private static FolderColorDatabase DatabaseGlobal
			{
				get
				{
					if (_databaseGlobal == null)
					{
						TryGenerateScriptable(GetGlobalAssetPath(), FolderColorMode.Global, ref _databaseGlobal);
					}

					return _databaseGlobal;
				}
			}

			private static FolderColorDatabase DatabasePersonal
			{
				get
				{
					if (_databasePersonal == null)
					{
						TryGenerateScriptable(GetPersonalAssetPath(), FolderColorMode.Personal, ref _databasePersonal);
					}

					return _databasePersonal;
				}
			}

			private static string UserId => Environment.UserName;

			static FolderColorData()
			{
				EditorApplication.delayCall += LoadOrCreate;
			}

			#region Public

			public static IconColorMix Get(string guid, FolderColorMode mode = FolderColorMode.Global)
			{
				var guidIcons = mode == FolderColorMode.Personal ? _guidWithIconPersonal : _guidWithIconGlobal;

				if (guidIcons.Count == 0 || !guidIcons.TryGetValue(guid, out IconColorMix iconColorMix))
					return null;

				return iconColorMix;
			}

			public static Texture2D GetCachedIcon(string relativeIconPath)
			{
				if (string.IsNullOrEmpty(relativeIconPath)) return null;

				if (_textureCache.TryGetValue(relativeIconPath, out Texture2D cachedTex) && cachedTex != null)
					return cachedTex;

				string fullPath = GetFolderImagePath(relativeIconPath);
				if (string.IsNullOrEmpty(fullPath)) return null;

				Texture2D loadedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
				if (loadedTex != null)
				{
					_textureCache[relativeIconPath] = loadedTex;
				}

				return loadedTex;
			}
			
			public static void Set(string guid, string iconPath, Color color, bool highlightName, bool highlightChildren, FolderColorMode mode)
			{
				var database = mode == FolderColorMode.Personal ? DatabasePersonal : DatabaseGlobal;
				var guidIcons = mode == FolderColorMode.Personal ? _guidWithIconPersonal : _guidWithIconGlobal;

				Remove(guid, mode);
				
				iconPath = iconPath?.TrimStart('/', '\\');

				var iconMix = new IconColorMix(guid, iconPath, color, highlightName, highlightChildren);
				database.IconEntries.Add(iconMix);
				guidIcons.Add(guid, iconMix);

				Save(database);
				
				FillDictionaryIcon(mode);
			}

			public static void Remove(string guid, FolderColorMode mode = FolderColorMode.Global)
			{
				var database = mode == FolderColorMode.Personal ? DatabasePersonal : DatabaseGlobal;
				var guidIcons = mode == FolderColorMode.Personal ? _guidWithIconPersonal : _guidWithIconGlobal;

				if (database == null || guidIcons.Count == 0 ||
				    !guidIcons.TryGetValue(guid, out IconColorMix iconColorMix))
					return;

				database.IconEntries.Remove(iconColorMix);
				guidIcons.Remove(guid);

				Save(database);
				
				FillDictionaryIcon(mode);
			}

			public static List<Color> GetPresetColors(FolderColorMode mode)
			{
				var database = mode == FolderColorMode.Personal ? DatabasePersonal : DatabaseGlobal;

				HashSet<Color> colors = new();

				if (database == null)
					return colors.ToList();

				foreach (var iconEntry in database.IconEntries)
				{
					colors.Add(iconEntry.IconColor);
				}

				return colors.ToList();
			}
			
			public static Color? GetChildHighlightColor(string assetPath, FolderColorMode mode)
			{
				var parentPaths = mode == FolderColorMode.Personal ? _highlightParentPathsPersonal : _highlightParentPathsGlobal;
             
				if (string.IsNullOrEmpty(assetPath) || parentPaths.Count == 0)
					return null;

				int longestMatch = 0;
				Color? bestMatchColor = null;

				foreach (var parent in parentPaths)
				{
					if (assetPath.StartsWith(parent.Path, StringComparison.Ordinal))
					{
						if (parent.Path.Length > longestMatch)
						{
							longestMatch = parent.Path.Length;
							bestMatchColor = parent.Color;
						}
					}
				}

				return bestMatchColor;
			}

			#endregion

			#region Load / Save

			private static void LoadOrCreate()
			{
				_cachedAnchorFolder = null;
				_textureCache.Clear();
				
				MigrateOLDJsonIfNeeded();

				TryGenerateScriptable(GetGlobalAssetPath(), FolderColorMode.Global, ref _databaseGlobal);
				TryGenerateScriptable(GetPersonalAssetPath(), FolderColorMode.Personal, ref _databasePersonal);

				UpdateOldStorageSystem(FolderColorMode.Global);
				UpdateOldStorageSystem(FolderColorMode.Personal);

				FillDictionaryIcon(FolderColorMode.Global);
				FillDictionaryIcon(FolderColorMode.Personal);
			}

			private static void TryGenerateScriptable(string assetPath, FolderColorMode mode,
				ref FolderColorDatabase database)
			{
				database = AssetDatabase.LoadAssetAtPath<FolderColorDatabase>(assetPath);

				if (database != null)
					return;

				database = ScriptableObject.CreateInstance<FolderColorDatabase>();
				database.Mode = mode;

				EnsureFolderExists(assetPath);
				AssetDatabase.CreateAsset(database, assetPath);
				AssetDatabase.SaveAssets();
			}

			private static void FillDictionaryIcon(FolderColorMode mode)
			{
				var database = mode == FolderColorMode.Personal ? DatabasePersonal : DatabaseGlobal;
				var guidIcons = mode == FolderColorMode.Personal ? _guidWithIconPersonal : _guidWithIconGlobal;
				var parentPaths = mode == FolderColorMode.Personal ? _highlightParentPathsPersonal : _highlightParentPathsGlobal;

				guidIcons.Clear();
				parentPaths.Clear();
				
				foreach (var iconEntry in database.IconEntries)
				{
					guidIcons[iconEntry.Guid] = iconEntry;

					if (iconEntry.HighlightChildren)
					{
						string path = AssetDatabase.GUIDToAssetPath(iconEntry.Guid);
						if (!string.IsNullOrEmpty(path))
						{
							path = path.Replace('\\', '/');
							parentPaths.Add((path + "/", iconEntry.IconColor));
						}
					}
				}
			}

			private static void Save(FolderColorDatabase database)
			{
				if (database == null)
					return;

				EditorUtility.SetDirty(database);
				AssetDatabase.SaveAssets();
			}

			#endregion

			#region OLD JSON to Scriptable

			private static void MigrateOLDJsonIfNeeded()
			{
				string globalPath = GetGlobalAssetPath();

				if (AssetDatabase.LoadAssetAtPath<FolderColorDatabase>(globalPath) != null)
					return;

				string jsonPath = FindOLDJson();
				if (string.IsNullOrEmpty(jsonPath))
					return;

				string json = File.ReadAllText(jsonPath);
				OldJson legacy = JsonUtility.FromJson<OldJson>(json);
				if (legacy == null || legacy.assetModified == null)
					return;

				EnsureFolderExists(globalPath);

				FolderColorDatabase database = ScriptableObject.CreateInstance<FolderColorDatabase>();
				database.Mode = FolderColorMode.Global;
				database.Entries = new List<FolderColorEntry>();

				AssetDatabase.CreateAsset(database, globalPath);
				AssetDatabase.SaveAssets();

				for (int i = 0; i < legacy.assetModified.Count; i++)
				{
					string assetPath = legacy.assetModified[i];
					string guid = AssetDatabase.AssetPathToGUID(assetPath);

					if (string.IsNullOrEmpty(guid))
						continue;

					database.Entries.Add(new FolderColorEntry
					{
						Guid = guid, IconPath = legacy.assetModifiedTexturePath[i], UserId = string.Empty
					});
				}

				EditorUtility.SetDirty(database);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				AssetDatabase.DeleteAsset(jsonPath);
				Debug.Log($"[FolderColor] Migrated {database.Entries.Count} entries from OLD JSON.");
			}

			private static string FindOLDJson()
			{
				string folder = GetSaveSetupFolder();
				if (string.IsNullOrEmpty(folder))
					return null;

				string jsonPath = Path.Combine(folder, "FolderModificationData.json").Replace('\\', '/');
				return File.Exists(jsonPath) ? jsonPath : null;
			}

			[Serializable]
			private class OldJson
			{
				public List<string> assetModified;
				public List<string> assetModifiedTexturePath;
			}

			#endregion

			#region OldStorageToNew

			private static void UpdateOldStorageSystem(FolderColorMode mode)
			{
				FolderColorDatabase database = mode == FolderColorMode.Personal ? DatabasePersonal : DatabaseGlobal;

				if (database == null || database.Entries.Count == 0)
					return;

				foreach (var entry in database.Entries)
				{
					Color color = GetColorFromOldAsset(entry.IconPath);
					database.IconEntries.Add(new(entry.Guid, "AbaseFolder.png", color));
				}

				database.Entries.Clear();
				Save(database);
			}

			private static Color GetColorFromOldAsset(string path)
			{
				if (string.IsNullOrEmpty(path))
					return Color.white;

				string fileName = Path.GetFileName(path);

				return fileName switch
				{
					"BlackFolder.png" => Color.black,
					"BlueishGreenFolder.png" => new(0f, 0.6f, 0.6f),
					"BrownFolder.png" => new(0.4f, 0.2f, 0f),
					"FolderBlue.png" => Color.blue,
					"FolderCyan.png" => Color.cyan,
					"FolderPurple.png" => new(0.6f, 0.1f, 0.9f),
					"GreenFolder.png" => Color.green,
					"OrangeFolder.png" => new(1f, 0.5f, 0f),
					"PinkFolder.png" => new(1f, 0.4f, 0.7f),
					"RedFolder.png" => Color.red,
					"SlimeFolder.png" => new(0.6f, 1f, 0.2f),
					"Whitefolder.png" => Color.white,
					"YellowFolder.png" => Color.yellow,
					_ => Color.white
				};
			}

			#endregion

			#region Path utilities

			private static string GetAnchorScriptFolder()
			{
				if (!string.IsNullOrEmpty(_cachedAnchorFolder))
					return _cachedAnchorFolder;

				string guid = AssetDatabase.FindAssets($"{nameof(FolderColorDataControl)} t:script").FirstOrDefault();
				if (string.IsNullOrEmpty(guid))
					return null;

				string scriptPath = AssetDatabase.GUIDToAssetPath(guid);
				_cachedAnchorFolder = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');
				return _cachedAnchorFolder;
			}

			private static string GetSaveSetupFolder()
			{
				string folder = GetAnchorScriptFolder();
				if (string.IsNullOrEmpty(folder))
					return null;

				string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
				return parent != null ? $"{parent}/SaveSetUp" : null;
			}

			public static string GetFolderImagePath(string imageName)
			{
				string folder = GetAnchorScriptFolder();
				if (string.IsNullOrEmpty(folder))
					return null;

				string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
				if (string.IsNullOrEmpty(parent))
					return null;

				if (string.IsNullOrEmpty(imageName))
					return parent;

				imageName = imageName.TrimStart('/', '\\');
				return $"{parent}/{imageName}";
			}
			
			private static string GetGlobalAssetPath()
			{
				return $"{GetSaveSetupFolder()}/FolderColorSettings_Global.asset";
			}

			private static string GetPersonalAssetPath()
			{
				return $"{GetSaveSetupFolder()}/FolderColorSettings_Perso_{UserId}.asset";
			}

			private static void EnsureFolderExists(string assetPath)
			{
				string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
				if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
					return;

				string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
				string name = Path.GetFileName(folder);
				AssetDatabase.CreateFolder(parent, name);
			}

			#endregion
		}
	}
}