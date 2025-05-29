using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
using UnityEngine.UI;

public class MonsterSpawner : MonoBehaviour
{
    List<GameObject> TheMonster = new List<GameObject>(); //The current created monster
    List<SkeletonAnimation> monsterAnimator = new List<SkeletonAnimation>(); //The animator script of the monster

    public Text monsterNameText;

    public List<Transform> TheFourPositions = new List<Transform>();

    public List<GameObject> AllMonsters = new List<GameObject>(); //a list of all monsters in the asset

    int CurrentMonster = 0;



    private void Start()
    {
        CurrentMonster =  0;
        for (int i = 0; i < 4; i++)
        {
            TheMonster.Add(null);
            monsterAnimator.Add(null);
        }
        SummonNewMonster();
    }

    public void SummonNextPrevMonster(int PrevOrNext) //Summon either next or previous monster
    {

        CurrentMonster += PrevOrNext;
        if (CurrentMonster >= AllMonsters.Count)
            CurrentMonster = 0;
        else if (CurrentMonster < 0)
            CurrentMonster = AllMonsters.Count - 1;


        //CurrentMonster += 3;
        //List<int> PlusOne = new List<int>() { 3, 56, 60 };
        //List<int> PlusTwo = new List<int>() { 10, 30, 97 };
        //List<int> PlusThree = new List<int>() { 44 };

        //Debug.Log(CurrentMonster);
        //if (PlusOne.Contains(CurrentMonster))
        //{
        //    CurrentMonster++;
        //}
        //else if (PlusTwo.Contains(CurrentMonster))
        //{
        //    CurrentMonster += 2;
        //}
        //else if (PlusThree.Contains(CurrentMonster))
        //{
        //    CurrentMonster += 3;
        //}

        SummonNewMonster();
    }
       
    public void SummonNewMonster()
    {
        if (TheMonster.Count > 0)
        {
            for (int i = 0; i < 4; i++)
            {
                if (TheMonster[i] != null)
                    Destroy(TheMonster[i]);
            }
        }

        //Create the selected monster
        for (int i = 0; i < 4; i++)
        {
            TheMonster[i] = Instantiate(AllMonsters[CurrentMonster], TheFourPositions[i]);
        }

        //Special dimensions for certain monsters
        string MonsterName = TheMonster[0].name;
        float additioanlYPos = 0;
        float scalingFactor = 1;

        if (MonsterName.Contains("Salamander"))
        {
            scalingFactor = 1.35f;
        }
        else if (MonsterName.Contains("Floating"))
        {
            scalingFactor = 0.67f;
            additioanlYPos = 12;
        }
        else if (MonsterName.Contains("Rabbit"))
        {
            scalingFactor = 0.75f;
        }
        else if (MonsterName.Contains("Mushroom"))
        {
            scalingFactor = 0.67f;
        }
        else if (MonsterName.Contains("Book"))
        {
            scalingFactor = 0.67f;
            additioanlYPos = 60;
        }
        else if (MonsterName.Contains("Corrupted"))
        {
            scalingFactor = 1.3f;
            additioanlYPos = 40;
        }
        else if (MonsterName.Contains("Ox"))
        {
            scalingFactor = 1.25f;
        }
        else if (MonsterName.Contains("Raptor"))
        {
            scalingFactor = 1.25f;
        }
        else if (MonsterName.Contains("Orc"))
        {
            scalingFactor = 1.1f;
        }
        else if (MonsterName.Contains("Snail"))
        {
            scalingFactor = 0.9f;
        }
        else if (MonsterName.Contains("Shell"))
        {
            scalingFactor = 1.2f;
        }
        else if (MonsterName.Contains("Lizard"))
        {
            scalingFactor = 1.55f;
        }
        else if (MonsterName.Contains("Hamy"))
        {
            scalingFactor = 1.3f;
        }
        //Change position and scale of the monster
        for (int i = 0; i < 4; i++)
        {
            TheMonster[i].transform.localPosition = new Vector2(0, additioanlYPos);
            TheMonster[i].transform.localScale = new Vector2(48, 48) * scalingFactor;
            monsterAnimator[i] = TheMonster[i].GetComponent<SkeletonAnimation>();
        }



        //string manipulation to get the name and id of the monster
        int idIndexStart = MonsterName.IndexOf('_', 1);
        int nameIndexStart = MonsterName.IndexOf('_', 9);
        int nameIndexEnd = MonsterName.IndexOf('(', 9);

        monsterNameText.text = "Monster "+ MonsterName.Substring (idIndexStart+1,nameIndexStart-idIndexStart-1)+ " : " +MonsterName.Substring(nameIndexStart + 1, nameIndexEnd - nameIndexStart -1);

        ChangeAnimation("Idle");
    }

    public void ChangeAnimation(string AnimationName)  //Names are: Idle, Walk, Death, Hurt and Attack
    {
        if (monsterAnimator == null)
            return;

        bool IsLoop = AnimationName == "Death" ? false : true;

        //set the animation state to the selected one for each directional monster
        monsterAnimator[0].skeleton.SetSkin("Side");
        monsterAnimator[0].skeleton.SetSlotsToSetupPose();
        monsterAnimator[0].AnimationState.SetAnimation(0, "Side_" + AnimationName, IsLoop);

        monsterAnimator[1].skeleton.SetSkin("Side");
        monsterAnimator[1].skeleton.SetSlotsToSetupPose();
        monsterAnimator[1].AnimationState.SetAnimation(0, "Side_" + AnimationName, IsLoop);

        monsterAnimator[2].skeleton.SetSkin("Back");
        monsterAnimator[2].skeleton.SetSlotsToSetupPose();
        monsterAnimator[2].AnimationState.SetAnimation(0, "Back_" + AnimationName, IsLoop);

        monsterAnimator[3].skeleton.SetSkin("Front");
        monsterAnimator[3].skeleton.SetSlotsToSetupPose();
        monsterAnimator[3].AnimationState.SetAnimation(0, "Front_" + AnimationName, IsLoop);
    }

    public void RateUs()
    {
        System.Diagnostics.Process.Start("https://assetstore.unity.com/packages/slug/216312#reviews");
    }

}
