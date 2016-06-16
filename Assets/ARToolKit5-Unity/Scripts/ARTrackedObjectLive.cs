
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(Transform))]
[ExecuteInEditMode]
public class ARTrackedObjectLive : MonoBehaviour
{
	private const string LogTag = "ARTrackedObjectLive: ";
    public bool remainVisible = true;               // Remain visible when Marker disappeared
    public GameObject eventReceiver;

    [SerializeField]
    private string _markerTag = "";                 // Unique tag for the marker to get tracking from
    
    private AROrigin _origin = null;
	private ARMarker _marker = null;

	private bool visible = false;                   // Current visibility from tracking
    private Vector3 vec3LocalDirect;
    private Vector3 vec3WorldDirect;
    private Transform camTrans;
    private Camera cam;
    private Vector3 vec3LocalPoint;
    private Vector3 vec3WorldPoint;
    private bool bRotate = false;
    private Vector2 oldPosition1 = Vector2.zero;
    private Vector2 oldPosition2 = Vector2.zero;

    public string MarkerTag
	{
		get
		{
			return _markerTag;
		}
		
		set
		{
			_markerTag = value;
			_marker = null;
		}
	}

	// Return the marker associated with this component.
	// Uses cached value if available, otherwise performs a find operation.
	public virtual ARMarker GetMarker()
	{
		if (_marker == null) {
			// Locate the marker identified by the tag
			ARMarker[] ms = FindObjectsOfType<ARMarker>();
			foreach (ARMarker m in ms) {
				if (m.Tag == _markerTag) {
					_marker = m;
					break;
				}
			}
		}
		return _marker;
	}

	// Return the origin associated with this component.
	// Uses cached value if available, otherwise performs a find operation.
	public virtual AROrigin GetOrigin()
	{
		if (_origin == null) {
			// Locate the origin in parent.
			_origin = this.gameObject.GetComponentInParent<AROrigin>(); // Unity v4.5 and later.
		}
		return _origin;
	}

	void Start()
	{
		if (Application.isPlaying) {
			// In Player, set initial visibility to not visible.
			for (int i = 0; i < this.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(false);
		} else {
			// In Editor, set initial visibility to visible.
			for (int i = 0; i < this.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(true);
		}
        camTrans = GameObject.Find("Camera").transform;
        cam = camTrans.GetComponent<Camera>();
    }

	// Use LateUpdate to be sure the ARMarker has updated before we try and use the transformation.
	void LateUpdate()
	{
        // Local scale is always 1 for now
        //transform.localScale = Vector3.one;

        if (!Application.isPlaying)
            return;
        
		// Sanity check, make sure we have an AROrigin in parent hierachy.
		AROrigin origin = GetOrigin();
        if (origin == null) {  return; }

        // Sanity check, make sure we have an ARMarker assigned.
        ARMarker marker = GetMarker();
        if (marker == null) { return; }

        ARMarker baseMarker = origin.GetBaseMarker();
		if (baseMarker != null && marker.Visible) // Marker appears
        {
            if (!visible)
            {	
				visible = true;

				if (eventReceiver != null) eventReceiver.BroadcastMessage("OnMarkerFound", marker, SendMessageOptions.DontRequireReceiver);

				for (int i = 0; i < this.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(true);
			}

            Matrix4x4 pose;
            if (marker == baseMarker)
            {   // If this marker is the base, no need to take base inverse etc.
                pose = origin.transform.localToWorldMatrix;
            }
            else
            {
				pose = (origin.transform.localToWorldMatrix * baseMarker.TransformationMatrix.inverse * marker.TransformationMatrix);
			}
			transform.position = ARUtilityFunctions.PositionFromMatrix(pose);
			transform.rotation = ARUtilityFunctions.QuaternionFromMatrix(pose);
            
            if (eventReceiver != null) eventReceiver.BroadcastMessage("OnMarkerTracked", marker, SendMessageOptions.DontRequireReceiver);

            if (Application.isMobilePlatform)
            {
                ProcessMobileInput();
            }
            else
            {
                ProcessPcInput();
            }
        }
        else // Marker disappear
        {
            if (visible)
            {
                visible = false;

                if(eventReceiver != null) eventReceiver.BroadcastMessage("OnMarkerLost", marker, SendMessageOptions.DontRequireReceiver);
            }

            if (!remainVisible)
            {
                for (int i = 0; i < this.transform.childCount; i++) this.transform.GetChild(i).gameObject.SetActive(false);
            }
            else
            {
                if (Application.isMobilePlatform)
                {
                    ProcessMobileInput();
                }
                else
                {
                    ProcessPcInput();
                }
                
            }                       
        }
	}

    bool isEnlarge(Vector2 oP1, Vector2 oP2, Vector2 nP1, Vector2 nP2)
    {
        var leng1 = Mathf.Sqrt((oP1.x - oP2.x) * (oP1.x - oP2.x) + (oP1.y - oP2.y) * (oP1.y - oP2.y));
        var leng2 = Mathf.Sqrt((nP1.x - nP2.x) * (nP1.x - nP2.x) + (nP1.y - nP2.y) * (nP1.y - nP2.y));
        if( leng1 < leng2 )
        {
             return true;
        }
        else
        {
            return false;
        }
    }

    void ProcessMobileInput()
    {       
        // Multi touch， scale
        if (Input.touchCount > 1)
        {
            // move of two fingers
            if (Input.GetTouch(0).phase == TouchPhase.Moved && Input.GetTouch(1).phase == TouchPhase.Moved)
    	    {
                var tempPosition1 = Input.GetTouch(0).position;
                var tempPosition2 = Input.GetTouch(1).position;
                
                if (isEnlarge(oldPosition1, oldPosition2, tempPosition1, tempPosition2))
                {
                    for (int i = 0; i < this.transform.childCount; i++)
                        transform.GetChild(i).localScale += new Vector3(0.01F, 0.01F, 0.01F);
                }
                else
                {
                    for (int i = 0; i < this.transform.childCount; i++)
                        transform.GetChild(i).localScale -= new Vector3(0.01F, 0.01F, 0.01F);
                }
                
                oldPosition1 = tempPosition1;
                oldPosition2 = tempPosition2;
            }
        }
        else if (Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                bRotate = true;
                // Get movement of the finger since last frame
                Vector2 touchDeltaPosition = Input.GetTouch(0).deltaPosition;

                // Move object across XY plane
                transform.Rotate(touchDeltaPosition.y, touchDeltaPosition.x, 0);
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Ended)
            {
                if (!bRotate)
                {
                    // get Z from Transform
                    Vector3 vec3LocalPoint = camTrans.InverseTransformPoint(transform.position);
                    Vector3 vecScreen = new Vector3(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y);
                    vecScreen[2] = vec3LocalPoint[2];
                    transform.position = cam.ScreenToWorldPoint(vecScreen);
                }
                bRotate = false;
            }

        }
    }

    void ProcessPcInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // get Z from camera
            Vector3 vecScreen = cam.WorldToScreenPoint(transform.position);
            vecScreen.x = Input.mousePosition.x;
            vecScreen.y = Input.mousePosition.y;
            transform.position = cam.ScreenToWorldPoint(vecScreen);

            Debug.Log("Dest(Screen):" + vecScreen.ToString() + " marker pos new(World):" + transform.position.ToString());
        }

        if (Input.GetMouseButtonDown(1)) // right click
        {
            bRotate = !bRotate;
        }
        if (bRotate)
        {
            float h = Input.GetAxis("Mouse X");
            float v = Input.GetAxis("Mouse Y");
            transform.Rotate(v, 0, h);
        }
        
        if (Input.GetKeyDown("left"))
        {   // Method 1，refer to camera
            transform.Translate(Vector3.left * Time.deltaTime, camTrans);
            Debug.Log("translate left refer to Camera coordinate ");
        }
        if (Input.GetKeyDown("right"))
        {   // Method 2，refer to world coordinate
            vec3WorldDirect = camTrans.TransformDirection(Vector3.right);
            vec3LocalDirect = transform.InverseTransformDirection(vec3WorldDirect);
            transform.Translate(vec3LocalDirect * Time.deltaTime);
            Debug.Log("Camera right(world):" + vec3WorldDirect.ToString() + "marker right(local):" + vec3LocalDirect.ToString());
        }
        if (Input.GetKeyDown("down"))
        {
            transform.Translate(Vector3.down * Time.deltaTime, camTrans);
        }
        if (Input.GetKeyDown("up"))
        {
            transform.Translate(Vector3.up * Time.deltaTime, camTrans);
        }
        if (Input.GetKeyDown("["))
        {
            transform.Translate(Vector3.forward * Time.deltaTime, camTrans);
        }
        if (Input.GetKeyDown("]"))
        {
            transform.Translate(Vector3.back * Time.deltaTime, camTrans);
        }

        if (Input.GetKeyDown("=")) //scale up
        {
            for (int i = 0; i < this.transform.childCount; i++)
                transform.GetChild(i).localScale += new Vector3(0.01F, 0.01F, 0.01F);
        }
        if (Input.GetKeyDown("-")) //scale down
        {
            for (int i = 0; i < this.transform.childCount; i++)
                transform.GetChild(i).localScale -= new Vector3(0.01F, 0.01F, 0.01F);
        }
    }
}