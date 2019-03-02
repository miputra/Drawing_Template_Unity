using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace putragonist_drawing_game
{
    /// <summary>
    /// Method to create a mesh as canvas
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteInEditMode]
    public class CanvasCreator : MonoBehaviour
    {
        public Camera cam;
        Vector3[] vertices = new Vector3[4];
        MeshFilter mesh_filter;
        public bool createMesh = false;

        // Start is called before the first frame update
        void Start()
        {
            if (cam == null)
            {

            }
            if (mesh_filter == null)
            {
                //CreateMesh();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (createMesh)
            {
                CreateMesh();
                createMesh = false;
            }
        }
        [SerializeField] Mesh mesh;
        public void CreateMesh()
        {
            mesh_filter = GetComponent<MeshFilter>();
            mesh = new Mesh();
            mesh.name = "Drawing Canvas";

            mesh_filter.mesh = mesh;

            Vector3 sizeRaw = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, 0)) - cam.transform.position;
            sizeRaw.x = Mathf.Abs(sizeRaw.x);
            sizeRaw.y = Mathf.Abs(sizeRaw.y);
            sizeRaw.z = 0;


            float height = cam.orthographicSize * 2;
            float width = height * (sizeRaw.x / sizeRaw.y);

            Vector3 camPosition = new Vector3(width, height, 0);
            vertices[0] = new Vector3(-camPosition.x / 2, -camPosition.y / 2, 0);
            vertices[1] = new Vector3(camPosition.x - (camPosition.x / 2), -camPosition.y / 2, 0);
            vertices[2] = new Vector3(-camPosition.x / 2, camPosition.y - (camPosition.y / 2), 0);
            vertices[3] = new Vector3(camPosition.x - (camPosition.x / 2), camPosition.y - (camPosition.y / 2), 0);
            mesh.vertices = vertices;

            int[] tris = new int[6];
            //  Lower left triangle.
            tris[0] = 0;
            tris[1] = 2;
            tris[2] = 1;

            //  Upper right triangle.   
            tris[3] = 2;
            tris[4] = 3;
            tris[5] = 1;

            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4];
            normals[0] = Vector3.forward;
            normals[1] = Vector3.forward;
            normals[2] = Vector3.forward;
            normals[3] = Vector3.forward;

            mesh.normals = normals;
            mesh.RecalculateNormals();


            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(0, 0);
            uvs[1] = new Vector2(1, 0);
            uvs[2] = new Vector2(0, 1);
            uvs[3] = new Vector2(1, 1);

            mesh.uv = uvs;
        }
    }
}