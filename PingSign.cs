using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class PingSign : MonoBehaviour
{
    public enum Sign { Help, Missing, Danger, Going, Exit };
    [SerializeField]
    public Sign sign = Sign.Exit;

    private Camera TargetCamera;

    private bool MakeOnce = false;
    [SerializeField]
    private bool isThisThingOn = false;

    private Vector3 endline = Vector3.zero;
    public float Timer = 3.0f;

    public Vector3 InitialCoordinate;
    private LineRenderer LineR;
    private Vector3 startPos = Vector3.zero;
    private Vector3 endPos;
    private Ray MousePosToRay;
    private float PlaneDistance;
    private Vector3 InitialCameraPos;
    private Plane plane;
    private Vector3 Mouse;

    public PingPooling pingPool;
    int layerMask;

    Vector3 GetMousePosition()
    {
        CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
        Canvas canvas = GetComponentInParent<Canvas>();

        var pos = new Vector2(Input.mousePosition.x * scaler.referenceResolution.x / Screen.width, Input.mousePosition.y * scaler.referenceResolution.y / Screen.height);
        pos.x -= scaler.referenceResolution.x * 0.5f;
        pos.y -= scaler.referenceResolution.y * 0.5f;
        return pos;
    }
    private void Awake()
    {
        PlaneDistance = transform.parent.GetComponent<Canvas>().planeDistance;
        TargetCamera = Camera.main;
        LineR = transform.parent.GetComponentInChildren<LineRenderer>();
        LineR.enabled = false;

        layerMask = (-1) - ((1 << LayerMask.NameToLayer("WallCollider")));

        if (pingPool.Equals(null))
            GameObject.FindGameObjectWithTag("PingPool").GetComponent<PingPooling>();

        this.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Mouse = Input.mousePosition;
        var screenPoint = new Vector3(Mouse.x, Mouse.y, PlaneDistance); // z값을 캔버스 Plane Distance 값 줌
        transform.position = Camera.main.ScreenToWorldPoint(screenPoint);
        startPos = transform.position; // UI 위치 조정

        // Line렌더 시작점 조정
        LineR.positionCount = 2;
        LineR.SetPosition(0, GetMousePosition());

        InitialCameraPos = Camera.main.transform.position;
        plane = new Plane(-transform.forward, transform.position);

    }

    private void Update()
    {
        isThisThingOn = this.gameObject.GetActive();

        if (PhotonNetwork.player.IsLocal)
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
            {
                LineR.enabled = true;
                isThisThingOn = true;
                //라인렌더 종료지점 설정
                MousePosToRay = TargetCamera.ScreenPointToRay(Input.mousePosition);
                float distanceForEndPos = 0;
                plane.Raycast(MousePosToRay, out distanceForEndPos);
                endPos = MousePosToRay.origin + MousePosToRay.direction * distanceForEndPos;
                endline = endPos;
                LineR.SetPosition(1, GetMousePosition());
                sign = GetMousePos(startPos, endPos);
            }


            if (LineR.enabled && Input.GetMouseButtonUp(0))
            {
                if (!pingPool.CanMakePing) //핑 횟수제한을 넘었다면
                {
                    isThisThingOn = false;
                    //사용할수없습니다 메세지 출력
                    gameObject.SetActive(false);
                    return;
                }
                else if ((sign.Equals(Sign.Exit))) // Exit 를 선택 했다면
                {
                    isThisThingOn = false;
                    gameObject.SetActive(false);
                    return;
                }

                Mouse.z = Camera.main.farClipPlane; // Enable 에서 받아온 마우스 초기값
                Ray ray = Camera.main.ScreenPointToRay(Mouse); // 마우스 좌표를 기준으로  스크린을투과하는 레이
                RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, layerMask);
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject.layer.Equals(LayerMask.NameToLayer("GroundLayer")))
                    {
                        if (!MakeOnce)
                        {
                            MakeOnce = true;
                            // 타겟을 레이캐스트가 충돌된 곳으로 옮긴다.
                            InitialCoordinate = hit.point;
                            MakePingSign();
                        }
                    }
                }

                isThisThingOn = false;
                LineR.enabled = false;
                LineR.positionCount = 0;
                this.gameObject.SetActive(false);
            }
        }
        if (isThisThingOn == false && this.gameObject.GetActive())
        {
            this.gameObject.SetActive(false);
        }
    }

    private Sign GetMousePos(Vector3 StartPos, Vector3 endPos)
    {
        float result = Vector3.SignedAngle(transform.up, StartPos - endPos, -transform.forward) + 180.0f;

        if (Vector2.Distance(StartPos, endPos) <= 0.13f)// 중앙  
            return Sign.Exit;
        else if (result > 45 && result < 135) // 우
            return Sign.Going;
        else if (result > 135 && result < 225) // 하 
            return Sign.Help;
        else if (result > 225 && result < 315) // 좌
            return Sign.Missing;
        else // 상
            return Sign.Danger;
    }

    private void MakePingSign()
    {
        if (pingPool.MakeCount >= pingPool.MakeMaxCount)
        {
            //print("생성할수 없다.");
        }
        else
        {
            switch (sign)
            {
                case Sign.Help:
                    pingPool.GetFxPool("Help", InitialCoordinate, false);
                    break;
                case Sign.Missing:
                    pingPool.GetFxPool("Missing", InitialCoordinate, false);
                    break;
                case Sign.Danger:
                    pingPool.GetFxPool("Danger", InitialCoordinate, false);
                    break;
                case Sign.Going:
                    pingPool.GetFxPool("Going", InitialCoordinate, false);
                    break;
                case Sign.Exit:
                    break;
            }
            pingPool.MakeCount++;
        }
        MakeOnce = false;
    }
}