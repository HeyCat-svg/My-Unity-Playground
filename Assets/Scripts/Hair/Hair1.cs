using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hair1 : MonoBehaviour {

    public float headRadius = 1.0f;
    public int hairNum = 50;
    public int hairNodeNum = 50;
    public float hairNodeLength = 0.05f;
    public float hairWidth = 0.01f;
    public Material hairMaterial;
    public GameObject mCamera;
    public GameObject head;
    public MeshFilter meshDrawer;
    
    struct Node {
        public Vector3 p0, p1;
        public float length;
    }

    struct Strand {
        public int nodeStart;
        public int nodeEnd;
        public Vector3 rootP;      // 相对于头的局部坐标
    }

    private Strand[] strands;
    private Node[] nodes;


    void Start() {
        InitHair();
    }


    void Update() {
        DrawHair();
    }

    
    void FixedUpdate() {
        UpdateHairState();
    }


    void OnDrawGizmosSelected() {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(head.transform.position, headRadius);
    }


    void InitHair() {
        strands = new Strand[hairNum];
        nodes = new Node[hairNum * hairNodeNum];

        // get head2World matrix


        for (int i = 0; i < hairNum; ++i) {
            Strand aStrend = new Strand();
            aStrend.nodeStart = i * hairNodeNum;
            aStrend.nodeEnd = (i + 1) * hairNodeNum - 1;

            // 生成发根相对坐标
            float theta = Random.Range(Mathf.PI, 2 * Mathf.PI);
            float phi = Random.Range(0, Mathf.PI / 2.0f);       // 上半球
            Vector3 relPos;
            relPos.x = headRadius * Mathf.Cos(phi) * Mathf.Cos(theta);
            relPos.z = headRadius * Mathf.Cos(phi) * Mathf.Sin(theta);
            relPos.y = headRadius * Mathf.Sin(phi);
            aStrend.rootP = relPos;

            strands[i] = aStrend;

            // 初始化发丝位置
            Vector3 rootPWorld = head.transform.TransformPoint(relPos);
            nodes[aStrend.nodeStart].p0 = rootPWorld;
            nodes[aStrend.nodeStart].p1 = rootPWorld;
            nodes[aStrend.nodeStart].length = hairNodeLength;
            Vector3 hairDir = (rootPWorld - head.transform.position).normalized;

            for (int j = aStrend.nodeStart + 1; j <= aStrend.nodeEnd; ++j) {
                Node node = new Node();
                node.length = hairNodeLength;
                node.p0 = rootPWorld + (j - aStrend.nodeStart) * hairNodeLength * hairDir;
                node.p1 = node.p0;

                nodes[j] = node;
            }
        }
    }


    Vector3 Verlet(Vector3 p0, Vector3 p1, float damping, Vector3 a, float dt) {
        Vector3 result = p1 + damping * (p1 - p0) + a * dt * dt;
        return result;
    }


    Vector3 CollideSphere(Vector3 spherePos, float radius, Vector3 p) {
        Vector3 dir = p - spherePos;
        float distance = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y + dir.z * dir.z);
        if (distance < radius) {
            p = p + (radius - distance) * dir.normalized;
        }
        return p;
    }


    Vector3[] LengthConstraint(Vector3 p1, Vector3 p2, float length) {
        Vector3 deltaP = p2 - p1;
        float m = deltaP.magnitude;
        Vector3 offset = deltaP * (m - length) / (2 * m);

        Vector3[] ret = new Vector3[2];
        ret[0] = p1 + offset;
        ret[1] = p2 - offset;

        return ret;
    }


    void UpdateHairState() {
        Vector3 a = new Vector3(0, -9.8f, 0);

        int nodeSize = nodes.Length;
        for (int i = 0; i < nodeSize; ++i) {
            Node n = nodes[i];
            Vector3 p2 = Verlet(n.p0, n.p1, 0.99f, a, Time.deltaTime);
            n.p0 = n.p1;
            n.p1 = p2;
            nodes[i] = n;
        }

        for (int i = 0; i < hairNum; ++i) {
            Strand aStrand = strands[i];
            int nodeStart = aStrand.nodeStart;
            int nodeEnd = aStrand.nodeEnd;

            for (int iter = 0; iter < 3; ++iter) {
                for (int j = nodeStart; j < nodeEnd; ++j) {     // 遍历到倒数第二个为止
                    Node na = nodes[j];
                    Node nb = nodes[j + 1];
                    
                    // 球形碰撞检测
                    nb.p1 = CollideSphere(head.transform.position, headRadius, nb.p1);
                    // 长度约束
                    Vector3[] ret = LengthConstraint(na.p1, nb.p1, nb.length);
                    na.p1 = ret[0];
                    nb.p1 = ret[1];

                    nodes[j] = na;
                    nodes[j + 1] = nb;
                }
                
                // 固定发根
                nodes[nodeStart].p1 = head.transform.TransformPoint(aStrand.rootP);
            }
        }
    }


    void DrawHair() {
        Vector3 cameraDir = -mCamera.transform.forward;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int idx = -1;

        Vector3 dir, lastNodePos = Vector3.zero;
        for (int i = 0; i < hairNum; ++i) {
            int nodeStart = strands[i].nodeStart;
            int nodeEnd = strands[i].nodeEnd;

            for (int j = nodeStart; j <= nodeEnd; ++j) {
                Vector3 p = nodes[j].p1;

                if (j != nodeStart) {
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
                else {
                    verts.Add(p);
                    uvs.Add(new Vector2(0.5f, 0.5f));
                    dir = Vector3.Cross(nodes[nodeStart].p1 - nodes[nodeStart + 1].p1, cameraDir).normalized;
                    verts.Add(p + dir * hairWidth);
                    uvs.Add(new Vector2(0.5f, 0.5f));

                    idx += 2;
                    lastNodePos = p;
                }
            }
        }

        Mesh m = new Mesh();
        m.vertices = verts.ToArray();
        m.triangles = tris.ToArray();
        m.uv = uvs.ToArray();
        m.RecalculateNormals();

        // draw hair mesh
        // Bounds bounds = new Bounds(head.transform.position, 3.0f * Vector3.one);
        // Graphics.DrawMeshInstancedProcedural(m, 0, hairMaterial, bounds, 1);

        meshDrawer.mesh = m;
    }
}
