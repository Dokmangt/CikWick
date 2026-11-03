using UnityEngine;

namespace FolderColor.Runtime
{
	[DisallowMultipleComponent]
	public class HierarchyColorVisualData : MonoBehaviour
	{
#if UNITY_EDITOR
		
		[SerializeField]
		private Color _highlightColor = Color.white;
		[SerializeField]
		private bool _highlightChildren = false;
		
		public  Color HighlightColor => _highlightColor;
		public bool HighlightChildren => _highlightChildren;
#endif
	}
}