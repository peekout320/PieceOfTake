using UnityEngine;

[System.Serializable]
public class UnitData 
{
    public int unitNo;
    public int cost;
    public int hp;
    public int attackPower;
    public float blowPower;
    public float moveSpeed;
    public float weight;
    public float intervalTime;
    public AttackRangeType attackRangeType;

    public Material material;

    //public Sprite unitSprite;
    //public AnimationClip moveAnime;
    //public AnimationClip attackAnime;
    //public AnimationClip deadAnime;
}
