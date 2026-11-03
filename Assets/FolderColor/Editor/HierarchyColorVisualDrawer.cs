using FolderColor.Runtime;
using UnityEditor;
using UnityEngine;

namespace FolderColor.Editor
{
	[InitializeOnLoad]
	public class HierarchyColorVisualDrawer
	{
		static HierarchyColorVisualDrawer()
		{
#if UNITY_6000_6_OR_NEWER
			EditorApplication.hierarchyWindowItemByEntityIdOnGUI += OnHierarchyGUI_EntityId;
#else
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI_InstanceID;
#endif
		}

#if UNITY_6000_6_OR_NEWER
		private static void OnHierarchyGUI_EntityId(EntityId entityId, Rect rect)
		{
			GameObject obj = EditorUtility.EntityIdToObject(entityId) as GameObject;
			ProcessItem(obj, rect);
		}
#else
        private static void OnHierarchyGUI_InstanceID(int instanceID, Rect rect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            ProcessItem(obj, rect);
        }
#endif

		private static void ProcessItem(GameObject obj, Rect rect)
		{
			if (obj == null)
				return;

			if (Event.current.type != EventType.Repaint) 
				return;
			
			DrawChildHighlight(obj, rect);

			if (!obj.TryGetComponent(out HierarchyColorVisualData colorData))
				return;

			Rect nameRect = new Rect(rect.x + 18f, rect.y, rect.width - 18f, rect.height);
			EditorGUI.DrawRect(nameRect,
				new Color(colorData.HighlightColor.r, colorData.HighlightColor.g, colorData.HighlightColor.b, 0.2f));
			
			Texture icon = EditorGUIUtility.ObjectContent(obj, typeof(GameObject)).image;
			if (icon != null)
			{
				Rect rightIconRect = new Rect(rect.xMax - 18f, rect.y, 16f, 16f);
             
				Color originalColor = GUI.color;
				GUI.color = Color.white;
				GUI.DrawTexture(rightIconRect, icon, ScaleMode.ScaleToFit);
				GUI.color = originalColor;
			}
		}

		private static void DrawChildHighlight(GameObject obj, Rect rect)
		{
			HierarchyColorVisualData[] datas = obj.GetComponentsInParent<HierarchyColorVisualData>();

			foreach (HierarchyColorVisualData data in datas)
			{
				if (data.gameObject == obj)
					continue;
				
				if (!data.HighlightChildren)
					continue;

				Rect highlightRect = new Rect(rect.x + 18f, rect.y, rect.x + rect.width + 2000, rect.height);
				EditorGUI.DrawRect(highlightRect,
					new Color(data.HighlightColor.r, data.HighlightColor.g, data.HighlightColor.b, 0.1f));
				return;
			}
		}
	}
}