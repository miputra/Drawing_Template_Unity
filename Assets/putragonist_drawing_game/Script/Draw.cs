/*

MIT License

Copyright (c) 2019 Muhammad Ihsan Diputra (https://github.com/Putragonist)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace putragonist_drawing_game { 
    public class Draw : MonoBehaviour
    {
        [Tooltip("Main Camera")]
        public Camera cam;

        [Tooltip("Render Camera To get All Texture")]
        public GameObject cameraRender;

        [Tooltip("Render Camera to get only active brush texture")]
        public Camera brushRender;

        [Tooltip("Brush GameObject")]
        public GameObject brush;
        Vector3 brushInitLocation; // brush position when first initiated
        public Renderer monitor;

        [Tooltip("Brush Color")]
        public Color brushColor;
        private Color lastBrushColor; // last brush color to compare if brush has changed

        [Tooltip("Scrollbar to change brush size")]
        public Scrollbar brushSizeScroll;
        public float maxBrushSize = 10; //max brush size
        public float minBrushSize = .1f; // min brush size
        public float brushSizeNow = .1f; //now brush size
        private float lastBrushSize = .1f; //last brush size to compare if brush has changed

        [Tooltip("List of color button to change color")]
        public List<Button> colorButton;
        Vector3 brushZ; //z position of brush
        private Queue<GameObject> brushPool = new Queue<GameObject>(); //brush pool for performance
        private Queue<GameObject> outOfPool = new Queue<GameObject>(); //temporary brush pool for redo previous undo.

        public GameObject drawScreenActive; //Screen to draw
        public GameObject drawScreensParent; //Parent of brush to keep it clean
        public GameObject drawScreenPrefab; // screen prefab to initate when last brush used    

        //How many time of stack until it joined
        public int maxUndoSize = 100;
        //how many texture gonna joined when max undo stack;
        public int maxUndoTextureSize = 50;

        //Call when scene start
        private void Start()
        {
            if (Info.isLoad)
            {
                OnLoad();
                Info.isLoad = false;
            }

            Init();
        
        }

        /// <summary>
        /// Initialization method
        /// </summary>
        private void Init()
        {
            cam = GetComponent<Camera>();
            //Set brush and add brush to pool
            brushInitLocation = brush.transform.position;
            brush.transform.localScale = new Vector3(brushSizeNow, brushSizeNow, 1);
            brushSizeScroll.numberOfSteps = (int)(maxBrushSize / minBrushSize);        
            brushColor = new Color32(180, 180, 180, 255);
            brush.GetComponent<SpriteRenderer>().color = brushColor;
            lastBrushColor = new Color(brushColor.r, brushColor.g, brushColor.b);
            brushZ = new Vector3(0, 0, brush.transform.localPosition.z);
            foreach (Button changeColor in colorButton)
            {
                Color color = changeColor.GetComponent<Image>().color;
                changeColor.onClick.AddListener(() => ChangeBrushColor(color));
            }
            AddPool();
        }  

        //Do save game
        public void DoSave()
        {
            SaveLoad.SaveTexture(JoinTextureNoReference());
        }

        //When User choose load
        public void OnLoad()
        {

            drawScreenActive.GetComponent<Renderer>().material.mainTexture = SaveLoad.LoadTextureToFile();

            GameObject newDrawScreen = (GameObject)Instantiate(drawScreenPrefab);
            newDrawScreen.transform.position = drawScreenActive.transform.position;
            newDrawScreen.GetComponent<CanvasCreator>().cam = brushRender.GetComponent<Camera>();
            newDrawScreen.GetComponent<CanvasCreator>().CreateMesh();

            Vector3 pp = drawScreensParent.transform.position;
            pp = drawScreensParent.transform.position;
            pp.z += 1;
            drawScreensParent.transform.position = pp;
            newDrawScreen.transform.parent = drawScreensParent.transform;

            drawScreenActive = newDrawScreen;
        
        }

    

        LayerMask maskArea = 1 << 9; //mask area to get
        LayerMask maskMonitor = 1 << 8; //mask area to get
        Vector3 mousePosition; //mouse position
        Vector3 lastMousePosition; //last mouse position to compare mouse change
        bool isLastMouse = false; //is mouse same as previous state
        private void Update()
        {
            // make sure player can't draw when mouse over UI
            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            //make brush follow mouse position
            mousePosition= cam.ScreenToWorldPoint(Input.mousePosition);
            mousePosition.z = brushZ.z;
            brush.transform.localPosition = mousePosition + brushZ;

            //change brush size with scroll value
            brushSizeNow = ((brushSizeScroll.value) * (maxBrushSize - minBrushSize)) + minBrushSize;

            //Change brush sprite color when brush color change from previous state
            if(brushColor != lastBrushColor)
            {

                brush.GetComponent<SpriteRenderer>().color = brushColor;
                lastBrushColor = new Color(brushColor.r, brushColor.g, brushColor.b);
            }

            //Join texture if player do undo for initiated count
            if(stackUndo.Count > maxUndoSize)
            {
                JoinActiveTexture();
            }
       
            //Change gameobject size when brush size different from previous state
            if (brushSizeNow != lastBrushSize)
            {
                brush.transform.localScale = new Vector3(brushSizeNow,brushSizeNow,1);            
            }

            //Destroy all redo gameobject when player start drawing
            if (Input.GetMouseButtonDown(0))
            {
                Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hitArea, Mathf.Infinity, maskArea);
                if (hitArea.collider != null && hitArea.collider.gameObject.name.Contains("AreaDrawing"))
                {
                    if(stackRedo.Count > 0)
                    {
                        while(stackRedo.Count > 0) {                     
                            GameObject pop = stackRedo.Pop();
                            Destroy(pop);
                        }
                    }
                }
            }

            // Do draw when player hit screen area with mouse
            if (Input.GetMouseButton(0))
            {
                mousePosition = cam.ScreenToWorldPoint(Input.mousePosition);
                Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hitArea, Mathf.Infinity, maskArea);
                Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, Mathf.Infinity, maskMonitor);
                if (lastMousePosition == mousePosition)
                    return;
                if (hitArea.collider != null && hitArea.collider.gameObject.name.Contains("AreaDrawing")) {
               
                     Draws(hit);
                }
                isLastMouse = true;
            }

            if (!Input.GetMouseButton(0))
            {
                isLastMouse = false;
            }
            //Join texture when player finish their mouse touch. Reset pool so it can be use anymore.
            if (Input.GetMouseButtonUp(0))
            {
                Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hitArea, Mathf.Infinity, maskArea);
                if (hitArea.collider != null && hitArea.collider.gameObject.name.Contains("AreaDrawing"))
                {
                    BrushApply();
                    ResetPool();
                    if (brushPool.Count < 100)
                    {
                        AddPool();
                    }
                    isLastMouse = false;
                }
            }

            //Undo and Redo
            if (Input.GetKey(KeyCode.LeftControl))
            {            
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    if(!Input.GetKey(KeyCode.LeftShift))
                        Undo();
                    else
                    {
                        Redo();
                    }
                }
            }
        }

        /// <summary>
        /// Add pool to increase performance isn instaniation is expensive
        /// </summary>
        public void AddPool()
        {        
                for (int i = 0; i < 1920; i++)
                {
                    GameObject go = Instantiate(brush);
                    go.transform.position = brushInitLocation;
                    brushPool.Enqueue(go);
                }        
        }

        /// <summary>
        /// Reset pool when texture layer instaniated so we don't need to add more brush
        /// </summary>
        public void ResetPool()
        {
            int outPoolCount = outOfPool.Count;
            for (int i = 0; i < outPoolCount; i++)
            {

                if (outOfPool.Count > 0)
                {
                    GameObject go = outOfPool.Dequeue();
                    go.transform.position = brushInitLocation;
                    brushPool.Enqueue(go);
                }
            }
        }

        /// <summary>
        /// Change brush color
        /// </summary>
        /// <param name="color">color you want brush to change</param>
        public void ChangeBrushColor(Color color)
        {
            brushColor = color;
        }

        /// <summary>
        /// Draw on screen
        /// </summary>
        /// <param name="hit">Where mouse is hit</param>
        public void Draws(RaycastHit hit)
        {
            GameObject go = hit.collider.gameObject;
            RenderTexture renderTexture = hit.collider.gameObject.GetComponent<Renderer>().material.mainTexture as RenderTexture;
            Vector2 pixelUV = hit.textureCoord;
            Camera cr = cameraRender.GetComponent<Camera>();

            if (brushPool.Count == 0)
                AddPool();
            GameObject go2 = brushPool.Dequeue();
            outOfPool.Enqueue(go2);
            Vector3 camLocation = new Vector3(pixelUV.x * (cr.pixelWidth), pixelUV.y * cr.pixelHeight, 25);
            go2.transform.localPosition = cr.ScreenToWorldPoint(camLocation);
            go2.GetComponent<SpriteRenderer>().color = brushColor;
            go2.transform.localScale = brush.transform.localScale;
            lastMousePosition = mousePosition;
        }

        /// <summary>
        /// Undo Process
        /// </summary>
        void Undo()
        {
            GameObject go = stackUndo.Pop();
            go.SetActive(false);
            stackRedo.Push(go);
        }

        /// <summary>
        /// Redo Process
        /// </summary>
        void Redo()
        {
            GameObject go = stackRedo.Pop();
            go.SetActive(true);
            stackUndo.Push(go);
        }

        Stack<GameObject> invertedStack = new Stack<GameObject>(); //temp stack to invert stack
        /// <summary>
        /// Join active texture in game
        /// </summary>
        void JoinActiveTexture()
        {        
            invertedStack.Clear();
            while(stackUndo.Count > 0)
            {
                GameObject stacked = stackUndo.Pop();
                invertedStack.Push(stacked);
            }
            GameObject lastStack = invertedStack.Pop();
            Texture2D text = lastStack.GetComponent<Renderer>().material.mainTexture as Texture2D;
            while(invertedStack.Count > maxUndoTextureSize)
            {
                GameObject stacked2 = invertedStack.Pop();
                Texture2D text2 = stacked2.GetComponent<Renderer>().material.mainTexture as Texture2D;
                for(int w=0; w < text.width; w++)
                {
                    for(int h=0; h< text.height; h++)
                    {
                    
                        Color c1 = text.GetPixel(w, h);
                        Color c2 = text2.GetPixel(w, h);
                        Color f = Color.Lerp(c1, c2, c2.a / 1f);
                        text.SetPixel(w, h, f);
                    }
                }
                Destroy(stacked2);
            }
            text.Apply();
            lastStack.GetComponent<Renderer>().material.mainTexture = text;
            stackUndo.Push(lastStack);
            while(invertedStack.Count > 0)
            {
                stackUndo.Push(invertedStack.Pop());
            }
        }

        /// <summary>
        /// Join texture without changing anything in scene
        /// </summary>
        /// <returns>Texture 2D</returns>
        Texture2D JoinTextureNoReference()
        {
            invertedStack.Clear();
            Stack<GameObject> stackTexture = new Stack<GameObject>(this.stackUndo);
            while (stackTexture.Count > 0)
            {
                GameObject stacked = stackTexture.Pop();
                invertedStack.Push(stacked);
            }
            GameObject lastStack = invertedStack.Pop();
            Texture2D tempText = lastStack.GetComponent<Renderer>().material.mainTexture as Texture2D;
            Texture2D text = new Texture2D(tempText.width, tempText.height);
            text.SetPixels(tempText.GetPixels());
            while (invertedStack.Count > 0)
            {
                GameObject stacked2 = invertedStack.Pop();
                Texture2D text2 = stacked2.GetComponent<Renderer>().material.mainTexture as Texture2D;
                for (int w = 0; w < text.width; w++)
                {
                    for (int h = 0; h < text.height; h++)
                    {
                        Color c1 = text.GetPixel(w, h);
                        Color c2 = text2.GetPixel(w, h);
                        Color f = Color.Lerp(c1, c2, c2.a / 1f);
                        text.SetPixel(w, h, f);
                    }
                }
            }
        
            text.Apply();
            return text;
        }

        Stack<GameObject> stackUndo = new Stack<GameObject>(); //Gameobject to undo
        Stack<GameObject> stackRedo = new Stack<GameObject>(); //Gameobject to redo

        /// <summary>
        /// Apply gameobject brush to single texture to increase performance, since too many instaniated brush gonna be too many drawcall
        /// </summary>
        void BrushApply()
        {
            Material mat = drawScreenActive.GetComponent<Renderer>().material;
            Material matCopy = new Material(mat);
            RenderTexture rd = brushRender.GetComponent<Camera>().targetTexture as RenderTexture;
        
            Texture2D newTexture = RenderToTexture2D(rd);

            matCopy.mainTexture = newTexture;
        

            drawScreenActive.GetComponent<Renderer>().material = matCopy;     

            int layerDraw = drawScreenActive.layer + 0;
            drawScreenActive.layer = 0;
            stackUndo.Push(drawScreenActive);

            GameObject newDrawScreen = (GameObject)Instantiate(drawScreenPrefab);
            newDrawScreen.transform.position = drawScreenActive.transform.position;
            newDrawScreen.GetComponent<CanvasCreator>().cam = brushRender.GetComponent<Camera>();
            newDrawScreen.GetComponent<CanvasCreator>().CreateMesh();
        
            Vector3 pp = drawScreensParent.transform.position;
            pp = drawScreensParent.transform.position;
            pp.z += 1;
            drawScreensParent.transform.position = pp;
            newDrawScreen.transform.parent = drawScreensParent.transform;
            drawScreenActive = newDrawScreen;
        }

        /// <summary>
        /// Render texture to Texture2D
        /// </summary>
        /// <param name="renderTexture"></param>
        /// <returns></returns>
        Texture2D RenderToTexture2D(RenderTexture renderTexture)
        {
            Texture2D tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
            RenderTexture.active = renderTexture;
            tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Check all color in texture is transparent
        /// </summary>
        /// <param name="tex">texture2d</param>
        /// <returns>if all transparent return true</returns>
        bool IsAllTransparant(Texture2D tex)
        {
            for(int i=0; i < tex.width; i++)
            {
                for(int j=0; j<tex.height; i++)
                {
                    if (tex.GetPixel(i, j).a > 0)
                        return false;
                }
            }


            return true;
        }
    }

    /// <summary>
    /// Info state of game
    /// </summary>
    public class Info
    {
        public static bool isLoad = false;    
    }
}