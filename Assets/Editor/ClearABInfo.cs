using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ClearABInfo
{
	private const string MenuPath = "Assets/清空文件夹AB信息";

	[MenuItem(MenuPath, false, priority: 2000)]
	private static void ClearSelectedFolderAssetBundles()
	{
		var selectedObjects = Selection.GetFiltered<Object>(SelectionMode.Assets);
		var selectedFolders = selectedObjects
			.Select(AssetDatabase.GetAssetPath)
			.Where(AssetDatabase.IsValidFolder)
			.Distinct()
			.ToArray();

		if (selectedFolders.Length == 0)
		{
			Debug.LogWarning("请选择需要清理AB信息的文件夹。");
			return;
		}

		try
		{
			AssetDatabase.StartAssetEditing();
			var clearedCount = 0;

			foreach (var folder in selectedFolders)
			{
				var assetGuids = AssetDatabase.FindAssets(string.Empty, new[] { folder });

				for (var index = 0; index < assetGuids.Length; index++)
				{
					var guid = assetGuids[index];
					var assetPath = AssetDatabase.GUIDToAssetPath(guid);

					if (AssetDatabase.IsValidFolder(assetPath))
					{
						continue;
					}

					var importer = AssetImporter.GetAtPath(assetPath);
					if (importer == null)
					{
						continue;
					}

					if (!string.IsNullOrEmpty(importer.assetBundleName) || !string.IsNullOrEmpty(importer.assetBundleVariant))
					{
						if (!string.IsNullOrEmpty(importer.assetBundleVariant))
						{
							importer.assetBundleVariant = string.Empty;
						}

						importer.assetBundleName = string.Empty;
						clearedCount++;
					}
				}
			}

			Debug.Log($"清理完成：共清空 {clearedCount} 个资源的AB信息。");
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
			AssetDatabase.RemoveUnusedAssetBundleNames();
			AssetDatabase.SaveAssets();
		}
	}

	[MenuItem(MenuPath, true)]
	private static bool ClearSelectedFolderAssetBundlesValidate()
	{
		var selectedObjects = Selection.GetFiltered<Object>(SelectionMode.Assets);
		return selectedObjects.Any(obj => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj)));
	}
}
