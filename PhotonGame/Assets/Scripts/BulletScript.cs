using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class BulletScript : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    int dir;

    void Start() => Destroy(gameObject, 3.5f);

    void Update() => transform.Translate(Vector3.right * 7 * Time.deltaTime * dir);

    private void OnTriggerEnter2D(Collider2D col)
    {//총알이 벽인지 아닌지를 체크하는 부분(총알이 벽에 닿으면 파괴시키기 위해서)
        if (col.tag == "Ground") PV.RPC("DestroyRPC", RpcTarget.AllBuffered);

        if(!PV.IsMine && col.tag=="Player" && col.GetComponent<PhotonView>().IsMine)
        {//총알을 맞는 쪽에서 Hit판정을 해야함
            col.GetComponent<PlayerScript>().Hit();
            PV.RPC("DestroyRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void DirRPC(int dir) => this.dir = dir;

    [PunRPC]
    public void DestroyRPC() => Destroy(gameObject);
}
