using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameManager : Photon.PunBehaviour
{
    public int LoadedPlayer = 0;
    public List<int> RedIncomingEvents = new List<int>();
    public List<int> BlueIncomingEvents = new List<int>();

    public List<GameObject> redTeamPlayer = new List<GameObject>();
    public List<GameObject> blueTeamPlayer = new List<GameObject>();

    private List<Vector3> redPos = new List<Vector3>();
    private List<Vector3> bluePos = new List<Vector3>();

    public List<GameObject> wellTowers = new List<GameObject>();

    public int minionDeadCount = 0;
    public Vector3 RedPos
    {
        get
        {
            return redPos[0];
        }
    }

    public Vector3 BluePos
    {
        get
        {
            return bluePos[0];
        }
    }

    // 팀킬은 tab갱신할때 거기서 갱신. 타워와 드래곤은 죽을때 hitme에서 불러주기
    public int blueTeamTowerKill = 0;
    public int redTeamTowerKill = 0;
    public int blueTeamDragonKill = 0;
    public int redTeamDragonKill = 0;

    // 초기 동기화용 변수
    public bool isloaded = false; // 로딩 완료?
    public bool runOnce = false;
    private bool TimerOnce = false;
    public bool isGameEnd = false; // 게임 종료?

    public float WaitingT = 10f;

    public SystemMessage sysmsg;
    private GameObject StartingWall;
    private GameObject Logo;
    private InGameTimer IngameTimer;
    private RTS_Cam.RTS_Camera mainCam;
    public GameObject NexusRed;
    public GameObject NexusBlue;
    public string team;
    public AudioSource BGM;
    //4 6 8 10 12 / 5 10 
    //262 4 6 8 70 /5 270
    private void Awake()
    {
        StartingWall = transform.GetChild(0).gameObject;
        Logo = transform.GetChild(1).gameObject;
        IngameTimer = GetComponent<InGameTimer>();
        mainCam = Camera.main.GetComponent<RTS_Cam.RTS_Camera>();

        if (!sysmsg)
            sysmsg = GameObject.FindGameObjectWithTag("SystemMsg").GetComponent<SystemMessage>();

        //이벤트 수신
        PhotonNetwork.OnEventCall += SceneLoaded_Received;

        //포지션 세팅
        for (int i = 0; i < 5; i++)
        {
            redPos.Add(new Vector3(4 + (i * 2), 0.5f, 10f));
        }
        for (int i = 0; i < 5; i++)
        {
            bluePos.Add(new Vector3(262 + (i * 2), 0.5f, 270f));
        }
    }

    private void Start()
    {
        team = PhotonNetwork.player.GetTeam().ToString().ToLower();
        if (!BGM)
        {
            BGM = GameObject.FindGameObjectWithTag("BGMSource").GetComponent<AudioSource>();
            BGM.PlayDelayed(10f);
            BGM.volume = 1f;
        }
    }

    private void SceneLoaded_Received(byte eventCode, object content, int senderId)
    {
        if (this == null)
            return;

        object[] datas = content as object[];
        GameObject temp = null;
        if (eventCode.Equals(33)) // 마스터클라 전용  챔피언 오브젝트 등록
        {
            int receiveViewID = (int)datas[0];
            foreach (int viewID in RedIncomingEvents)
            {
                if (viewID == receiveViewID)
                    return;
            }
            foreach (int viewID in BlueIncomingEvents)
            {
                if (viewID == receiveViewID)
                    return;
            }

            LoadedPlayer++;

            if ((string)datas[1] == "red")
            {
                RedIncomingEvents.Add(receiveViewID);
                temp = PhotonView.Find(RedIncomingEvents[RedIncomingEvents.Count - 1]).gameObject;
                redTeamPlayer.Add(temp);
                temp.transform.position = redPos[RedIncomingEvents.Count - 1];
            }
            else if ((string)datas[1] == "blue")
            {
                BlueIncomingEvents.Add(receiveViewID);
                temp = PhotonView.Find(BlueIncomingEvents[BlueIncomingEvents.Count - 1]).gameObject;
                blueTeamPlayer.Add(temp);
                temp.transform.position = bluePos[BlueIncomingEvents.Count - 1];
            }

            SetChampionMove();
        }
        else if (eventCode.Equals(34)) // 모든 클라이언트 수신. 벽해제
        {
            for (int i = 0; i < wellTowers.Count; ++i)
                wellTowers[i].SetActive(true);
            runOnce = true;
            StartCoroutine(StartingWall_Off());
        }
        else if (eventCode.Equals(35))// 타 클라이언트 수신. 매니저 동기화용
        {
            temp = null;
            int redTotalCount = (int)datas[datas.Length - 1];
            int redCount = 0;
            int blueCount = 0;
            for (int i = 0; i < datas.Length - 1; i++)//blue
            {
                temp = PhotonView.Find((int)datas[i]).gameObject;
                if (i < redTotalCount)
                {
                    temp.transform.position = redPos[redCount];
                    redCount++;
                    redTeamPlayer.Add(temp);
                    //temp.transform.parent.GetChild(1).position = temp.transform.position;
                }
                else
                {
                    temp.transform.position = bluePos[blueCount];
                    blueCount++;
                    blueTeamPlayer.Add(temp);
                    //temp.transform.parent.GetChild(1).position = temp.transform.position;
                }
                SetChampionMove();
            }
        }
        else if (eventCode.Equals(151))//게임종료
        {
            isGameEnd = true;
            // 게임한번 끝나고 새게임시작할때 이전 인게임매니저에 이벤트가 들어올때가 있어서 끝날때 빼줌
            PhotonNetwork.OnEventCall -= SceneLoaded_Received;

            //진팀이랑 같은가.
            if ((string)datas[0] == "red")//진 팀이 레드인가?
            {
                StartCoroutine(EndSequence("red"));//레드 넥서스 꽝)
            }
            else if ((string)datas[0] == "blue")
            {
                StartCoroutine(EndSequence("blue"));//블루 넥서스 꽝
            }
        }
    }


    private void Event_StartingWall_Off()
    {
        byte evcode = 34;
        object[] datas = { evcode };
        RaiseEventOptions op = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(evcode, datas, true, op);
        
    }

    private void Event_SendViewID()
    {
        byte evcode = 35;

        object[] datas = new object[RedIncomingEvents.Count + BlueIncomingEvents.Count + 1];
        for (int i = 0; i < RedIncomingEvents.Count; i++)
        {
            datas[i] = RedIncomingEvents[i];
        }
        for (int i = 0; i < BlueIncomingEvents.Count; i++)
        {
            datas[RedIncomingEvents.Count + i] = BlueIncomingEvents[i];
        }
        datas[RedIncomingEvents.Count + BlueIncomingEvents.Count] = RedIncomingEvents.Count;

        RaiseEventOptions op = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(evcode, datas, true, op);
    }

    public void GameEnded(string Team)//모두에게 진팀 보내주기
    {
        byte evcode = 151;
        object[] datas = new object[] { (string)Team };
        RaiseEventOptions op = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(evcode, datas, true, op);
        PhotonNetwork.SendOutgoingCommands();
    }

    IEnumerator EndSequence(string team)
    {
        Debug.LogError("미니언데드카운트 :" + minionDeadCount);
        yield return new WaitForSeconds(0.5f);
        // 상점이나 옵션 열려있으면 꺼버림
        var shop = GameObject.FindGameObjectWithTag("ShopCanvas");
        if (shop != null)
            shop.SetActive(false);

        var option = GameObject.FindGameObjectWithTag("OptionCanvas");
        if (option != null)
            option.SetActive(false);

        if (team.Equals("red"))
        {
            mainCam.SetTarget(NexusRed.transform);
            yield return new WaitForSeconds(1f);
            //넥서스 꽝 -> 넥서스 파괴전에 레이즈 이벤트로 다 보내서 WinLose시스템 메세지 출력할것.
            NexusRed.GetComponent<SuppressorBehaviour>().bomb = true;
        }
        else if (team.Equals("blue"))
        {
            mainCam.SetTarget(NexusBlue.transform);
            //넥서스 꽝 -> 넥서스 파괴전에 레이즈 이벤트로 다 보내서 WinLose시스템 메세지 출력할것.
            yield return new WaitForSeconds(1f);
            NexusBlue.GetComponent<SuppressorBehaviour>().bomb = true;
        }
        yield return null;
    }

    IEnumerator StartingWall_Off()
    {

        yield return new WaitForSeconds(WaitingT);
        PhotonNetwork.automaticallySyncScene = false;

        for (int i = 0; i < StartingWall.transform.childCount - 1; i++)
        {
            Destroy(StartingWall);
            StartingWall.GetComponentInChildren<Pathfinding.NavmeshCut>().enabled = false;
        }
        Destroy(Logo);
        Invoke("Annoucement", 3f);

        // 로딩이끝나면 카메라를 다시 나에게로 돌려줌
        if (PhotonNetwork.player.GetTeam().ToString().Equals("red"))
        {
            foreach (GameObject go in redTeamPlayer)
            {
                if (go.GetPhotonView().owner.Equals(PhotonNetwork.player))
                {
                    mainCam.SetTarget(go.transform);
                    break;
                }
            }
        }
        else
        {
            foreach (GameObject go in blueTeamPlayer)
            {
                if (go.GetPhotonView().owner.Equals(PhotonNetwork.player))
                {
                    mainCam.SetTarget(go.transform);
                    break;
                }
            }
        }

        yield return new WaitForSeconds(2.0f);
        mainCam.ResetTarget();

        yield return null;
    }

    private void Annoucement()
    {
        sysmsg.Annoucement(1, true);
    }

    private void Update()
    {
        if (PhotonNetwork.isMasterClient)
        {
            if (LoadedPlayer == PhotonNetwork.playerList.Length && !runOnce)
            {
                runOnce = true;
                Event_SendViewID();
                Event_StartingWall_Off();
            }
            if (!TimerOnce && runOnce)
            {
                if (IngameTimer.temp >= 28f)
                {
                    TimerOnce = true;
                    sysmsg.Annoucement(2, true);
                }
            }
        }
    }

    private void SetChampionMove()
    {
        if (redTeamPlayer.Count >= 1)
        {
            for (int i = 0; i < redTeamPlayer.Count; i++)
            {
                redTeamPlayer[i].transform.GetComponent<Pathfinding.AIBase>().enabled = true;
                redTeamPlayer[i].transform.GetComponent<Pathfinding.RVO.RVOController>().radius = 0.5f;
                redTeamPlayer[i].transform.GetComponent<Pathfinding.RVO.RVOController>().enabled = true;
            }
        }
        if (blueTeamPlayer.Count >= 1)
        {
            for (int i = 0; i < blueTeamPlayer.Count; i++)
            {
                blueTeamPlayer[i].transform.GetComponent<Pathfinding.AIBase>().enabled = true;
                blueTeamPlayer[i].transform.GetComponent<Pathfinding.RVO.RVOController>().radius = 0.5f;
                blueTeamPlayer[i].transform.GetComponent<Pathfinding.RVO.RVOController>().enabled = true;
            }
        }
    }

    // 마스터가 겜 나가면 플레이어들 로비로 보내버림
    public override void OnPhotonPlayerDisconnected(PhotonPlayer otherPlayer)
    {
        if (otherPlayer.IsMasterClient)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }
}