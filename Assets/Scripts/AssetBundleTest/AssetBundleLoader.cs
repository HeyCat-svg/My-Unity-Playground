using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AssetBundleLoader : MonoBehaviour {
    

    void Start() {
        //AssetBundle bundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/cubebundle");
        //GameObject cubePrefab = (GameObject)bundle.LoadAsset("Cube");
        //Instantiate(cubePrefab, new Vector3(0, 1, 0), Quaternion.identity);

        StartCoroutine(GetData());
    }

    void Update() {
        
    }

    IEnumerator GetData() {
        UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(/*"file:///" + */Application.streamingAssetsPath + "/cubebundle");
        yield return uwr.SendWebRequest();
        AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
        AssetBundleRequest loadAsset = bundle.LoadAssetAsync<GameObject>("Cube");
        yield return loadAsset;
        Instantiate(loadAsset.asset, new Vector3(0, 1, 0), Quaternion.identity);
    }
}
