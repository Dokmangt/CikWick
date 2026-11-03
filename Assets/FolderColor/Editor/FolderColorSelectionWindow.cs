using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace FolderColor.Editor
{
	public class FolderColorSelectionWindow : EditorWindow
	{
		private string[] _targetGuids;
		private Texture2D[] _icons;

		private bool _foldoutPresetColor = false;
		private Color _colorField = Color.white;

		private bool _useGlobal;
		private int _selectedIndex = 1;

		private bool _highlightName;
		private bool _highlightChildren;
		
		private Vector2 _scrollPosition = Vector2.zero;

		[MenuItem("Assets/Custom Folder Icon", false, 100)]
		private static void Open()
		{
			Open(Selection.assetGUIDs);
		}

		[MenuItem("Assets/Custom Folder Icon", true)]
		private static bool Validate()
		{
			return Selection.assetGUIDs.Any(g =>
				AssetDatabase.IsValidFolder(AssetDatabase.GUIDToAssetPath(g)));
		}

		private static void Open(string[] guids)
		{
			FolderColorSelectionWindow win = GetWindow<FolderColorSelectionWindow>("Folder Selection");
			win.minSize = new Vector2(300, 450);
			win._targetGuids = guids;
			win.LoadIcons();
			win.Show();
		}

		private void LoadIcons()
		{
			string path = FolderColorDataControl.FolderColorData.GetFolderImagePath("");

			_icons = AssetDatabase.FindAssets("t:Texture2D", new[]
				{
					path
				})
				.Select(g => AssetDatabase.LoadAssetAtPath<Texture2D>(
					AssetDatabase.GUIDToAssetPath(g)))
				.ToArray();
		}

		private void OnGUI()
		{
			FolderColorToolBar();

			_scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(15);

			GUILayout.BeginVertical();
			GUILayout.Space(15);

			EditorGUILayout.BeginVertical("helpbox");
			GUILayout.Space(5);
			GUILayout.Label("Folder Tint Color", EditorStyles.boldLabel);
			_colorField = EditorGUILayout.ColorField(GUIContent.none, _colorField, false, true, false, GUILayout.Height(30));
			DrawPreviousSelectedColor();
			GUILayout.Space(5);
			EditorGUILayout.EndVertical();

			GUILayout.Space(10);

			EditorGUILayout.BeginVertical("helpbox");
			GUILayout.Space(5);
			GUILayout.Label("Highlight Options", EditorStyles.boldLabel);

			EditorGUIUtility.labelWidth = 150;
			_highlightName = EditorGUILayout.Toggle("Highlight Folder Name", _highlightName);
			_highlightChildren = EditorGUILayout.Toggle("Highlight Asset Under", _highlightChildren);
			EditorGUIUtility.labelWidth = 0;

			GUILayout.Space(5);
			EditorGUILayout.EndVertical();

			GUILayout.Space(10);

			EditorGUILayout.BeginVertical("helpbox");
			GUILayout.Space(5);
			GUILayout.Label("Icon Selection", EditorStyles.boldLabel);
			DrawIconGrid();
			GUILayout.Space(5);
			EditorGUILayout.EndVertical();

			GUILayout.FlexibleSpace();

			DrawPreview();

			GUILayout.Space(10);

			if (GUILayout.Button("Apply", GUILayout.Height(40)))
			{
				ApplyChanges();
			}

			GUILayout.Space(15);
			GUILayout.EndVertical();

			GUILayout.Space(15);
			GUILayout.EndHorizontal();
			
			GUILayout.EndScrollView();
		}

		private void FolderColorToolBar()
		{
			GUILayout.BeginHorizontal(EditorStyles.toolbar);
			GUILayout.FlexibleSpace();

			GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);

			GUI.backgroundColor = !_useGlobal ? Color.green : Color.white;
			if (GUILayout.Button("Personal", buttonStyle, GUILayout.Width(80)))
			{
				_useGlobal = false;
				Repaint();
			}

			GUI.backgroundColor = _useGlobal ? Color.green : Color.white;
			if (GUILayout.Button("Global", buttonStyle, GUILayout.Width(80)))
			{
				_useGlobal = true;
				Repaint();
			}

			GUI.backgroundColor = Color.white;
			GUILayout.EndHorizontal();
		}

		private void DrawPreviousSelectedColor()
		{
			GUILayout.Space(5);

			_foldoutPresetColor =  EditorGUILayout.Foldout(_foldoutPresetColor, "Preset Colors");
			
			if (!_foldoutPresetColor)
				return;
			
			List<Color> globalColor = FolderColorDataControl.FolderColorData.GetPresetColors(FolderColorDataControl.FolderColorMode.Global);
			if (globalColor != null && globalColor.Count > 0)
			{
				GUILayout.Label("Global used colors", EditorStyles.boldLabel);
				DrawColorGrid(globalColor);
			}

			GUILayout.Space(5);

			List<Color> personalColor =
				FolderColorDataControl.FolderColorData.GetPresetColors(FolderColorDataControl.FolderColorMode.Personal);
			if (personalColor != null && personalColor.Count > 0)
			{
				GUILayout.Label("Personal used colors", EditorStyles.boldLabel);
				DrawColorGrid(personalColor);
			}
		}

		private void DrawColorGrid(List<Color> colors)
		{
			int boxSize = 24;
			int padding = 4;
			float availableWidth = position.width - 30;
			int buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (boxSize + padding)));

			for (int i = 0; i < colors.Count; i++)
			{
				if (i % buttonsPerRow == 0)
					GUILayout.BeginHorizontal();

				if (GUILayout.Button(GUIContent.none, GUILayout.Width(boxSize), GUILayout.Height(boxSize)))
				{
					GUI.FocusControl(null);
					_colorField = colors[i];
				}

				if (Event.current.type == EventType.Repaint)
				{
					Rect btnRect = GUILayoutUtility.GetLastRect();
					Rect colorRect = new Rect(btnRect.x + 2, btnRect.y + 2, btnRect.width - 4, btnRect.height - 4);
					EditorGUI.DrawRect(colorRect, colors[i]);
				}

				if (i % buttonsPerRow == buttonsPerRow - 1 || i == colors.Count - 1)
					GUILayout.EndHorizontal();
			}
		}

		private void DrawIconGrid()
		{
			int buttonSize = 70;
			int padding = 4;
			int totalItems = _icons.Length + 1;
			int buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt(position.width / (buttonSize + padding)));

			for (int i = 0; i < totalItems; i++)
			{
				if (i % buttonsPerRow == 0)
					GUILayout.BeginHorizontal();

				GUI.backgroundColor = _selectedIndex == i ? Color.green : Color.white;

				if (i == 0)
				{
					if (GUILayout.Button("None", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
					{
						_selectedIndex = 0;
					}
				}
				else
				{
					if (GUILayout.Button(_icons[i - 1], GUILayout.Width(buttonSize), GUILayout.Height(buttonSize)))
					{
						_selectedIndex = i;
					}
				}

				GUI.backgroundColor = Color.white;

				if (i % buttonsPerRow == buttonsPerRow - 1 || i == totalItems - 1)
					GUILayout.EndHorizontal();
			}
		}

		private void DrawPreview()
		{
			GUILayout.BeginVertical("box");
			GUILayout.Label("Preview", EditorStyles.centeredGreyMiniLabel);
			GUILayout.Space(5);

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			Rect previewRect = GUILayoutUtility.GetRect(64, 64, GUILayout.ExpandWidth(false));

			if (_selectedIndex == 0)
			{
				GUI.Label(previewRect, "Default\nFolder", EditorStyles.centeredGreyMiniLabel);
			}
			else
			{
				Color originalColor = GUI.color;
				GUI.color = _colorField;
				GUI.DrawTexture(previewRect, _icons[_selectedIndex - 1], ScaleMode.ScaleToFit);
				GUI.color = originalColor;
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			GUILayout.EndVertical();
		}

		private void ApplyChanges()
		{
			FolderColorDataControl.FolderColorMode mode = _useGlobal
				? FolderColorDataControl.FolderColorMode.Global
				: FolderColorDataControl.FolderColorMode.Personal;

			string rootPath = FolderColorDataControl.FolderColorData.GetFolderImagePath("");
			
			foreach (string guid in _targetGuids)
			{
				if (_selectedIndex == 0)
				{
					FolderColorDataControl.FolderColorData.Remove(guid, mode);
				}
				else
				{
					string iconPath = AssetDatabase.GetAssetPath(_icons[_selectedIndex - 1]);
					if (!string.IsNullOrEmpty(rootPath) && iconPath.StartsWith(rootPath))
					{
						iconPath = iconPath.Substring(rootPath.Length).TrimStart('/', '\\');
					}
			
					FolderColorDataControl.FolderColorData.Set(guid, iconPath, _colorField,
						_highlightName, _highlightChildren, mode);
				}
			}

			Close();
		}
	}
}