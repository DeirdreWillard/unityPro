using UnityEngine;
using UnityEditor;
using System.IO;

public class EditorHelper : MonoBehaviour {

	[MenuItem("Assets/fnt文件生成美术字体", false, 1000)]
	static public void BatchCreateArtistFont()
	{
		ArtistFont.BatchCreateArtistFont();
	}

	[MenuItem("Assets/fnt文件生成美术字体", true)]
	static bool ValidateBatchCreateArtistFont()
	{
		// 检查选中的文件是否为 .fnt 文件
		string path = AssetDatabase.GetAssetPath(Selection.activeObject);
		return !string.IsNullOrEmpty(path) && Path.GetExtension(path).ToLower() == ".fnt";
	}
}
