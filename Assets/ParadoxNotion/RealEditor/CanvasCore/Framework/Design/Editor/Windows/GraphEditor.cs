#if UNITY_EDITOR

using NodeCanvas.Framework;
using ParadoxNotion;
using ParadoxNotion.Design;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using CanvasGroup = NodeCanvas.Framework.CanvasGroup;
using Logger = ParadoxNotion.Services.Logger;

namespace NodeCanvas.Editor
{

    public partial class GraphEditor : EditorWindow
    {

        //the root graph that was first opened in the editor
        [System.NonSerialized]
        private Graph _rootGraph;
        private int _rootGraphID;

        //the GrapOwner if any, that was used to open the editor and from which to read the rootGraph
        [System.NonSerialized]
        private GraphOwner _targetOwner;
        private int _targetOwnerID;

        ///----------------------------------------------------------------------------------------------

        //the current instance of the opened editor
        public static GraphEditor current { get; private set; }
        //the current graph loaded for editing. Can be a nested graph of the root graph
        public static Graph currentGraph { get; private set; }

        ///----------------------------------------------------------------------------------------------
        private const float ZOOM_MAX = 1f;
        private const float ZOOM_MIN = 0.25f;
        private const int TAB_HEIGHT = 21;
        private const int TOP_MARGIN = TAB_HEIGHT + 0;
        private const int BOTTOM_MARGIN = 5;
        private const int SIDE_MARGIN = 5;
        private const int GRID_SIZE = 15;
        private static Rect canvasRect; //rect within which the graph is drawn (the window)
        private static Rect viewRect; //the panning rect that is drawn within canvasRect
        private static Rect minimapRect; //rect to show minimap within

        ///----------------------------------------------------------------------------------------------

        private static Event e;
        private static bool isMultiSelecting;
        private static Vector2 selectionStartPos;
        private static bool isResizingMinimap;
        private static bool isDraggingMinimap;
        private static bool willRepaint = true;
        private static bool fullDrawPass = true;
        private static System.Action OnDoPopup;

        private static Node[] tempCanvasGroupNodes;
        private static CanvasGroup[] tempCanvasGroupGroups;

        ///----------------------------------------------------------------------------------------------

        private static float lastUpdateTime = -1;
        private static Vector2? smoothPan;
        private static float? smoothZoomFactor;
        private static Vector2 _panVelocity = Vector2.one;
        private static float _zoomVelocity = 1;
        private static float pingValue;
        private static Rect pingRect;

        ///----------------------------------------------------------------------------------------------

        private static bool welcomeShown;

        ///----------------------------------------------------------------------------------------------

        public static event System.Action<Graph> onCurrentGraphChanged;

        //The graph from which we start editing
        public static Graph rootGraph
        {
            get
            {
                if (current._rootGraph == null)
                {
                    current._rootGraph = EditorUtility.InstanceIDToObject(current._rootGraphID) as Graph;
                }
                return current._rootGraph;
            }
            private set
            {
                current._rootGraph = value;
                current._rootGraphID = value != null ? value.GetInstanceID() : 0;
            }
        }

        //The owner of the root graph if any
        public static GraphOwner targetOwner
        {
            get
            {
                if (current == null)
                { //this fix the maximize/minimize window
                    current = OpenWindow();
                }

                if (current._targetOwner == null)
                {
                    current._targetOwner = EditorUtility.InstanceIDToObject(current._targetOwnerID) as GraphOwner;
                }
                return current._targetOwner;
            }
            private set
            {
                current._targetOwner = value;
                current._targetOwnerID = value != null ? value.GetInstanceID() : 0;
            }
        }

        //The translation of the graph
        private static Vector2 pan
        {
            get { return currentGraph != null ? currentGraph.translation : viewCanvasCenter; }
            set
            {
                if (currentGraph != null)
                {
                    Vector2 t = value;
                    if (smoothPan == null)
                    {
                        t.x = Mathf.Round(t.x); //pixel perfect correction
                        t.y = Mathf.Round(t.y); //pixel perfect correction
                    }
                    currentGraph.translation = t;
                }
            }
        }

        //The zoom factor of the graph
        private static float zoomFactor
        {
            get { return currentGraph != null ? Mathf.Clamp(currentGraph.zoomFactor, ZOOM_MIN, ZOOM_MAX) : ZOOM_MAX; }
            set
            {
                if (currentGraph != null)
                {
                    currentGraph.zoomFactor = Mathf.Clamp(value, ZOOM_MIN, ZOOM_MAX);
                }
            }
        }

        //The center of the canvas
        private static Vector2 viewCanvasCenter
        {
            get { return viewRect.size / 2; }
        }

        //The mouse position in the canvas
        private static Vector2 mousePosInCanvas
        {
            get { return ViewToCanvas(Event.current.mousePosition); }
        }

        //window width. Handles retina
        private static float screenWidth
        {
            get { return Screen.width / EditorGUIUtility.pixelsPerPoint; }
        }

        //window height. Handles retina
        private static float screenHeight
        {
            get { return Screen.height / EditorGUIUtility.pixelsPerPoint; }
        }

        ///----------------------------------------------------------------------------------------------
        //...
        private void OnEnable()
        {
            current = this;
            titleContent = new GUIContent("Canvas", StyleSheet.canvasIcon);

            willRepaint = true;
            fullDrawPass = true;
            wantsMouseMove = true;
            minSize = new Vector2(700, 300);

            EditorApplication.playModeStateChanged -= OnPlayModeChange;
            EditorApplication.playModeStateChanged += OnPlayModeChange;

#if UNITY_2018_3_OR_NEWER
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
#endif

            Selection.selectionChanged -= OnUnityObjectSelectionChange;
            Selection.selectionChanged += OnUnityObjectSelectionChange;

            Logger.RemoveListener(OnLogMessageReceived);
            Logger.AddListener(OnLogMessageReceived);

            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        //...
        private void OnDisable()
        {
            current = null;
            welcomeShown = false;
            GraphEditorUtility.activeElement = null;
            GraphEditorUtility.activeElements = null;
            tempCanvasGroupNodes = null;
            tempCanvasGroupGroups = null;

            EditorApplication.playModeStateChanged -= OnPlayModeChange;

#if UNITY_2018_3_OR_NEWER
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
#endif

            Selection.selectionChanged -= OnUnityObjectSelectionChange;
            Logger.RemoveListener(OnLogMessageReceived);
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            // AssetDatabase.SaveAssets();
        }

        //...
        private void OnUndoRedoPerformed()
        {
            if (current == null || currentGraph == null) { return; }
            GraphEditorUtility.activeElement = null;
            GraphEditorUtility.activeElements = null;
            tempCanvasGroupNodes = null;
            tempCanvasGroupGroups = null;
            GUIUtility.hotControl = 0;
            GUIUtility.keyboardControl = 0;
            willRepaint = true;
            fullDrawPass = true;
            if (!Application.isPlaying)
            {
                UpdateReferencesAndNodeIDs();
                currentGraph.Validate();
            }
        }

        //...
        private void OnPlayModeChange(PlayModeStateChange state)
        {
            RemoveNotification();
            GraphEditorUtility.activeElement = null;
            welcomeShown = true;
            willRepaint = true;
            fullDrawPass = true;
        }

#if UNITY_2018_3_OR_NEWER
        private void OnPrefabStageClosing(UnityEditor.Experimental.SceneManagement.PrefabStage stage)
        {
            //when exiting prefab state we are left with a floating graph instance which can creat confusion
            SetReferences(null, null, null);
        }
#endif

        //Change viewing graph based on Graph or GraphOwner
        private void OnUnityObjectSelectionChange()
        {

            if (Prefs.isEditorLocked && rootGraph != null)
            {
                return;
            }

            if (Selection.activeObject is GraphOwner)
            {
                SetReferences((GraphOwner)Selection.activeObject);
                return;
            }

            if (Selection.activeObject is Graph)
            {
                SetReferences((Graph)Selection.activeObject);
                return;
            }

            if (Selection.activeGameObject != null)
            {
                GraphOwner foundOwner = Selection.activeGameObject.GetComponent<GraphOwner>();
                if (foundOwner != null) { SetReferences(foundOwner); }
            }
        }

        //Listen to Logs and return true if handled
        private bool OnLogMessageReceived(Logger.Message msg)
        {
            if (msg.tag == LogTag.EDITOR)
            {
                if (!string.IsNullOrEmpty(msg.text))
                {
                    ShowNotification(new GUIContent(msg.text));
                }
                return true;
            }
            return false;
        }

        //Whenever the graph we are viewing has changed and after the fact.
        private void OnCurrentGraphChanged()
        {
            UpdateReferencesAndNodeIDs();
            GraphEditorUtility.activeElement = null;
            willRepaint = true;
            fullDrawPass = true;
            smoothPan = null;
            smoothZoomFactor = null;
            if (onCurrentGraphChanged != null)
            {
                onCurrentGraphChanged(currentGraph);
            }
        }

        //Update the references for editor convenience.
        private void UpdateReferencesAndNodeIDs()
        {

            rootGraph = targetOwner != null ? targetOwner.graph : rootGraph;

            //do this in editor only. In runtime these are updated when graph initialize anyway
            if (!Application.isPlaying && rootGraph != null)
            {
                rootGraph.UpdateNodeIDs(true);
                rootGraph.UpdateReferencesFromOwner(targetOwner, true);

                //update refs for the currenlty viewing nested graph as well
                Graph deepGraph = GetCurrentGraph(rootGraph);
                deepGraph.UpdateNodeIDs(true);
                deepGraph.UpdateReferencesFromOwner(targetOwner, true);
            }
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OpenAsset(int instanceID, int line)
        {
            Graph target = EditorUtility.InstanceIDToObject(instanceID) as Graph;
            if (target != null)
            {
                GraphEditor.OpenWindow(target);
                return true;
            }
            return false;
        }

        ///Open the window without any references
        public static GraphEditor OpenWindow() { return OpenWindow(null, null, null); }
        ///Opening the window for a graph owner
        public static GraphEditor OpenWindow(GraphOwner owner) { return OpenWindow(owner.graph, owner, owner.blackboard); }
        ///For opening the window from gui button in the nodegraph's Inspector.
        public static GraphEditor OpenWindow(Graph newGraph) { return OpenWindow(newGraph, null, newGraph.blackboard); }
        ///Open GraphEditor initializing target graph
        public static GraphEditor OpenWindow(Graph newGraph, GraphOwner owner, IBlackboard blackboard)
        {
            GraphEditor window = GetWindow<GraphEditor>();
            SetReferences(newGraph, owner, blackboard);
            if (Prefs.showWelcomeWindow && !Application.isPlaying && welcomeShown == false)
            {
                welcomeShown = true;
                System.Type graphType = newGraph != null ? newGraph.GetType() : null;
                WelcomeWindow.ShowWindow(graphType);
            }
            return window;
        }

        ///Set GraphEditor inspected references
        public static void SetReferences(GraphOwner newOwner) { SetReferences(newOwner.graph, newOwner, newOwner.blackboard); }
        ///Set GraphEditor inspected references
        public static void SetReferences(Graph newGraph) { SetReferences(newGraph, null, newGraph.blackboard); }
        ///Set GraphEditor inspected references
        public static void SetReferences(Graph newGraph, GraphOwner newOwner, IBlackboard newBlackboard)
        {
            if (current == null)
            {
                return;
            }
            willRepaint = true;
            fullDrawPass = true;
            rootGraph = newGraph;
            targetOwner = newOwner;
            if (rootGraph != null)
            {
                rootGraph.SetCurrentChildGraphAssignable(null);
                if (!Application.isPlaying)
                {
                    rootGraph.UpdateNodeIDs(true);
                    rootGraph.UpdateReferences(newOwner, newBlackboard, true);
                }
            }
            GraphEditorUtility.activeElement = null;
            current.Repaint();
        }

        ///Editor update
        private void Update()
        {
            float currentTime = Time.realtimeSinceStartup;
            float deltaTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            bool needsRepaint = false;
            needsRepaint |= UpdateSmoothPan(deltaTime);
            needsRepaint |= UpdateSmoothZoom(deltaTime);
            needsRepaint |= UpdatePing(deltaTime);
            if (needsRepaint)
            {
                Repaint();
            }
        }

        ///Update smooth pan
        private bool UpdateSmoothPan(float deltaTime)
        {

            if (smoothPan == null)
            {
                return false;
            }

            Vector2 targetPan = (Vector2)smoothPan;
            if ((targetPan - pan).magnitude < 0.1f)
            {
                smoothPan = null;
                return false;
            }

            targetPan = new Vector2(Mathf.FloorToInt(targetPan.x), Mathf.FloorToInt(targetPan.y));
            pan = Vector2.SmoothDamp(pan, targetPan, ref _panVelocity, 0.08f, Mathf.Infinity, deltaTime);
            return true;
        }

        ///Update smooth pan
        private bool UpdateSmoothZoom(float deltaTime)
        {

            if (smoothZoomFactor == null)
            {
                return false;
            }

            float targetZoom = (float)smoothZoomFactor;
            if (Mathf.Abs(targetZoom - zoomFactor) < 0.00001f)
            {
                smoothZoomFactor = null;
                return false;
            }

            zoomFactor = Mathf.SmoothDamp(zoomFactor, targetZoom, ref _zoomVelocity, 0.08f, Mathf.Infinity, deltaTime);
            if (Mathf.Abs(1 - zoomFactor) < 0.00001f) { zoomFactor = 1; }
            return true;
        }

        ///Update ping value
        private bool UpdatePing(float deltaTime)
        {
            if (pingValue > 0)
            {
                pingValue -= deltaTime;
                return true;
            }
            return false;
        }

        ///----------------------------------------------------------------------------------------------

        //GUI space to canvas space
        private static Vector2 ViewToCanvas(Vector2 viewPos)
        {
            return (viewPos - pan) / zoomFactor;
        }

        //Canvas space to GUI space
        private static Vector2 CanvasToView(Vector2 canvasPos)
        {
            return (canvasPos * zoomFactor) + pan;
        }

        //Show modal quick popup
        private static void DoPopup(System.Action Call)
        {
            OnDoPopup = Call;
        }

        //Just so that there is some repainting going on
        private void OnInspectorUpdate()
        {
            if (!willRepaint)
            {
                Repaint();
            }
        }

        ///----------------------------------------------------------------------------------------------

        //...
        private void OnGUI()
        {

            //Init gui
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.skin.label.richText = true;
            e = Event.current;
            GraphEditorUtility.realMousePosition = e.mousePosition;

            //canvas an minimap rects
            canvasRect = Rect.MinMaxRect(SIDE_MARGIN, TOP_MARGIN, position.width - SIDE_MARGIN, position.height - BOTTOM_MARGIN);
            float aspect = canvasRect.width / canvasRect.height;
            minimapRect = Rect.MinMaxRect(canvasRect.xMax - (Prefs.minimapSize.y * aspect), canvasRect.yMax - Prefs.minimapSize.y, canvasRect.xMax - 2, canvasRect.yMax - 2);
            //canvas bg
            Styles.Draw(canvasRect, StyleSheet.canvasBG);

            if (!CheckSumOK())
            {
                return;
            }

            ///----------------------------------------------------------------------------------------------

            if (e.type == EventType.MouseDown)
            {
                RemoveNotification();
            }

            if (mouseOverWindow == current && (e.isMouse || e.isKey))
            {
                willRepaint = true;
            }

            ///should we set dirty? Put in practise at the end
            bool willDirty = e.rawType == EventType.MouseUp;


            //background grid
            DrawGrid(canvasRect, pan, zoomFactor);
            //handle minimap
            HandleMinimapEvents(minimapRect);
            //PRE nodes events
            HandlePreNodesGraphEvents(currentGraph, mousePosInCanvas);

            //begin zoom
            Rect originalCanvasRect = canvasRect;
            Matrix4x4 originalMatrix = default(Matrix4x4);
            if (zoomFactor != 1)
            {
                canvasRect = StartZoomArea(canvasRect, zoomFactor, out originalMatrix);
            }

            {
                // calc viewRect
                viewRect = canvasRect;
                viewRect.x = 0;
                viewRect.y = 0;
                viewRect.position -= pan / zoomFactor;

                //main group
                GUI.BeginClip(canvasRect, pan / zoomFactor, default(Vector2), false);
                {
                    DoCanvasGroups();
                    BeginWindows();
                    ShowNodesGUI(currentGraph, viewRect, fullDrawPass, mousePosInCanvas, zoomFactor);
                    EndWindows();
                    DrawPings();
                    DoCanvasRectSelection();
                }
                GUI.EndClip();
            }

            //end zoom
            if (zoomFactor != 1 && originalMatrix != default(Matrix4x4))
            {
                EndZoomArea(originalMatrix);
                //set original back
                canvasRect = originalCanvasRect;
            }

            DrawMinimap(minimapRect);
            StartBreadCrumbNavigation(rootGraph);
            HandlePostNodesGraphEvents(currentGraph, mousePosInCanvas);
            ShowToolbar(currentGraph);
            ShowPanels(currentGraph, mousePosInCanvas);
            AcceptDrops(currentGraph, mousePosInCanvas);
            ShowPlaymodeGUI();
            // ShowConsoleLog();

            //dirty?
            if (willDirty)
            {
                willDirty = false;
                willRepaint = true;
                currentGraph.SelfSerialize();
                UndoUtility.SetDirty(currentGraph);
                if (targetOwner != null && targetOwner.graphIsBound)
                {
                    UndoUtility.SetDirty(targetOwner);
                }
            }

            //repaint?
            if (willRepaint || e.type == EventType.MouseMove || rootGraph.isRunning)
            {
                Repaint();
            }

            //reset flags
            if (e.type == EventType.Repaint)
            {
                fullDrawPass = false;
                willRepaint = false;
            }

            //hack for quick popups
            if (OnDoPopup != null)
            {
                System.Action temp = OnDoPopup;
                OnDoPopup = null;
                QuickPopup.Show(temp);
            }

            //PostGUI
            GraphEditorUtility.InvokePostGUI();
            //closure
            Styles.Draw(canvasRect, StyleSheet.canvasBorders);
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
        }

        ///----------------------------------------------------------------------------------------------

        //Check that all is ok to continue with GUI
        private bool CheckSumOK()
        {

            if (EditorApplication.isCompiling && !Application.isPlaying)
            {
                ShowNotification(new GUIContent("...Compiling Please Wait..."));
                willRepaint = true;
                return false;
            }

            //get the graph from the GraphOwner if one is set
            if (targetOwner != null)
            {
                rootGraph = targetOwner.graph;
                if (EditorUtility.IsPersistent(targetOwner) && targetOwner.graphIsBound)
                {
                    ShowNotification(new GUIContent("Bound Graphs on prefabs can only be edited by opening the prefab in the prefab editor."));
                    Rect btnRect = new Rect(0, 0, 200, 50);
                    btnRect.center = new Vector2(canvasRect.width / 2, TOP_MARGIN + 70);
                    if (GUI.Button(btnRect, "Open Prefab"))
                    {
                        AssetDatabase.OpenAsset(targetOwner);
                    }
                    return false;
                }
            }

            if (rootGraph == null)
            {
                ShowEmptyGraphGUI();
                return false;
            }

            //set the currently viewing graph by getting the current child graph from the root graph recursively
            Graph curr = GetCurrentGraph(rootGraph);
            if (!ReferenceEquals(curr, currentGraph))
            {
                currentGraph = curr;
                OnCurrentGraphChanged();
            }

            if (currentGraph == null || ReferenceEquals(currentGraph, null))
            {
                return false;
            }

            if (currentGraph.serializationHalted)
            {
                ShowNotification(new GUIContent("Due to last deserialization attempt failure, this graph is protected from changes.\nAny change you make is not saved until the graph has deserialized successfully.\nPlease try restarting Unity to attempt deserialization again.\nIf you think this is a bug, please contact support."));
            }

            return true;
        }


        ///----------------------------------------------------------------------------------------------

        //Recursively get the currenlty showing nested graph starting from the root
        private static Graph GetCurrentGraph(Graph root)
        {
            if (root.GetCurrentChildGraph() == null)
            {
                return root;
            }
            return GetCurrentGraph(root.GetCurrentChildGraph());
        }

        //Starts a zoom area, returns the scaled container rect
        private static Rect StartZoomArea(Rect container, float zoomFactor, out Matrix4x4 oldMatrix)
        {
            GUI.EndClip();
            container.y += TAB_HEIGHT;
            container.width *= 1 / zoomFactor;
            container.height *= 1 / zoomFactor;
            oldMatrix = GUI.matrix;
            Matrix4x4 matrix1 = Matrix4x4.TRS(new Vector2(container.x, container.y), Quaternion.identity, Vector3.one);
            Matrix4x4 matrix2 = Matrix4x4.Scale(new Vector3(zoomFactor, zoomFactor, 1f));
            GUI.matrix = matrix1 * matrix2 * matrix1.inverse * GUI.matrix;
            return container;
        }

        //Ends the zoom area
        private static void EndZoomArea(Matrix4x4 oldMatrix)
        {
            GUI.matrix = oldMatrix;
            Rect recover = new Rect(0, TAB_HEIGHT, screenWidth, screenHeight);
            GUI.BeginClip(recover);
        }

        //This is called while within Begin/End windows
        private static void ShowNodesGUI(Graph graph, Rect drawCanvas, bool fullDrawPass, Vector2 canvasMousePos, float zoomFactor)
        {

            //ensure IDs are updated. Must do on seperate iteration before gui
            //FIXME: while it's not expensive, it's still an extra iteration -> move elsewhere?
            if (Event.current.type == EventType.Layout)
            {
                for (int i = 0; i < graph.allNodes.Count; i++)
                {
                    if (graph.allNodes[i].ID != i)
                    {
                        graph.UpdateNodeIDs(true);
                        break;
                    }
                }
            }

            for (int i = 0; i < graph.allNodes.Count; i++)
            {
                Node.ShowNodeGUI(graph.allNodes[i], drawCanvas, fullDrawPass, canvasMousePos, zoomFactor);
            }

            if (graph.primeNode != null)
            {
                GUI.Box(new Rect(graph.primeNode.rect.x, graph.primeNode.rect.y - 20, graph.primeNode.rect.width, 20), "<b>START</b>", StyleSheet.box);
            }
        }

        ///Translate the graph to focus selection
        public static void FocusSelection()
        {
            if (GraphEditorUtility.activeElements != null && GraphEditorUtility.activeElements.Count > 0)
            {
                FocusPosition(GetNodeBounds(GraphEditorUtility.activeElements.Cast<Node>().ToList()).center);
                return;
            }
            if (GraphEditorUtility.activeElement != null)
            {
                FocusElement(GraphEditorUtility.activeElement);
                return;
            }
            if (currentGraph.allNodes.Count > 0)
            {
                FocusPosition(GetNodeBounds(currentGraph.allNodes).center);
                return;
            }
            FocusPosition(viewCanvasCenter);
        }

        ///Ping element
        public static void PingElement(IGraphElement element)
        {
            if (element is Node) { PingRect((element as Node).rect); }
            if (element is Connection) { PingRect((element as Connection).GetMidRect()); }
        }

        ///Translate the graph to the center of target element (node, connection)
        public static void FocusElement(IGraphElement element, bool alsoSelect = false)
        {
            if (element is Node) { FocusNode((Node)element, alsoSelect); }
            if (element is Connection) { FocusConnection((Connection)element, alsoSelect); }
        }

        ///Translate the graph to the center of the target node
        public static void FocusNode(Node node, bool alsoSelect = false)
        {
            if (currentGraph == node.graph)
            {
                FocusPosition(node.rect.center);
                PingRect(node.rect);
                if (alsoSelect) { GraphEditorUtility.activeElement = node; }
            }
        }

        ///Translate the graph to the center of the target connection
        public static void FocusConnection(Connection connection, bool alsoSelect = false)
        {
            if (currentGraph == connection.sourceNode.graph)
            {
                FocusPosition(connection.GetMidRect().center);
                PingRect(connection.GetMidRect());
                if (alsoSelect) { GraphEditorUtility.activeElement = connection; }
            }
        }

        ///Translate the graph to to center of the target pos
        public static void FocusPosition(Vector2 targetPos, bool smooth = true)
        {
            if (smooth)
            {
                smoothPan = -targetPos;
                smoothPan += new Vector2(viewRect.width / 2, viewRect.height / 2);
                smoothPan *= zoomFactor;
            }
            else
            {
                pan = -targetPos;
                pan += new Vector2(viewRect.width / 2, viewRect.height / 2);
                pan *= zoomFactor;
                smoothPan = null;
                smoothZoomFactor = null;
            }
        }

        ///Ping rect
        public static void PingRect(Rect rect)
        {
            pingValue = 1;
            pingRect = rect;
        }

        ///Refresh full draw pass flag
        public static void FullDrawPass()
        {
            fullDrawPass = true;
        }

        ///Zoom with center position
        private static void ZoomAt(Vector2 center, float delta)
        {
            if (zoomFactor == 1 && delta > 0)
            {
                return;
            }

            Vector2 pinPoint = (center - pan) / zoomFactor;
            float newZ = zoomFactor;
            newZ += delta;
            newZ = Mathf.Clamp(newZ, ZOOM_MIN, ZOOM_MAX);
            smoothZoomFactor = newZ;

            Vector2 a = (pinPoint * newZ) + pan;
            Vector2 b = center;
            Vector2 diff = b - a;
            smoothPan = pan + diff;
        }

        //Handles D&D operations
        private static void AcceptDrops(Graph graph, Vector2 canvasMousePos)
        {
            if (GraphEditorUtility.allowClick)
            {
                if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length == 1)
                {
                    if (e.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                    }
                    if (e.type == EventType.DragPerform)
                    {
                        Object value = DragAndDrop.objectReferences[0];
                        DragAndDrop.AcceptDrag();
                        graph.CallbackOnDropAccepted(value, canvasMousePos);
                    }
                }
            }
        }

        ///Gets the bound rect for the nodes
        private static Rect GetNodeBounds(List<Node> nodes)
        {
            if (nodes == null || nodes.Count == 0)
            {
                return default(Rect);
            }

            Rect[] arr = new Rect[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                arr[i] = nodes[i].rect;
            }
            return RectUtils.GetBoundRect(arr);
        }

        //Do graphical multi selection box for nodes
        private static void DoCanvasRectSelection()
        {

            if (GraphEditorUtility.allowClick && e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.shift && canvasRect.Contains(CanvasToView(e.mousePosition)))
            {
                if (e.clickCount == 1)
                {
                    GraphEditorUtility.activeElement = null;
                    selectionStartPos = e.mousePosition;
                    isMultiSelecting = true;
                    e.Use();
                }
            }

            if (isMultiSelecting && e.rawType == EventType.MouseUp)
            {
                Rect rect = RectUtils.GetBoundRect(selectionStartPos, e.mousePosition);
                List<Node> overlapedNodes = currentGraph.allNodes.Where(n => rect.Overlaps(n.rect) && !n.isHidden).ToList();
                isMultiSelecting = false;
                if (e.control && rect.width > 50 && rect.height > 50)
                {
                    UndoUtility.RecordObjectComplete(currentGraph, "Create Group");
                    if (currentGraph.canvasGroups == null) { currentGraph.canvasGroups = new List<CanvasGroup>(); }
                    currentGraph.canvasGroups.Add(new CanvasGroup(rect, "..."));
                    UndoUtility.SetDirty(currentGraph);
                }
                else
                {
                    if (overlapedNodes.Count > 0)
                    {
                        GraphEditorUtility.activeElements = overlapedNodes.Cast<IGraphElement>().ToList();
                        e.Use();
                    }
                }
            }

            if (isMultiSelecting)
            {
                Rect rect = RectUtils.GetBoundRect(selectionStartPos, e.mousePosition);
                if (rect.width > 5 && rect.height > 5)
                {
                    GUI.color = new Color(0.5f, 0.5f, 1, 0.3f);
                    Styles.Draw(rect, StyleSheet.box);
                    for (int i = 0; i < currentGraph.allNodes.Count; i++)
                    {
                        Node node = currentGraph.allNodes[i];
                        if (rect.Overlaps(node.rect) && !node.isHidden)
                        {
                            Rect highlightRect = node.rect;
                            Styles.Draw(highlightRect, StyleSheet.windowHighlight);
                        }
                    }
                    if (rect.width > 50 && rect.height > 50)
                    {
                        GUI.color = new Color(1, 1, 1, e.control ? 0.6f : 0.15f);
                        GUI.Label(new Rect(e.mousePosition.x + 16, e.mousePosition.y, 120, 22), "<i>+ control for group</i>", StyleSheet.labelOnCanvas);
                    }
                }
            }

            GUI.color = Color.white;
        }



        //Draw a grid
        private static void DrawGrid(Rect container, Vector2 offset, float zoomFactor)
        {
            if (!Prefs.showGrid) { return; }
            if (Event.current.type != EventType.Repaint) { return; }

            Handles.color = Color.black.WithAlpha(0.15f);

            int drawGridSize = zoomFactor > 0.5f ? GRID_SIZE : GRID_SIZE * 5;
            float step = drawGridSize * zoomFactor;

            float xDiff = offset.x % step;
            float xStart = container.xMin + xDiff;
            float xEnd = container.xMax;
            for (float i = xStart; i < xEnd; i += step)
            {
                if (i > container.xMin)
                { //this avoids one step being drawn before x min on negative mod
                    Handles.DrawLine(new Vector3(i, container.yMin, 0), new Vector3(i, container.yMax, 0));
                }
            }

            float yDiff = offset.y % step;
            float yStart = container.yMin + yDiff;
            float yEnd = container.yMax;
            for (float i = yStart; i < yEnd; i += step)
            {
                if (i > container.yMin)
                { //this avoids one step being drawn before y min on negative mod
                    Handles.DrawLine(new Vector3(container.xMin, i, 0), new Vector3(container.xMax, i, 0));
                }
            }

            Handles.color = Color.white;
        }


        //This is the hierarchy shown at top left. Recusrsively show the nested path
        private static void StartBreadCrumbNavigation(Graph root)
        {
            GUILayout.BeginArea(Rect.MinMaxRect(canvasRect.xMin + 15, canvasRect.yMin + 5, canvasRect.xMax, canvasRect.yMax));
            DoBreadCrumbNavigationStep(root);
            GUILayout.EndArea();
        }

        private static void DoBreadCrumbNavigationStep(Graph root)
        {
            if (root == null) { return; }
            //if something selected the inspector panel shows on top of the breadcrub. If external inspector active it doesnt matter, so draw anyway.
            if (GraphEditorUtility.activeElement != null && !Prefs.useExternalInspector) { return; }

            string resultInfo = EditorUtility.IsPersistent(root) ? "Asset Reference" : (Application.isPlaying ? "Instance" : "Bound");
            if (targetOwner != null && EditorUtility.IsPersistent(targetOwner)) { resultInfo += " | Prefab Asset"; }
            string graphInfo = string.Format("<color=#ff4d4d>({0})</color>", resultInfo);

            GUI.color = Color.white.WithAlpha(0.5f);

            GUILayout.BeginVertical();
            Graph childGraph = root.GetCurrentChildGraph();
            if (childGraph == null)
            {

                if (root.agent == null && root.blackboard == null)
                {
                    GUILayout.Label(string.Format("<b><size=22>{0} {1}</size></b>", root.name, graphInfo), StyleSheet.labelOnCanvas);
                }
                else
                {
                    string agentInfo = root.agent != null ? "@" + root.agent.gameObject.name : "No Agent";
                    GUILayout.Label(string.Format("<b><size=22>{0} {1}</size></b>\n<size=10>{2}</size>", root.name, graphInfo, agentInfo), StyleSheet.labelOnCanvas);
                }

            }
            else
            {

                GUILayout.BeginHorizontal();
                GUILayout.Label("⤴ " + root.name, GUI.skin.button);
                Rect lastRect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(lastRect, MouseCursor.Link);
                if (e.type == EventType.MouseUp && lastRect.Contains(Event.current.mousePosition))
                {
                    root.SetCurrentChildGraphAssignable(null);
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                DoBreadCrumbNavigationStep(childGraph);
            }

            GUILayout.EndVertical();
            GUI.color = Color.white;
        }


        ///Canvas groups
        private static void DoCanvasGroups()
        {

            if (currentGraph.canvasGroups == null)
            {
                return;
            }

            for (int i = 0; i < currentGraph.canvasGroups.Count; i++)
            {
                CanvasGroup group = currentGraph.canvasGroups[i];
                Rect headerRect = new Rect(group.rect.x, group.rect.y, group.rect.width, 25);
                Rect autoRect = new Rect(headerRect.xMax - 50, headerRect.y + 2, 50, headerRect.height);
                Rect scaleRectBR = new Rect(group.rect.xMax - 20, group.rect.yMax - 20, 20, 20);

                GUI.color = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.4f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
                Styles.Draw(group.rect, StyleSheet.editorPanel);

                if (group.color != default(Color))
                {
                    GUI.color = group.color;
                    GUI.DrawTexture(group.rect.ExpandBy(2, -27, 2, 0), EditorGUIUtility.whiteTexture);
                }

                GUI.color = Color.white;
                GUI.Box(new Rect(scaleRectBR.x + 10, scaleRectBR.y + 10, 6, 6), string.Empty, StyleSheet.scaleArrowBR);

                if (group.editState != CanvasGroup.EditState.Renaming)
                {
                    float size = StyleSheet.canvasGroupHeader.fontSize / zoomFactor;
                    string name = string.Format("<size={0}><b>{1}</b></size>", size, group.name);
                    GUI.Label(headerRect, name, StyleSheet.canvasGroupHeader);

                    EditorGUIUtility.AddCursorRect(headerRect, group.editState == CanvasGroup.EditState.Renaming ? MouseCursor.Text : MouseCursor.Link);
                    EditorGUIUtility.AddCursorRect(scaleRectBR, MouseCursor.ResizeUpLeft);

                    GUI.color = GUI.color.WithAlpha(0.25f);
                    bool newAutoValue = GUI.Toggle(autoRect, group.autoGroup, "Auto");
                    if (newAutoValue != group.autoGroup)
                    {
                        UndoUtility.RecordObject(currentGraph, "AutoGroup");
                        group.autoGroup = newAutoValue;
                        group.GatherAjustAndFlushContainedNodes(currentGraph);
                        UndoUtility.SetDirty(currentGraph);
                    }
                    GUI.color = Color.white;
                }

                if (group.editState == CanvasGroup.EditState.Renaming)
                {
                    GUI.SetNextControlName("GroupRename" + i);
                    group.name = EditorGUI.TextField(headerRect, group.name, StyleSheet.canvasGroupHeader);
                    GUI.FocusControl("GroupRename" + i);
                    if (e.keyCode == KeyCode.Return || (e.type == EventType.MouseDown && !headerRect.Contains(e.mousePosition)))
                    {
                        group.editState = CanvasGroup.EditState.None;
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                    }
                }

                if (e.type == EventType.MouseDown && GraphEditorUtility.allowClick)
                {

                    if (headerRect.Contains(e.mousePosition))
                    {

                        UndoUtility.RecordObjectComplete(currentGraph, "Move Canvas Group");

                        //calc group nodes
                        tempCanvasGroupNodes = currentGraph.allNodes.Where(n => group.rect.Encapsulates(n.rect)).ToArray();
                        tempCanvasGroupGroups = currentGraph.canvasGroups.Where(c => group.rect.Encapsulates(c.rect)).ToArray();

                        if (e.button == 1)
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Rename"), false, () => { group.editState = CanvasGroup.EditState.Renaming; });
                            menu.AddItem(new GUIContent("Edit Color"), false, () => { DoPopup(() => { group.color = EditorGUILayout.ColorField(group.color); }); });
                            menu.AddItem(new GUIContent("Select Nodes"), false, () => { GraphEditorUtility.activeElements = tempCanvasGroupNodes.Cast<IGraphElement>().ToList(); });
                            menu.AddItem(new GUIContent("Delete Group"), false, () => { currentGraph.canvasGroups.Remove(group); });
                            GraphEditorUtility.PostGUI += () => { menu.ShowAsContext(); };
                        }
                        else if (e.button == 0)
                        {
                            group.editState = CanvasGroup.EditState.Dragging;
                            if (e.clickCount == 2)
                            {
                                group.editState = CanvasGroup.EditState.Renaming;
                                GUI.FocusControl("GroupRename" + i);
                            }
                        }

                        UndoUtility.SetDirty(currentGraph);
                        e.Use();
                    }

                    if (e.button == 0 && scaleRectBR.Contains(e.mousePosition))
                    {
                        UndoUtility.RecordObjectComplete(currentGraph, "Scale Canvas Group");
                        group.editState = CanvasGroup.EditState.Scaling;
                        UndoUtility.SetDirty(currentGraph);
                        e.Use();
                    }
                }

                if (e.type == EventType.MouseDrag)
                {

                    if (group.editState == CanvasGroup.EditState.Dragging)
                    {

                        group.rect.position += e.delta;

                        if (!e.shift)
                        {
                            for (int j = 0; j < tempCanvasGroupNodes.Length; j++)
                            {
                                tempCanvasGroupNodes[j].position += e.delta;
                            }

                            for (int j = 0; j < tempCanvasGroupGroups.Length; j++)
                            {
                                tempCanvasGroupGroups[j].rect.position += e.delta;
                            }
                        }
                    }

                    if (group.editState == CanvasGroup.EditState.Scaling)
                    {
                        group.rect.xMax = Mathf.Max(e.mousePosition.x + 5, group.rect.xMin + 100);
                        group.rect.yMax = Mathf.Max(e.mousePosition.y + 5, group.rect.yMin + 100);
                    }
                }

                if (e.rawType == EventType.MouseUp && group.editState != CanvasGroup.EditState.Renaming)
                {
                    if (group.editState == CanvasGroup.EditState.Dragging)
                    {
                        foreach (Node node in group.GatherContainedNodes(currentGraph))
                        {
                            node.TrySortConnectionsByPositionX();
                        }
                        group.FlushContainedNodes();
                    }
                    //sort groups so that smaller ones show on top of bigger ones
                    if (group.editState != CanvasGroup.EditState.None)
                    {
                        currentGraph.canvasGroups = currentGraph.canvasGroups.OrderBy(g => -(g.rect.width * g.rect.height)).ToList();
                    }
                    group.editState = CanvasGroup.EditState.None;
                    tempCanvasGroupGroups = null;
                    tempCanvasGroupNodes = null;
                }
            }
        }


        //Snap all nodes either to grid if option enabled
        private static void SnapNodesToGrid(Graph graph)
        {
            if (Prefs.snapToGrid)
            {
                for (int i = 0; i < graph.allNodes.Count; i++)
                {
                    Node node = graph.allNodes[i];
                    Vector2 pos = node.position;
                    pos.x = Mathf.Round(pos.x / GRID_SIZE) * GRID_SIZE;
                    pos.y = Mathf.Round(pos.y / GRID_SIZE) * GRID_SIZE;
                    node.position = pos;
                }
            }
        }

        //before nodes for handling events
        private static void HandleMinimapEvents(Rect container)
        {

            if (!GraphEditorUtility.allowClick) { return; }

            Rect resizeRect = new Rect(container.x, container.y, 6, 6);
            EditorGUIUtility.AddCursorRect(resizeRect, MouseCursor.ResizeUpLeft);
            if (e.type == EventType.MouseDown && e.button == 0 && resizeRect.Contains(e.mousePosition))
            {
                isResizingMinimap = true;
                e.Use();
            }
            if (e.rawType == EventType.MouseUp)
            {
                isResizingMinimap = false;
            }
            if (isResizingMinimap && e.type == EventType.MouseDrag)
            {
                Prefs.minimapSize -= e.delta;
                e.Use();
            }

            if (Prefs.minimapSize != Prefs.MINIMAP_MIN_SIZE)
            {
                EditorGUIUtility.AddCursorRect(container, MouseCursor.MoveArrow);
                if (e.type == EventType.MouseDown && e.button == 0 && container.Contains(e.mousePosition))
                {
                    Rect finalBound = ResolveMinimapBoundRect(currentGraph, viewRect);
                    Vector2 norm = Rect.PointToNormalized(container, e.mousePosition);
                    Vector2 pos = Rect.NormalizedToPoint(finalBound, norm);
                    FocusPosition(pos);
                    isDraggingMinimap = true;
                    e.Use();
                }
                if (e.rawType == EventType.MouseUp)
                {
                    isDraggingMinimap = false;
                }
                if (isDraggingMinimap && e.type == EventType.MouseDrag)
                {
                    Rect finalBound = ResolveMinimapBoundRect(currentGraph, viewRect);
                    Vector2 norm = Rect.PointToNormalized(container, e.mousePosition);
                    Vector2 pos = Rect.NormalizedToPoint(finalBound, norm);
                    FocusPosition(pos);
                    e.Use();
                }
            }
        }

        ///after nodes, a cool minimap
        private static void DrawMinimap(Rect container)
        {

            GUI.color = Colors.Grey(0.5f).WithAlpha(0.85f);
            Styles.Draw(container, StyleSheet.windowShadow);
            GUI.Box(container, currentGraph.allNodes.Count > 0 ? string.Empty : "Minimap", StyleSheet.box);

            if (Prefs.minimapSize != Prefs.MINIMAP_MIN_SIZE)
            {

                Rect finalBound = ResolveMinimapBoundRect(currentGraph, viewRect);
                Rect lensRect = viewRect.TransformSpace(finalBound, container);
                GUI.color = new Color(1, 1, 1, 0.8f);
                Styles.Draw(lensRect, StyleSheet.box);
                GUI.color = Color.white;
                finalBound = finalBound.ExpandBy(25);

                //repaint only
                if (Event.current.type == EventType.Repaint)
                {

                    //groups
                    if (currentGraph.canvasGroups != null)
                    {
                        for (int i = 0; i < currentGraph.canvasGroups.Count; i++)
                        {
                            CanvasGroup group = currentGraph.canvasGroups[i];
                            Rect blipRect = group.rect.TransformSpace(finalBound, container);
                            Rect blipHeaderRect = Rect.MinMaxRect(blipRect.xMin, blipRect.yMin, blipRect.xMax, blipRect.yMin + 2);
                            Color color = group.color != default(Color) ? group.color : Color.gray;
                            color.a = 0.5f;
                            GUI.color = color;
                            GUI.DrawTexture(blipRect, Texture2D.whiteTexture);
                            GUI.DrawTexture(blipHeaderRect, Texture2D.whiteTexture);
                            GUI.color = Color.white;
                        }
                    }

                    //ping
                    if (pingValue >= 0)
                    {
                        GUI.color = Color.white.WithAlpha(pingValue);
                        Rect pingBlipRect = pingRect.TransformSpace(finalBound, container);
                        GUI.DrawTexture(pingBlipRect.ExpandBy(2), Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }


                    if (currentGraph.allNodes != null)
                    {

                        //connections
                        for (int i = 0; i < currentGraph.allNodes.Count; i++)
                        {
                            for (int j = 0; j < currentGraph.allNodes[i].outConnections.Count; j++)
                            {
                                Connection connection = currentGraph.allNodes[i].outConnections[j];
                                if (connection.targetNode.isHidden) { continue; }
                                Vector2 snp = connection.sourceNode.rect.center.TransformSpace(finalBound, container);
                                Vector2 tnp = connection.targetNode.rect.center.TransformSpace(finalBound, container);
                                Vector2 sp = connection.startRect.center.TransformSpace(finalBound, container);
                                Vector2 tp = connection.endRect.center.TransformSpace(finalBound, container);
                                Handles.color = Application.isPlaying ? StyleSheet.GetStatusColor(connection.status) : Colors.Grey(0.35f);
                                Handles.DrawAAPolyLine(snp, sp, tp, tnp);
                                Handles.color = Color.white;
                            }
                        }

                        //nodes
                        for (int i = 0; i < currentGraph.allNodes.Count; i++)
                        {
                            Node node = currentGraph.allNodes[i];
                            if (node.isHidden) { continue; }
                            Rect blipRect = node.rect.TransformSpace(finalBound, container);

                            if (Application.isPlaying && node.status != Status.Resting)
                            {
                                GUI.color = StyleSheet.GetStatusColor(node.status);
                                GUI.DrawTexture(blipRect.ExpandBy(2), Texture2D.whiteTexture);
                            }

                            if (GraphEditorUtility.activeElement == node || GraphEditorUtility.activeElements.Contains(node))
                            {
                                GUI.color = Color.white;
                                GUI.DrawTexture(blipRect.ExpandBy(2), Texture2D.whiteTexture);
                            }

                            GUI.color = node.nodeColor != default(Color) ? node.nodeColor : Color.grey;
                            GUI.DrawTexture(blipRect, Texture2D.whiteTexture);
                            GUI.color = Color.white;
                        }
                    }
                }
            }

            Rect resizeRect = new Rect(container.x, container.y, 6, 6);
            GUI.Box(resizeRect, string.Empty, StyleSheet.scaleArrowTL);
            GUI.color = Color.white;
        }

        //resolves the bounds used in the minimap
        private static Rect ResolveMinimapBoundRect(Graph graph, Rect container)
        {

            Rect[] arr1 = new Rect[graph.allNodes.Count];
            for (int i = 0; i < graph.allNodes.Count; i++)
            {
                arr1[i] = graph.allNodes[i].rect;
            }

            Rect nBounds = RectUtils.GetBoundRect(arr1);
            Rect finalBound = nBounds;

            if (graph.canvasGroups != null && graph.canvasGroups.Count > 0)
            {
                Rect[] arr2 = new Rect[graph.canvasGroups.Count];
                for (int i = 0; i < graph.canvasGroups.Count; i++)
                {
                    arr2[i] = graph.canvasGroups[i].rect;
                }
                Rect gBounds = RectUtils.GetBoundRect(arr2);
                finalBound = RectUtils.GetBoundRect(nBounds, gBounds);
            }

            finalBound = RectUtils.GetBoundRect(finalBound, container);
            return finalBound;
        }

        ///
        private void DrawPings()
        {
            if (pingValue > 0)
            {
                GUI.color = Color.white.WithAlpha(pingValue);
                Styles.Draw(pingRect, Styles.highlightBox);
                GUI.color = Color.white;
            }
        }

        //Playmode gui
        private void ShowPlaymodeGUI()
        {
            if (!Application.isPlaying || targetOwner == null) { return; }

            int bWidth = 96;
            int bHeight = 22;

#if !UNITY_2019_3_OR_NEWER
            bWidth = 98;
            bHeight = 25;
#endif

            Rect buttonsRect = new Rect(0, 0, bWidth, 0);
            buttonsRect.center = new Vector2(canvasRect.width / 2, 0);
            buttonsRect.yMax = canvasRect.yMax - 5;
            buttonsRect.yMin = canvasRect.yMax - 5 - bHeight;

            //prevent click through to nodes
            GraphEditorUtility.allowClick &= !buttonsRect.Contains(Event.current.mousePosition);

            GUI.color = Colors.Grey(0.2f);
            GUI.DrawTexture(buttonsRect.ExpandBy(5), Texture2D.whiteTexture);
            Rect labelRect = Rect.MinMaxRect(buttonsRect.xMin - 20, buttonsRect.yMin - 40, buttonsRect.xMax + 20, buttonsRect.yMin - 6);
            string runLabelRoot = string.Format("<color=#FFEB04><b>Runtime: {0}</b></color>", targetOwner.elapsedTime.ToString("0.00"));
            string runLabelCurrent = string.Format("<color=#FFEB04><b>({0})</b></color>", currentGraph.elapsedTime.ToString("0.00"));
            string runLabelFinal = rootGraph != currentGraph ? runLabelCurrent + '\n' + runLabelRoot : runLabelRoot;
            GUI.Label(labelRect, runLabelFinal, Styles.bottomCenterLabel);

            GUI.color = Color.white;

            float buttonWidth = buttonsRect.width / 3f;
            Rect playRect = Rect.MinMaxRect(buttonsRect.xMin, buttonsRect.yMin, buttonsRect.xMin + buttonWidth, buttonsRect.yMax);
            Rect pauseRect = Rect.MinMaxRect(playRect.xMax, buttonsRect.yMin, playRect.xMax + buttonWidth, buttonsRect.yMax);
            Rect stepRect = Rect.MinMaxRect(pauseRect.xMax, buttonsRect.yMin, pauseRect.xMax + buttonWidth, buttonsRect.yMax);

            GUI.contentColor = EditorGUIUtility.isProSkin ? Color.white : Colors.Grey(0.3f);
            GUI.color = Colors.Grey(targetOwner.isRunning ? 1f : 0.7f);
            if (GUI.Button(playRect, Icons.playIcon, Styles.buttonLeft))
            {
                if (targetOwner.isRunning) { targetOwner.StopBehaviour(); } else { targetOwner.StartBehaviour(); }
                Event.current.Use();
            }

            GUI.color = Colors.Grey(targetOwner.isPaused ? 1f : 0.7f);
            if (GUI.Button(pauseRect, Icons.pauseIcon, Styles.buttonMid))
            {
                if (targetOwner.isPaused) { targetOwner.StartBehaviour(); } else { targetOwner.PauseBehaviour(); }
                Event.current.Use();
            }

            GUI.color = Colors.Grey(0.7f);
            if (GUI.Button(stepRect, Icons.stepIcon, Styles.buttonRight))
            {
                targetOwner.PauseBehaviour(); targetOwner.UpdateBehaviour();
                Event.current.Use();
            }
            GUI.contentColor = Color.white;
            GUI.color = Color.white;
        }

        //an idea but it's taking up space i dont like
        private void ShowConsoleLog()
        {
            Rect rect = Rect.MinMaxRect(canvasRect.xMin + 2, canvasRect.yMax + 5, canvasRect.xMax, canvasRect.yMax + 20);
            Logger.Message msg = GraphConsole.GetFirstMessageForGraph(currentGraph);
            if (msg.IsValid())
            {
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
                if (GUI.Button(rect, GraphConsole.GetFormatedGUIContentForMessage(msg), StyleSheet.labelOnCanvas))
                {
                    GraphConsole.ShowWindow();
                }
            }
        }

        //this is shown when root graph is null
        //TODO: Add something like a menu to create graphs from here?
        private void ShowEmptyGraphGUI()
        {
            if (targetOwner != null)
            {
                string text = string.Format("The selected {0} does not have a {1} assigned.\n Please create or assign a new one in it's inspector.", targetOwner.GetType().Name, targetOwner.graphType.Name);
                ShowNotification(new GUIContent(text));
                return;
            }
            ShowNotification(new GUIContent("Please select a GraphOwner GameObject or a Graph Asset."));
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2 && Event.current.button == 0)
            {
                current.maximized = !current.maximized;
                Event.current.Use();
            }
        }
    }
}

#endif