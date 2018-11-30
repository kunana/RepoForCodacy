using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PingPooling : Photon.PunBehaviour
{
    //풀링
    public GameObject[] FxPrefabs = new GameObject[6];
    public List<GameObject> HelpPool = new List<GameObject>();
    public List<GameObject> DangerPool = new List<GameObject>();
    public List<GameObject> TargetPool = new List<GameObject>();
    public List<GameObject> MissingPool = new List<GameObject>();
    public List<GameObject> GoingPool = new List<GameObject>();
    public List<GameObject> HerePool = new List<GameObject>();

    public Vector3 adjustHeight = new Vector3(0, 1.4f, 0);

    //핑 횟수 제어
    public int MakeCount = 0;
    public int MakeMaxCount = 15;
    public float PingResetTime = 7f;
    public bool CanMakePing = true;
    public bool once = false;

    //레이
    Vector3 pos = Vector3.zero;
    Ray ray;
    RaycastHit[] hits;

    //동기화용 변수
    private byte MyEventGroup = 0;
    private GameObject Player;
    private string SenderName;
    public ChatFunction Chatmanager;
    private void Awake()
    {
        //풀링
        MakeFxPool("Going");
        MakeFxPool("Missing");
        MakeFxPool("Help");
        MakeFxPool("Danger");
        MakeFxPool("Target");
        MakeFxPool("Here");
        //동기화
        SenderName = PlayerData.Instance.championName;
        Player = GameObject.FindGameObjectWithTag("Player");


        Chatmanager = GameObject.FindGameObjectWithTag("ChatManager").GetComponentInChildren<ChatFunction>();
    }

    private void OnEnable()
    {
        if (PhotonNetwork.player.IsLocal)
        {
            if (PhotonNetwork.player.GetTeam().ToString().Equals("red"))
                MyEventGroup = 30;
            else
                MyEventGroup = 40;
        }

        PhotonNetwork.OnEventCall += SyncPing;
    }

    private void OnDestroy()
    {
        PhotonNetwork.OnEventCall -= SyncPing;

    }

    private void Update()
    {
        if (!CanMakePing || MakeCount >= MakeMaxCount && !once)
        {
            once = true;
            Invoke("PingReset", 7.0f);
        }

        if (!Chatmanager.chatInput.IsActive())
        {
            if (Input.GetKeyDown(KeyCode.G) && CanMakePing && !EventSystem.current.IsPointerOverGameObject() && CanMakePing)
            {
                GetFxPool("Here", MakePing(), false);
                MakeCount++;
                if (MakeCount >= MakeMaxCount)
                {
                    CanMakePing = false;
                }
            }
        }
    }

    private Vector3 MakePing()
    {
        pos = Vector3.zero;
        ray = Camera.main.ScreenPointToRay(Input.mousePosition); // 마우스 좌표를 기준으로  스크린을투과하는 레이
        hits = Physics.RaycastAll(ray);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.layer.Equals(LayerMask.NameToLayer("GroundLayer")))
            {
                pos = hit.point;
            }
        }
        return pos;
    }

    public void PingReset()
    {
        once = false;
        MakeCount = 0;
        CanMakePing = true;
    }

    private void SyncPing(byte eventCode, object content, int senderId)
    {
        if (eventCode.Equals(MyEventGroup))
        {
            PhotonPlayer sender = PhotonPlayer.Find(senderId);
            object[] datas = content as object[];
            if (datas.Length.Equals(4) && sender != PhotonNetwork.player)
                GetFxPool((string)datas[0], (Vector3)datas[1], true);
        }
    }

    //Fx 풀링
    public void MakeFxPool(string name)
    {
        if (name.Equals("Help"))
        {
            for (int i = 0; i < MakeMaxCount; i++)
            {
                var fx = Instantiate(FxPrefabs[0], transform);
                HelpPool.Add(fx);
                fx.transform.position = Vector3.zero;
                fx.gameObject.SetActive(false);
            }
        }
        else if (name.Equals("Danger"))
        {
            for (int i = 0; i < MakeMaxCount; i++)
            {
                var fx = Instantiate(FxPrefabs[1], transform);
                DangerPool.Add(fx);
                fx.transform.position = Vector3.zero;
                fx.gameObject.SetActive(false);
                fx.gameObject.layer = 12;
            }
        }
        else if (name.Equals("Missing"))
        {
            for (int i = 0; i < MakeMaxCount; i++)
            {
                var fx = Instantiate(FxPrefabs[2], transform);
                MissingPool.Add(fx);
                fx.transform.position = Vector3.zero;
                fx.gameObject.SetActive(false);
                fx.gameObject.layer = 12;
            }
        }
        else if (name.Equals("Going"))
        {
            for (int i = 0; i < MakeMaxCount; i++)
            {
                var fx = Instantiate(FxPrefabs[3], transform);
                GoingPool.Add(fx);
                fx.transform.position = Vector3.zero;
                fx.gameObject.SetActive(false);
                fx.gameObject.layer = 12;
            }
        }
        else if (name.Equals("Target"))
        {
            for (int i = 0; i < MakeMaxCount; i++)
            {
                var fx = Instantiate(FxPrefabs[4], transform);
                TargetPool.Add(fx);
                fx.transform.position = Vector3.zero;
                fx.gameObject.SetActive(false);
                fx.gameObject.layer = 12;
            }
        }
        else if (name.Equals("Here"))
        {
            for (int i = 0; i < MakeMaxCount; i++)
            {
                var fx = Instantiate(FxPrefabs[5], transform);
                HerePool.Add(fx);
                fx.transform.position = Vector3.zero;
                fx.gameObject.SetActive(false);
                fx.gameObject.layer = 12;
            }
        }
    }

    private void SendPING_Myteam(string name, Vector3 pos) // 레이즈이벤트. 핑 프리팹 이름, 월드 좌표, 샌더챔피언 이름, 샌더의 포지션
    {
        byte evCode = MyEventGroup;
        object[] content = new object[] { name, pos, SenderName, Player.transform.position };

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { CachingOption = EventCaching.DoNotCache, Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(evCode, content, false, raiseEventOptions);
    }

    public void GetFxPool(string name, Vector3 pos, bool islocal)
    {
        GameObject fx = null;

        if (name.Equals("Help"))
        {
            if (HelpPool.Count <= 0)
                MakeFxPool("Help");

            fx = HelpPool[0];
            HelpPool.RemoveAt(0);
            HelpPool.Add(fx);
            SoundManager.instance.PlaySound(SoundManager.instance.Help);
        }
        else if (name.Equals("Missing"))
        {
            if (MissingPool.Count <= 0)
                MakeFxPool("Missing");

            fx = MissingPool[0];
            MissingPool.RemoveAt(0);
            MissingPool.Add(fx);
            SoundManager.instance.PlaySound(SoundManager.instance.Missing);
        }
        else if (name.Equals("Going"))
        {
            if (GoingPool.Count <= 0)
                MakeFxPool("Going");

            fx = GoingPool[0];
            GoingPool.RemoveAt(0);
            GoingPool.Add(fx);
            SoundManager.instance.PlaySound(SoundManager.instance.Coming);
        }
        else if (name.Equals("Target"))
        {
            if (TargetPool.Count <= 0)
                MakeFxPool("Target");

            fx = TargetPool[0];
            TargetPool.RemoveAt(0);
            TargetPool.Add(fx);
            SoundManager.instance.PlaySound(SoundManager.instance.Help);
        }
        else if (name.Equals("Danger"))
        {
            if (DangerPool.Count <= 0)
                MakeFxPool("Danger");

            fx = DangerPool[0];
            DangerPool.RemoveAt(0);
            DangerPool.Add(fx);
            SoundManager.instance.PlaySound(SoundManager.instance.Danger);
        }
        else if (name.Equals("Here"))
        {
            if (HerePool.Count <= 0)
                MakeFxPool("Here");

            fx = HerePool[0];
            HerePool.RemoveAt(0);
            HerePool.Add(fx);
            SoundManager.instance.PlaySound(SoundManager.instance.basic);

        }

        fx.transform.position = pos;
        fx.transform.position = pos + adjustHeight;
        fx.gameObject.SetActive(true);

        if (!islocal)
        {
            SendPING_Myteam(name, pos);
        }
    }

}