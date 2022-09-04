using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRangeCollider : MonoBehaviour
{
    [SerializeField]
    private UnitController myUnitController;

    private void OnTriggerStay(Collider other)
    {
        //rangeCollider.enabled = true;

        if (myUnitController.TargetUnit != null)
        {
            if (other.gameObject == myUnitController.TargetUnit.gameObject)
            {
                myUnitController.isAttack = true;

                //StartCoroutine(myUnitController.PreparateAttack());
                myUnitController.PreparateAttack();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        myUnitController.isAttack = false;

        //myUnitController.TargetUnit.gameObject = null;
    }
}
