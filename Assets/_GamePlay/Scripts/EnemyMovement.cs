using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    NavMeshAgent agent;
    public Transform stackHolder;
    public GameObject stackPrefab;
    private int numOfStacks;

    public Constant.BrickTags brickTag;
    public Constant.BridgeTag bridgeTag;

    public Material bridgeMaterial;

    public GameObject brickPrefab;
    public Vector3 targetPos;
    public Animator enemyAnimator;

    private int currentStage;
    public List<Transform> stagePoints;

    public BrickSpawner brickSpawner;

    public Constant.BrickType brickType;

    private bool isWin;
    private bool isLose = false;

    public EnemyMovement thisEnemy;
    public List<EnemyMovement> enemies = new List<EnemyMovement>();
    public PlayerMovement player;

    public List<Transform> enemiesPos;
    public Transform playerPos;
    public Transform posFinish;

    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        targetPos += transform.position + new Vector3(0, 0, 0.3f);
        agent.SetDestination(targetPos);
        currentStage = 0;
        numOfStacks = 0;
        isWin = false;
    }

    public void MovePosBrick()
    {
        if (Vector3.Distance(transform.position, targetPos) < 0.1f)
        {
            targetPos = SimplePool.GetPositionBrick(brickPrefab);
            agent.SetDestination(targetPos);
        }
    }

    public void MovePosStageNewAndSpawnBrick()
    {
        if (Vector3.Distance(transform.position, stagePoints[currentStage].position) < 0.06f)
        {
            currentStage++;
            SimplePool.Collect(brickPrefab);
            brickSpawner.SpawnerBrick((int)brickType, currentStage);
            targetPos = SimplePool.GetPositionBrick(brickPrefab);
            agent.SetDestination(targetPos);
        }
    }

    public void BuildBridge()
    {
        int layermask = LayerMask.GetMask(Constant.LAYER_BRIDGE);
        if (Physics.Raycast(transform.position + Vector3.forward * 0.1f + Vector3.up * 10f, Vector3.down, out RaycastHit hit, Mathf.Infinity, layermask))
        {
            if (hit.collider.CompareTag(bridgeTag.ToString()))
            {
                return;
            }
            else
            {
                if (numOfStacks == 0)
                {
                    agent.SetDestination(targetPos);
                }
                else
                {
                    hit.collider.gameObject.GetComponent<Renderer>().enabled = true;
                    hit.collider.gameObject.GetComponent<Renderer>().material = bridgeMaterial;
                    hit.collider.tag = bridgeTag.ToString();
                    RemoveStack();
                    SimplePool.Respawn(brickPrefab);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isWin) return;
       /* if (isLose)
        {
            Lose();
            return;
        }*/
        enemyAnimator.SetBool(Constant.ANIM_ISRUN, true);

        MovePosBrick();

        MovePosStageNewAndSpawnBrick();

        BuildBridge();
    }

    public void AddStack()
    {
        GameObject newStack = SimplePool.Spawn(stackPrefab, stackHolder.position + stackHolder.up * numOfStacks * 0.05f, stackHolder.rotation);
        newStack.transform.SetParent(stackHolder);
        numOfStacks += 1;
        if (numOfStacks >= 8)
        {
            agent.SetDestination(stagePoints[currentStage].position);
        }
    }

    public void RemoveStack()
    {
        SimplePool.DespawnNewest(stackPrefab);
        numOfStacks -= 1;
    }

    public int GetNumOfStacks()
    {
        return numOfStacks;
    }

    public void ColliderEnemy()
    {
        foreach(EnemyMovement enemy in enemies)
        {
            if (thisEnemy.GetNumOfStacks() < enemy.GetNumOfStacks())
            {
                Fall();
                return;
            }
        }
    }

    public void ColliderPlayer()
    {
        if(thisEnemy.GetNumOfStacks() < player.GetNumOfStack())
        {
            Fall();
        }
    }

    public void Win()
    {
        isWin = true;
        enemyAnimator.Play(Constant.ANIM_WIN);
        SimplePool.Collect(stackPrefab);
        agent.enabled = true;
    }

    public void Lose()
    {
        if(Vector3.Distance(playerPos.position, posFinish.position) < 0.1f)
        {
            agent.isStopped = true;
            enemyAnimator.SetBool(Constant.ANIM_LOSE, true);
            SimplePool.Collect(stackPrefab);
        }

        for(int i = 0; i < enemies.Count; i++)
        {

        }
    }

    public void Fall()
    {
        StartCoroutine(NotFall());
    }

    IEnumerator NotFall()
    {
        enemyAnimator.SetBool(Constant.ANIM_ISFALL, true);
        agent.speed = 0;
        while(numOfStacks > 0)
        {
            SimplePool.DespawnNewest(stackPrefab);
            numOfStacks--;
        }
        agent.isStopped = true;
        yield return new WaitForSeconds(4f);
        enemyAnimator.SetBool(Constant.ANIM_ISFALL, false);
        agent.SetDestination(targetPos);
        agent.speed = 1;
        agent.isStopped = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(brickTag.ToString()))
        {
            AddStack();
            SimplePool.Despawn(other.gameObject);
        }
        if (other.gameObject.CompareTag(Constant.TAG_FINISH))
        {
            Win();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(Constant.TAG_ENEMY))
        {
            ColliderEnemy();
        }

        if (collision.gameObject.CompareTag(Constant.TAG_PLAYER))
        {
            ColliderPlayer();
        }
    }
}
