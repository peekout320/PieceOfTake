using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class UnitController : MonoBehaviour
{
    private Rigidbody rigid;

    [SerializeField]
    private UnitController targetUnit;
    public UnitController TargetUnit { get => targetUnit; }

    private List<UnitController> unitList = new List<UnitController>();

    [SerializeField]
    private ParticleSystem isAttackParticle;

    //攻撃間隔用タイマー
    [SerializeField]
    private int timer;

    //敵の距離を比較するための基準となる変数。適当な数値を代入
    public float standerdDistanceValue = 1000;

    private GameManager gameManager;
    private UIManager uiManager;

    private NavMeshAgent agent;

    public Tweener tweener;

    private int maxHp;

    public int unitNumber;

    public bool isAttack = false;

    [SerializeField]
    LayerMask stageLayer;

    private Animator anime;
    private int attackAnime;
    private int knockBackAnime;
    private int walkAnime;
    private int deadAnime;

    //ユニットステータス群
    [SerializeField, Header("コスト")]
    private int cost;
    public int Cost { get => cost; }
    [SerializeField, Header("HP")]
    private int hp;
    [Header("攻撃力")]
    public int attackPower;
    [Header("衝撃力")]
    public float blowPower;
    [SerializeField, Header("移動速度")]
    private float moveSpeed;
    [SerializeField, Header("重量")]
    private float weight;
    [SerializeField, Header("攻撃間隔")]
    private float intervalTime;
    [SerializeField, Header("攻撃範囲")]
    private BoxCollider attackRangeSize;
    public bool isGround;
    public Material material;

    private void Start()
    {
        //コンポーネントのキャッシュ
        anime = GetComponent<Animator>();
        attackAnime = Animator.StringToHash("attack");
        knockBackAnime = Animator.StringToHash("knockBack");
        walkAnime = Animator.StringToHash("walk");
        deadAnime = Animator.StringToHash("dead");

        rigid = GetComponent<Rigidbody>();

        isGround = true;

        //OnJudgeGround();
    }
    /// <summary>
    /// ユニットステータスの設定
    /// </summary>
    /// <param name="uiManager"></param>
    /// <param name="unitGenerator"></param>
    /// <returns></returns>
    public void SetupUnitStateAlly(List<UnitData> unitDatas,UIManager uiManager)
    {
        agent = GetComponent<NavMeshAgent>();

        this.uiManager = uiManager;

        cost = unitDatas[uiManager.btnIndex].cost;
        hp = unitDatas[uiManager.btnIndex].hp;
        attackPower = unitDatas[uiManager.btnIndex].attackPower;
        blowPower = unitDatas[uiManager.btnIndex].blowPower;
        agent.speed = unitDatas[uiManager.btnIndex].moveSpeed;
        weight = unitDatas[uiManager.btnIndex].weight;
        (attackRangeSize.size, attackRangeSize.center) = DataBaseManager.instance.GetAttackRange(unitDatas[uiManager.btnIndex].attackRangeType);
        isGround = unitDatas[uiManager.btnIndex].isGround;
        intervalTime = unitDatas[uiManager.btnIndex].intervalTime;
        maxHp = hp;

        material = unitDatas[uiManager.btnIndex].material;
        this.GetComponent<Renderer>().material = material;
    }

    public void SetupUnitStateEnemy(List<UnitData> unitDatas)
    {
        agent = GetComponent<NavMeshAgent>();

        cost = unitDatas[unitNumber].cost;
        hp = unitDatas[unitNumber].hp;
        attackPower = unitDatas[unitNumber].attackPower;
        blowPower = unitDatas[unitNumber].blowPower;
        agent.speed = unitDatas[unitNumber].moveSpeed;
        weight = unitDatas[unitNumber].weight;
        (attackRangeSize.size, attackRangeSize.center) = DataBaseManager.instance.GetAttackRange(unitDatas[unitNumber].attackRangeType);
        isGround = unitDatas[unitNumber].isGround;
        intervalTime = unitDatas[unitNumber].intervalTime;
        maxHp = hp;

        material = unitDatas[unitNumber].material;
        this.GetComponent<Renderer>().material = material;
    }

    /// <summary>
    /// MoveUnitをUnitController上でCoroutineで動かすためのメソッド
    /// </summary>
    /// <param name="gameManager"></param>
    /// <param name="unitList"></param>
    public void StartMoveUnit(GameManager gameManager, List<UnitController> unitList)
    {
        this.unitList = unitList;
        this.gameManager = gameManager;

        StartCoroutine("OnMoveUnit");
    }

    /// <summary>
    /// ユニットの移動
    /// </summary>
    /// <param name="gameManager"></param>
    /// <returns></returns>
    public IEnumerator OnMoveUnit()
    {
        //Debug.Log("監視開始");
        while (this.hp > 0)
        {
            //EnemyListに登録してあるユニットの内、一番近いユニットに向かって移動する処理↓↓                 
            foreach (UnitController target in unitList)
            {
                if (gameManager.gameMode == GameManager.GameMode.Play && target != null)
                {
                    //攻撃間隔タイマーはMoveUnitメソッド内で換算
                    timer++;
                    
                    //EnemyUnitList内に登録してあるオブジェクトとの距離を測り変数に代入する
                    float nearTargetDistanceValue = Vector3.SqrMagnitude(target.transform.position - transform.position);

                    //基準値より小さければその数値を基準値に代入していき一番小さい数値が変数に残る。その数値を持つオブジェクトが一番近い敵となる
                    if (standerdDistanceValue > nearTargetDistanceValue)
                    {
                        standerdDistanceValue = nearTargetDistanceValue;

                        targetUnit = target;
                    }

                    if (targetUnit != null　&& targetUnit.isGround == true)
                    {
                        //ナビメッシュを使用した移動
                        agent.destination = targetUnit.transform.position;

                        anime.SetFloat(walkAnime, agent.velocity.sqrMagnitude);

                        //進行方向を向く
                        transform.LookAt(targetUnit.transform);
                    }
                    else
                    {
                        standerdDistanceValue = 1000;
                        anime.SetFloat(walkAnime, 0);
                    }
                }
            }
            yield return null;
        }
        Debug.Log(this.name + "MoveUnit終了");
    }

    /// <summary>
    /// 一定間隔毎に攻撃アニメーションを実行
    /// </summary>
    /// <returns></returns>
    public void PreparateAttack()
    {
        if (this.hp > 0 && gameManager.gameMode == GameManager.GameMode.Play)
        {
            if (targetUnit.hp > 0)
            {
                if (timer > intervalTime)
                {
                    timer = 0;

                    //Attack(attackPower);
                    anime.SetTrigger(attackAnime);
                }
            }
        }
    }

    /// <summary>
    /// ダメージを与える処理
    /// </summary>
    /// <param name="amount"></param>
    public void OnDamage(int amount)
    {
        this.hp = Mathf.Clamp(this.hp -= amount, 0, maxHp);

        if(this.hp <= 0)
        {
            agent.enabled = true;
            agent.isStopped = true;
            targetUnit = null;
            anime.SetTrigger(deadAnime);

            gameManager.GenerateEnemyList.Remove(this);
            gameManager.GenerateAllyList.Remove(this);

            Destroy(this.gameObject, 3);
        }
    }

    /// <summary>
    /// ノックバック演出
    /// </summary>
    /// <param name="blowPower"></param>
    public void OnKnockBack(float blowPower)
    {
        anime.SetTrigger(knockBackAnime);

        //ステージ外に出たら落ちて破壊される処理
        StopCoroutine("OnMoveUnit");
        //targetUnit = null;
        agent.enabled = false;
        rigid.isKinematic = false;
        tweener = rigid.DOMove(transform.forward * -blowPower, 1)
            .OnComplete(() => SwitchOnMoveUnit())
            .SetLink(this.gameObject);
    }

    /// <summary>
    /// 弓矢・魔法を発射するエフェクト
    /// </summary>
    public void OnAttackPartical()
    {
        isAttackParticle.Play();
    }

    /// <summary>
    /// 攻撃アニメーションで呼び出すメソッド
    /// </summary>
    public void AnimationEventDamage()
    {
        if (targetUnit != null)
        {
            targetUnit.OnDamage(this.attackPower);

            //ノックバック演出
            targetUnit.OnKnockBack(this.blowPower);
        }
    }

    //private void OnJudgeGround()
    //{
    //    StartCoroutine(JudgeGoround());
    //}

    //private IEnumerator JudgeGoround()
    //{
    //    while (true)
    //    {
    //        if (!isGround)
    //        {
    //            StopCoroutine(OnMoveUnit());
    //            agent.enabled = false;
    //            rigid.isKinematic = false;
    //            Destroy(this.gameObject, 1);
    //        }
    //        yield return null;                
    //    }
    //}

    private void SwitchOnMoveUnit()
    {
        if (JudgeGround() == true)
        {
            rigid.isKinematic = true;
            agent.enabled = true;
            StartCoroutine("OnMoveUnit");
        }
    }

    /// <summary>
    /// ノックバック後ユニットから下方向へRayを飛ばしstageLayerのオブジェクトに接触した場合はステージ上にいるとしてtrueを返す。
    /// </summary>
    /// <returns></returns>
    private bool JudgeGround()
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.2f, Vector3.down);
        Debug.DrawRay(transform.position + Vector3.up * 0.2f, Vector3.down);
        if (Physics.Raycast(ray.origin, ray.direction, out RaycastHit hit, 4.0f, stageLayer))
        {
            return true;
        }
        return false;
    }
}
