using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Cinemachine;

public class PlayerScript : MonoBehaviourPunCallbacks, IPunObservable
{
    public Rigidbody2D RB;
    public Animator AN;
    public SpriteRenderer SR;
    public PhotonView PV;
    public Text NickNameText;
    public Image HealthImage;

    bool isGround;
    Vector3 curPos; 

    void Awake()
    {
        //닉네임
        //자신의 캐릭터의 닉네임 색깔은 초록색
        //상대방 캐릭터의 닉네임 색깔은 빨간색
        NickNameText.text = PV.IsMine ? PhotonNetwork.NickName : PV.Owner.NickName;
        NickNameText.color = PV.IsMine ? Color.green : Color.red;

        if (PV.IsMine)
        {
            //2D 카메라
            var CM = GameObject.Find("CMCamera").GetComponent<CinemachineVirtualCamera>();
            CM.Follow = transform;
            CM.LookAt = transform;
        }
    }
    
    void Update()
    {
        if (PV.IsMine)//내 화면이면
        {
            // <- -> 이동
            float axis = Input.GetAxisRaw("Horizontal");
            RB.velocity = new Vector2(4 * axis, RB.velocity.y);

            if (axis != 0)
            {//현재 이동키를 계속 누르고 있다면
                AN.SetBool("walk", true);//걷는상태로 표시
                PV.RPC("FlipXRPC", RpcTarget.AllBuffered, axis);
            }
            else AN.SetBool("walk", false);//이동키를 누르고 있지 않다면 걷는상태에서 Idle 상태로

            //↑ 점프,바닥체크
            isGround = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(0, -0.5f), 0.07f, 1 << LayerMask.NameToLayer("Ground"));
            AN.SetBool("jump", !isGround);
            if (Input.GetKeyDown(KeyCode.UpArrow) && isGround) PV.RPC("JumpRPC", RpcTarget.All);

            //스페이스 총알 발사
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PhotonNetwork.Instantiate("Bullet", transform.position + new Vector3(SR.flipX ? -0.4f : 0.4f, -0.11f, 0), Quaternion.identity)
                    .GetComponent<PhotonView>().RPC("DirRPC", RpcTarget.All, SR.flipX ? -1 : 1);
                AN.SetTrigger("shot");
            }
        }

        //내 화면이 아닐경우 부드럽게 동기화
        
        //내 화면에서의 플레이어의 위치와 다른 화면에서의 내 위치의 위치 차이가 너무많이 날경우에 양쪽 동기화를 위해서
        else if ((transform.position - curPos).sqrMagnitude >= 100) transform.position = curPos;

        else transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10);        
    }

    [PunRPC]
    void FlipXRPC(float axis) => SR.flipX = axis == -1;

    [PunRPC]
    void JumpRPC()
    {
        RB.velocity = Vector2.zero;
        RB.AddForce(Vector2.up * 700);
    }

    public void Hit()
    {
        HealthImage.fillAmount -= 0.1f;//총알에 맞을때마다 피를 0.1씩 깎음(총 체력은 1)

        if (HealthImage.fillAmount <= 0)
        {//플레이어의 체력이 모두 소진되었으면
            //죽어서 다시 리스폰 시키기 위한 부분
            //Respawn부분은 죽기전까지는 비활성화 되있기 때문에 바로 찾을수 없고 그 부모옵젝임 Canvas를 먼저 찾아야함
            GameObject.Find("Canvas").transform.Find("RespawnPanel").gameObject.SetActive(true);//Respawn 활성화
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void DestroyRPC() => Destroy(gameObject);

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //위치,체력 변수 동기화
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(HealthImage.fillAmount);
        }
        else
        {
            curPos=(Vector3)stream.ReceiveNext();
            HealthImage.fillAmount = (float)stream.ReceiveNext();
        }
    }
}
