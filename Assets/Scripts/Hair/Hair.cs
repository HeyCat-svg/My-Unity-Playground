using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hair : MonoBehaviour {

    public Vector3 headPos = Vector3.zero;
    public float headRadius = 1.0f;
    public int hairNum = 50;
    public int hairNodeNum = 50;
    public float hairNodeLength = 0.05f;
    public float hairWidth = 0.01f;
    public Material hairMaterial;
    public GameObject mCamera;
    
    private struct Node {   // 节点粒子
        public Vector3 p0, p1;     // 前帧/本帧的位置
        public float length;       // 和上一节的止动长度
    }

    private struct Strand { // 发丝
        public Node[] nodes;
        public Vector3 rootP;
    }

    private Strand[] strands;


    void Start() {
        InitHair();
    }


    void FixedUpdate() {
        DrawHair();
    }


    void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(headPos, headRadius);
    }


    void InitHair() {
        strands = new Strand[hairNum];

        for (int i = 0; i < hairNum; ++i) {
            float theta = Random.Range(Mathf.PI, 2 * Mathf.PI);
            float phi = Random.Range(0, Mathf.PI / 2.0f);       // 上半球
            Vector3 newPos;
            newPos.x = headRadius * Mathf.Cos(phi) * Mathf.Cos(theta);
            newPos.z = headRadius * Mathf.Cos(phi) * Mathf.Sin(theta);
            newPos.y = headRadius * Mathf.Sin(phi);

            Strand aStrand = new Strand();
            aStrand.rootP = newPos + headPos;
            aStrand.nodes = new Node[hairNodeNum];
            Vector3 hairDir = new Vector3(newPos.x, 0.0f, newPos.z).normalized;

            for (int j = 0; j < hairNodeNum; ++j) {
                Node node = new Node();
                node.length = hairNodeLength;
                node.p0 = aStrand.rootP + (j + 1) * hairNodeLength * hairDir;
                node.p1 = node.p0;

                aStrand.nodes[j] = node;
            }

            strands[i] = aStrand;
        }
    }


    Vector3 Verlet(Vector3 p0, Vector3 p1, float damping, Vector3 a, float dt) {
        Vector3 result = p1 + damping * (p1 - p0) + a * dt * dt;
        return result;
    }


    Vector3 CollideSphere(Vector3 pos, float radius, Vector3 p) {

        return new Vector3();
    }


    Vector3[] LengthConstraint(Vector3 p1, Vector3 p2, float length) {

        return new Vector3[2];
    }


    void UpdateHairState() {
        Vector3 a = new Vector3(0, -9.8f, 0);

        for (int i = 0; i < hairNum; ++i) {
            Node[] nodes = strands[i].nodes;

            for (int j = 0; j < hairNodeNum; ++j) {
                Node tmpN = nodes[j];
                Vector3 p2 = Verlet(tmpN.p0, tmpN.p1, 1, a, Time.deltaTime);
                tmpN.p0 = tmpN.p1;
                tmpN.p1 = p2;
                nodes[j] = tmpN;
            }
        }

        // 松弛法解约束
        for (int i = 0; i < hairNum; ++i) {
            Node[] nodes = strands[i].nodes;
            Vector3 rootP = strands[i].rootP;
        
            // 迭代3次
            for (int iter = 0; iter < 3; ++iter) {
                Vector3 lastNodePos = rootP;
                
                for (int j = 0; j < hairNodeNum; ++j) {
                    // 碰撞检测和决议
                    nodes[j].p1 = CollideSphere(headPos, headRadius, nodes[j].p1);
                    if (j != 0) {
                        // 长度约束
                        Vector3[] ret = LengthConstraint(nodes[j - 1].p1, nodes[j].p1, nodes[j].length);
                        nodes[j - 1].p1 = ret[0];
                        nodes[j].p1 = ret[1];
                    }
                }
                
            }
        }
    }


    void DrawHair() {
        Vector3 cameraDir = -mCamera.transform.forward;

        // 根据线段生成网格
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int idx = -1;

        for (int i = 0; i < hairNum; ++i) {
            Strand aStrand = strands[i];
            Node[] nodes = aStrand.nodes;

            verts.Add(aStrand.rootP);
            uvs.Add(new Vector2(0.5f, 0.5f));
            Vector3 dir = Vector3.Cross(nodes[0].p1 - nodes[1].p1, cameraDir).normalized;
            verts.Add(aStrand.rootP + dir * hairWidth);
            uvs.Add(new Vector2(0.5f, 0.5f));
            idx += 2;

            Vector3 lastNodePos = aStrand.rootP;
            for (int j = 0; j < hairNodeNum; ++j) {
                Vector3 p = nodes[j].p1;
                verts.Add(p);
                uvs.Add(new Vector2(0.5f, 0.5f));
                dir = Vector3.Cross(lastNodePos - p, cameraDir).normalized;
                verts.Add(p + dir * hairWidth);
                uvs.Add(new Vector2(0.5f, 0.5f));

                idx += 2;
                tris.Add(idx - 3);      // top-left
                tris.Add(idx - 1);      // bottom-left
                tris.Add(idx);          // bottom-right
                tris.Add(idx);          // bottom-right
                tris.Add(idx - 2);      // top-right
                tris.Add(idx - 3);      // top-left

                lastNodePos = p;
            }
        }

        Mesh m = new Mesh();
        m.vertices = verts.ToArray();
        m.triangles = tris.ToArray();
        m.uv = uvs.ToArray();
        m.RecalculateNormals();
        
        // draw hair mesh
        Bounds bounds = new Bounds(headPos, 3.0f * Vector3.one);
        Graphics.DrawMeshInstancedProcedural(m, 0, hairMaterial, bounds, 1);
    }
}
