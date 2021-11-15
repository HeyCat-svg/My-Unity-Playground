using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowMapping : MonoBehaviour {

    struct FrustumCorners {
        public Vector3[] nearCorners;
        public Vector3[] farCorners;
    }

    public Light dirLight;
    public Shader shadowCaster;
    public Camera mainCamera;
    
    GameObject dirLightCameraObj;
    Camera dirLightCamera;
    Matrix4x4 world2ShadowMat = Matrix4x4.identity;
    RenderTexture depthTexture;
    FrustumCorners mainCameraFrust, lightCameraFrust;

    void Awake() {
        InitFrustumCorners();
        InitLightCamera();
        InitRenderTexture();
    }

    void Start() {

    }

    void Update() {
        if (!dirLight || !dirLightCamera) {
            return;
        }

        CalMainCameraFrustCorners();
        CalLightCameraFrustCorners();

        Shader.SetGlobalFloat("_ShadowBias", 0.005f);
        Shader.SetGlobalFloat("_ShadowStrength", 0.5f);
        world2ShadowMat = GL.GetGPUProjectionMatrix(dirLightCamera.projectionMatrix, false);
        world2ShadowMat = world2ShadowMat * dirLightCamera.worldToCameraMatrix;
        Shader.SetGlobalMatrix("_WorldToShadow", world2ShadowMat);

        dirLightCamera.targetTexture = depthTexture;
        dirLightCamera.RenderWithShader(shadowCaster, "");
    }

    void OnDestroy() {
        depthTexture.Release();
        dirLightCamera = null;
        // DestroyImmediate(depthTexture);
    }

    void InitFrustumCorners() {
        mainCameraFrust = new FrustumCorners();
        lightCameraFrust = new FrustumCorners();

        mainCameraFrust.nearCorners = new Vector3[4];
        mainCameraFrust.farCorners = new Vector3[4];
        lightCameraFrust.nearCorners = new Vector3[4];
        lightCameraFrust.farCorners = new Vector3[4];
    }

    void InitRenderTexture() {
        RenderTextureFormat rtFormat = RenderTextureFormat.Default;
        if (!SystemInfo.SupportsRenderTextureFormat(rtFormat)) {
            rtFormat = RenderTextureFormat.Default;
        }

        depthTexture = new RenderTexture(1024, 1024, 24, rtFormat);
        Shader.SetGlobalTexture("_gShadowMapTexture", depthTexture);
    }

    void InitLightCamera() {
        dirLightCameraObj = new GameObject("Directional Light Camera");
        dirLightCamera = dirLightCameraObj.AddComponent<Camera>();

        dirLightCamera.cullingMask = 1 << LayerMask.NameToLayer("Caster");
        dirLightCamera.backgroundColor = Color.black;
        dirLightCamera.clearFlags = CameraClearFlags.SolidColor;
        dirLightCamera.orthographic = true;
        dirLightCamera.enabled = false;     // 重要 因为不禁用则会在主渲染loop中再渲染一遍 覆盖原有的_gShadowMapTexture
    }

    /* 计算主相机视锥顶点世界坐标 */
    void CalMainCameraFrustCorners() {
        float near = mainCamera.nearClipPlane;
        float far = mainCamera.farClipPlane;

        mainCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), near, Camera.MonoOrStereoscopicEye.Mono, mainCameraFrust.nearCorners);
        for (int i = 0; i < 4; ++i) {
            mainCameraFrust.nearCorners[i] = mainCamera.transform.TransformPoint(mainCameraFrust.nearCorners[i]);
        }

        mainCamera.CalculateFrustumCorners(new Rect(0, 0, 1, 1), far, Camera.MonoOrStereoscopicEye.Mono, mainCameraFrust.farCorners);
        for (int i = 0; i < 4; ++i) {
            mainCameraFrust.farCorners[i] = mainCamera.transform.TransformPoint(mainCameraFrust.farCorners[i]);
        }
    }

    void CalLightCameraFrustCorners() {
        if (dirLightCamera == null) {
            return;
        }

        dirLightCameraObj.transform.rotation = dirLight.transform.rotation;

        // turn coords in world space to light camera space
        for (int i = 0; i < 4; ++i) {
            lightCameraFrust.nearCorners[i] = dirLightCameraObj.transform.InverseTransformPoint(mainCameraFrust.nearCorners[i]);
            lightCameraFrust.farCorners[i] = dirLightCameraObj.transform.InverseTransformPoint(mainCameraFrust.farCorners[i]);
        }

        // cal max min
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        for (int i = 0; i < 4; ++i) {
            minX = (lightCameraFrust.nearCorners[i].x < minX) ? lightCameraFrust.nearCorners[i].x : minX;
            minX = (lightCameraFrust.farCorners[i].x < minX) ? lightCameraFrust.farCorners[i].x : minX;

            maxX = (lightCameraFrust.nearCorners[i].x > maxX) ? lightCameraFrust.nearCorners[i].x : maxX;
            maxX = (lightCameraFrust.farCorners[i].x > maxX) ? lightCameraFrust.farCorners[i].x : maxX;

            minY = (lightCameraFrust.nearCorners[i].y < minY) ? lightCameraFrust.nearCorners[i].y : minY;
            minY = (lightCameraFrust.farCorners[i].y < minY) ? lightCameraFrust.farCorners[i].y : minY;

            maxY = (lightCameraFrust.nearCorners[i].y > maxY) ? lightCameraFrust.nearCorners[i].y : maxY;
            maxY = (lightCameraFrust.farCorners[i].y > maxY) ? lightCameraFrust.farCorners[i].y : maxY;

            minZ = (lightCameraFrust.nearCorners[i].z < minZ) ? lightCameraFrust.nearCorners[i].z : minZ;
            minZ = (lightCameraFrust.farCorners[i].z < minZ) ? lightCameraFrust.farCorners[i].z : minZ;

            maxZ = (lightCameraFrust.nearCorners[i].z > maxZ) ? lightCameraFrust.nearCorners[i].z : maxZ;
            maxZ = (lightCameraFrust.farCorners[i].z > maxZ) ? lightCameraFrust.farCorners[i].z : maxZ;
        }

        float halfWidth = 0.5f * (maxX - minX);
        float halfHeight = 0.5f * (maxY - minY);
        float zRange = maxZ - minZ;
        // Debug.Log(zRange);

        lightCameraFrust.nearCorners[0] = new Vector3(-halfWidth, -halfHeight, 0);
        lightCameraFrust.nearCorners[1] = new Vector3(halfWidth, -halfHeight, 0);
        lightCameraFrust.nearCorners[2] = new Vector3(halfWidth, halfHeight, 0);
        lightCameraFrust.nearCorners[3] = new Vector3(-halfWidth, halfHeight, 0);

        lightCameraFrust.farCorners[0] = new Vector3(-halfWidth, -halfHeight, zRange);
        lightCameraFrust.farCorners[1] = new Vector3(halfWidth, -halfHeight, zRange);
        lightCameraFrust.farCorners[2] = new Vector3(halfWidth, halfHeight, zRange);
        lightCameraFrust.farCorners[3] = new Vector3(-halfWidth, halfHeight, zRange);

        Vector3 pos = 0.5f * new Vector3(minX + maxX, minY + maxY, 2 * minZ);
        dirLightCameraObj.transform.position = dirLightCameraObj.transform.TransformPoint(pos);
        dirLightCameraObj.transform.rotation = dirLight.transform.rotation;

        dirLightCamera.nearClipPlane = 0;
        dirLightCamera.farClipPlane = zRange;
        dirLightCamera.aspect = halfWidth / halfHeight;
        dirLightCamera.orthographicSize = halfHeight;

        // dirLightCamera.nearClipPlane = minZ;
        // dirLightCamera.farClipPlane = maxZ;
        // dirLightCamera.aspect = halfWidth / halfHeight;
        // dirLightCamera.orthographicSize = halfHeight;
    }

    void OnDrawGizmos() {
        if (dirLightCamera == null) {
            return;
        }

        FrustumCorners fsc = new FrustumCorners();
        fsc.nearCorners = new Vector3[4];
        fsc.farCorners = new Vector3[4];
        for (int i = 0; i < 4; ++i) {
            fsc.nearCorners[i] = dirLightCameraObj.transform.TransformPoint(lightCameraFrust.nearCorners[i]);
            fsc.farCorners[i] = dirLightCameraObj.transform.TransformPoint(lightCameraFrust.farCorners[i]);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawLine(fsc.nearCorners[0], fsc.nearCorners[1]);
        Gizmos.DrawLine(fsc.nearCorners[1], fsc.nearCorners[2]);
        Gizmos.DrawLine(fsc.nearCorners[2], fsc.nearCorners[3]);
        Gizmos.DrawLine(fsc.nearCorners[3], fsc.nearCorners[0]);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(fsc.farCorners[0], fsc.farCorners[1]);
        Gizmos.DrawLine(fsc.farCorners[1], fsc.farCorners[2]);
        Gizmos.DrawLine(fsc.farCorners[2], fsc.farCorners[3]);
        Gizmos.DrawLine(fsc.farCorners[3], fsc.farCorners[0]);

        Gizmos.DrawLine(fsc.nearCorners[0], fsc.farCorners[0]);
        Gizmos.DrawLine(fsc.nearCorners[1], fsc.farCorners[1]);
        Gizmos.DrawLine(fsc.nearCorners[2], fsc.farCorners[2]);
        Gizmos.DrawLine(fsc.nearCorners[3], fsc.farCorners[3]);
        
    }
}
