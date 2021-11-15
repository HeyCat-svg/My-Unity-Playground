using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class AssetBundleCreater : MonoBehaviour {
	[MenuItem("Test/Build Asset Bundles")]
    static void BuildAssetBundles() {
        BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.WebGL);
    }
}
