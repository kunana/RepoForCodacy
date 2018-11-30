using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// UI 활성화는 MinimapClick.cs 에서.
public class PingSignSmall : MonoBehaviour
{
    public enum Sign { Help, Missing, Danger, Going, Exit };
    [SerializeField]
    public Sign sign = Sign.Exit;

    private Vector2 UIposition;
    public Vector2 StartPos;
    public Vector2 Endpos;
    public LineRenderer Line;

    private bool MakeOnce = false;

    public Vector3 InitialCoordinate;
    public PingPooling pingPool;


    private void Awake()
    {
        if (pingPool.Equals(null))
        {
            GameObject.FindGameObjectWithTag("PingPool").GetComponent<PingPooling>();
        }
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        UIposition = Input.mousePosition;
        transform.position = UIposition;
        Line.positionCount = 2;
    }

    private void Update()
    {
        if (PhotonNetwork.player.IsLocal)
        {
            if (Input.GetKey(KeyCode.LeftAlt) && Input.GetMouseButton(0))
            {
                Line.enabled = true;
                sign = GetMousePosS(StartPos, Endpos);
            }

            if (Line.enabled && Input.GetMouseButtonUp(0))
            {
                if ((sign.Equals(Sign.Exit))) // Exit 를 선택 했다면
                {
                    gameObject.SetActive(false);
                    return;
                }

                if (!pingPool.CanMakePing) //핑 횟수제한을 넘었다면
                {
                    //사용할수없습니다 메세지 출력
                    gameObject.SetActive(false);
                    return;
                }

                if (!MakeOnce)
                {
                    MakeOnce = true;
                    MakePingSign();
                }
                this.gameObject.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        Line.enabled = false;
        Line.positionCount = 0;
    }

    public void setLine(string line, Vector3 pos)
    {
        if (line.StartsWith("Start"))
        {
            Line.SetPosition(0, pos);
        }
        else if (line.StartsWith("End"))
        {
            Line.SetPosition(1, pos);
        }
    }

    private Sign GetMousePosS(Vector2 StartPos, Vector2 endPos)
    {
        Vector2 s = StartPos; //시작
        Vector2 e = endPos; // 끝
        Vector2 dir = e - s; // 방향
        //45 135 225 315
        float result = Vector2.SignedAngle(Vector2.up, dir);
        if (result < 0)
            result = result + 360;

        if (Vector2.Distance(StartPos, endPos) <= 6)// 중앙  
            return Sign.Exit;
        else if (result > 45 && result < 135) // 하 
            return Sign.Missing;
        else if (result > 135 && result < 225) // 좌
            return Sign.Help;
        else if (result > 225 && result < 315) // 우
            return Sign.Danger;
        else // 상
            return Sign.Going;
    }

    private void MakePingSign()
    {
        if (pingPool.MakeCount >= pingPool.MakeMaxCount)
        {
            //print("사용할수 없습니다");
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