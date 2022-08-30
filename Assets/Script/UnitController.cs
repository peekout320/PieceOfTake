using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class UnitController : MonoBehaviour
{
    public UnitController targetUnit;

    private GameManager gameManager;
    private UIManager uiManager;

    private NavMeshAgent agent;

    //敵の距離を比較するための基準となる変数。適当な数値を代入
    private float standardDistanceValue = 1000;　

    private int maxHp;

    [SerializeField]
    private int timer;

    private Vector3 latestPos;

    public bool isAttack = false;

    private Animator anime;
    private int attackAnime;
    private int walkAnime;

    //ユニットステータス群
    [SerializeField, Header("ユニットNo.")]
    private int unitNo;
    [SerializeField, Header("コスト")]
    private int cost;
    public int Cost { get => cost; }
    [SerializeField, Header("HP")]
    private int hp;
    [SerializeField, Header("攻撃力")]
    private int attackPower;
    [SerializeField, Header("衝撃力")]
    private float blowPower;
    [SerializeField, Header("移動速度")]
    private float moveSpeed = 0.01f;
    [SerializeField, Header("重量")]
    private float weight;
    [SerializeField, Header("攻撃間隔")]
    private float intervalTime;
    [SerializeField, Header("攻撃範囲")]
    private BoxCollider attackRangeSize;
    
    public Material material;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        anime = GetComponent<Animator>();
        attackAnime = Animator.StringToHash("attack");
        walkAnime = Animator.StringToHash("walk");

    }
    /// <summary>
    /// ユニットステータスの設定
    /// </summary>
    /// <param name="uiManager"></param>
    /// <param name="unitGenerator"></param>
    /// <returns></returns>
    public void SetupUnitState(List<UnitData> unitDatas,UIManager uiManager)
    {
        this.uiManager = uiManager;

        unitNo = unitDatas[uiManager.btnIndex].unitNo;
        cost = unitDatas[uiManager.btnIndex].cost;
        hp = unitDatas[uiManager.btnIndex].hp;
        attackPower = unitDatas[uiManager.btnIndex].attackPower;
        blowPower = unitDatas[uiManager.btnIndex].blowPower;
        moveSpeed = unitDatas[uiManager.btnIndex].moveSpeed;
        weight = unitDatas[uiManager.btnIndex].weight;
        (attackRangeSize.size, attackRangeSize.center) = DataBaseManager.instance.GetAttackRange(unitDatas[uiManager.btnIndex].attackRangeType);
        intervalTime = unitDatas[uiManager.btnIndex].intervalTime;
        maxHp = hp;

        material = unitDatas[uiManager.btnIndex].material;
        this.GetComponent<Renderer>().material = material;
    }

    /// <summary>
    /// ユニットの移動
    /// </summary>
    /// <param name="gameManager"></param>
    /// <returns></returns>
    public IEnumerator MoveUnit(GameManager gameManager,List<UnitController> unitList)
    {
        this.gameManager = gameManager;

        //Debug.Log("監視開始");
        while (this.hp >= 0)
        {
            //EnemyListに登録してあるユニットの内、一番近いユニットに向かって移動する処理↓↓                 
            foreach (UnitController target in unitList)
            {
                if (gameManager.gameMode == GameManager.GameMode.Play && target != null)
                {
                    //EnemyUnitList内に登録してあるオブジェクトとの距離を測り変数に代入する
                    float nearTargetDistanceValue = Vector3.SqrMagnitude(target.transform.position - transform.position);

                    //基準値より小さければその数値を基準値に代入していき一番小さい数値が変数に残る。その数値を持つオブジェクトが一番近い敵となる
                    if (standardDistanceValue > nearTargetDistanceValue)
                    {
                        standardDistanceValue = nearTargetDistanceValue;

                        targetUnit = target;
                    }

                    if (targetUnit != null)
                    {
                        //ナビメッシュを使用した移動
                        agent.destination = targetUnit.transform.position;

                        int walkpower = Animator.StringToHash("walk");
                        anime.SetFloat(walkpower, agent.velocity.sqrMagnitude);

                        transform.LookAt(targetUnit.transform);
                        //進行方向を向く
                        //Vector3 diff = transform.position - latestPos;
                        //latestPos = transform.position;

                        //if (diff.magnitude > 0.01f)
                        //    transform.rotation = Quaternion.LookRotation(diff);
                    }
                    else
                        anime.SetFloat(walkAnime, 0);
                        //agent.speed = 0;

                }
            }
            yield return null;
        }
    }

    /// <summary>
    /// MoveUnitをUnitController上でCoroutineで動かすためのメソッド
    /// </summary>
    /// <param name="gameManager"></param>
    /// <param name="unitList"></param>
    public void StartMoveUnit(GameManager gameManager, List<UnitController> unitList)
    {
        StartCoroutine(MoveUnit(gameManager, unitList));
    }

    /// <summary>
    /// 一定間隔毎にAttack()メソッドを実行
    /// </summary>
    /// <returns></returns>
    public IEnumerator PreparateAttack()
    {
        Debug.Log("攻撃準備開始");

        while (isAttack)
        {
            if (gameManager.gameMode == GameManager.GameMode.Play)
            {
                timer++;

                if (timer > intervalTime)
                {
                    timer = 0;

                    //Attack(attackPower);
                    anime.SetTrigger(attackAnime);
                    
                }
            }
            yield return null;
        }
        yield break;
    }

    /// <summary>
    /// 対象にダメージを与える、倒した時の処理、ノックバック演出
    /// </summary>
    /// <param name="amount"></param>
    public void Attack()
    {
        Debug.Log("Attack()開始");
        if (targetUnit != null)
        {
            targetUnit.hp = Mathf.Clamp(targetUnit.hp -= attackPower, 0, maxHp);

            if (targetUnit.hp <= 0)
            {
                targetUnit.agent.isStopped = true;
                targetUnit.targetUnit = null;

                isAttack = false;
                targetUnit.isAttack = false;
                targetUnit.timer = 0;
                standardDistanceValue = 1000;

                targetUnit.anime.SetTrigger("dead");

                gameManager.EnemyList.Remove(targetUnit);
                gameManager.AllyList.Remove(targetUnit);

                Destroy(targetUnit.gameObject, 3);
                targetUnit = null;
            }
            else
                //ノックバック演出
                KnockBack(this.blowPower);
        }
    }

    /// <summary>
    /// ノックバック演出
    /// </summary>
    /// <param name="blowPower"></param>
    private void KnockBack(float blowPower)
    {
        targetUnit.agent.velocity += transform.forward * blowPower;
        targetUnit.anime.SetTrigger("knockBack");
        //Rigidbody targetRb = targetUnit.GetComponent<Rigidbody>();
        //targetRb.velocity = transform.forward * blowPower;
        blowPower *= 0.98f;
        //targetUnit.transform.DOMove(transform.forward * blowPower,1);

    }
}
