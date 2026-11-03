using UnityEngine;
using UnityEditor;

namespace FolderColor.Editor
{
	[InitializeOnLoad]
	public class FolderColorVisualDrawer
	{
		static FolderColorVisualDrawer()
		{
			EditorApplication.projectWindowItemOnGUI += OnGUI;
		}

		private static void OnGUI(string guid, Rect rect)
		{
			if (Event.current.type != EventType.Repaint) 
				return;
			
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (string.IsNullOrEmpty(path)) return;

			DrawChildHighlight(path, rect);

			if (!AssetDatabase.IsValidFolder(path)) return;

			DrawForDataBase(guid, rect, FolderColorDataControl.FolderColorMode.Global);
			DrawForDataBase(guid, rect, FolderColorDataControl.FolderColorMode.Personal);
		}

		private static void DrawChildHighlight(string path, Rect rect)
		{
			Color? childColor = FolderColorDataControl.FolderColorData.GetChildHighlightColor(path, FolderColorDataControl.FolderColorMode.Personal) 
			                    ?? FolderColorDataControl.FolderColorData.GetChildHighlightColor(path, FolderColorDataControl.FolderColorMode.Global);

			if (childColor.HasValue && rect.height <= 18)
			{
				Rect highlightRect = new Rect(0, rect.y, rect.x + rect.width + 2000, rect.height);
				EditorGUI.DrawRect(highlightRect, new Color(childColor.Value.r, childColor.Value.g, childColor.Value.b, 0.05f));
			}
		}
		
		private static void DrawForDataBase(string guid, Rect rect, FolderColorDataControl.FolderColorMode mode)
		{
			var entry = FolderColorDataControl.FolderColorData.Get(guid, mode);
			if (entry == null) return;
			Color originalColor = GUI.color;

			if (entry.HighlightName && rect.height <= 18)
			{
				float iconOffset = 18f;
				Rect nameRect = new Rect(rect.x + iconOffset, rect.y, rect.width - iconOffset, rect.height);
				EditorGUI.DrawRect(nameRect, new(entry.IconColor.r, entry.IconColor.g, entry.IconColor.b, 0.2f));
			}

			string iconPath = FolderColorDataControl.FolderColorData.GetFolderImagePath(entry.IconPath);
			Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
			if (icon == null)
			{
				FolderColorDataControl.FolderColorData.Remove(guid, mode);
				return;
			}

			Rect iconRect = rect.height <= 18
				? new Rect(rect.x, rect.y, rect.height, rect.height)
				: new Rect(rect.x, rect.y, rect.height - 8, rect.height - 8);

			GUI.color = entry.IconColor;

			GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);

			GUI.color = originalColor;
		}
	}
}