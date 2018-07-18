using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    /// <summary>
    /// 甜品相关的成员变量
    /// </summary>
    #region
    //甜品的种类
    public enum SweetsType
    {
        EMPTY,
        NORMAL,
        BARRIER,
        ROW_CLEAR,
        COLUMN_CLEAR,
        RAINBOWCANDY,
        COUNT//标记类型
    }

    //甜品预制体的字典，可以通过甜品种类得到对应的游戏物体
    public Dictionary<SweetsType, GameObject> sweetPrefabDict;

    //必须调用这个面板才可见
    [System.Serializable]
    //字典面板是没用显示的，通过定义结构体来实现
    public struct SweetPrefab
    {
        public SweetsType type;
        public GameObject prefab;
    }

    //结构体数组
    public SweetPrefab[] sweetPrefabs;

    public GameObject gridPrefab;

    //甜品数组
    private GameSweet[,] sweets;

    //要交换的甜品对象
    private GameSweet pressedSweet;
    private GameSweet enteredSweet;
    #endregion

    //单例
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            return _instance;
        }

        set
        {
            _instance = value;
        }
    }

    //网格列与行
    public int xColumn;
    public int yRow;

    //填充时间
    public float fillTime;

    //有关游戏显示的内容
    public Text timeText;

    private float gameTime=60;

    private bool gameOver;

    public int playerScore;

    public Text playerScoreText;

    private float addScoreTime;

    private float currentScore;

    public GameObject gameOverPanel;

    public Text finalScoreText;

    private void Awake()
    {
        _instance = this;
    }

    private void Start()
    {

        //字典实例化
        sweetPrefabDict = new Dictionary<SweetsType, GameObject>();
        for (int i = 0; i < sweetPrefabs.Length; i++)
        {
            if (!sweetPrefabDict.ContainsKey(sweetPrefabs[i].type))
            {
                sweetPrefabDict.Add(sweetPrefabs[i].type, sweetPrefabs[i].prefab);
            }
        }

        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                GameObject chocolate = Instantiate(gridPrefab, CorrectPosition(x, y), Quaternion.identity);
                chocolate.transform.SetParent(transform);
            }
        }

        sweets = new GameSweet[xColumn, yRow];

        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                CreatNewSweet(x, y, SweetsType.EMPTY);
            }
        }

        //饼干的生成
        Destroy(sweets[4, 4].gameObject);
        CreatNewSweet(4, 4, SweetsType.BARRIER);
        Destroy(sweets[5, 4].gameObject);
        CreatNewSweet(5, 4, SweetsType.BARRIER);
        Destroy(sweets[6, 4].gameObject);
        CreatNewSweet(6, 4, SweetsType.BARRIER);
        Destroy(sweets[7, 4].gameObject);
        CreatNewSweet(7, 4, SweetsType.BARRIER);
        Destroy(sweets[2, 2].gameObject);
        CreatNewSweet(2, 2, SweetsType.BARRIER);

        StartCoroutine(AllFill());
    }

    private void Update()
    {
        gameTime -= Time.deltaTime;
        if (gameTime <= 0)
        {
            gameTime = 0;
            //显示失败面板，并播放失败动画
            gameOver = true;
            gameOverPanel.SetActive(true);
            finalScoreText.text = playerScore.ToString();
        }
        timeText.text = gameTime.ToString("0");
        if (addScoreTime<=0.05f)
        {
            addScoreTime += Time.deltaTime;
        }
        else
        {
            if (currentScore < playerScore)
            {
                currentScore++;
                playerScoreText.text = currentScore.ToString();
                addScoreTime = 0;
            }
        }
        
    }

    public Vector3 CorrectPosition(int x, int y)
    {
        //实际需要实例化巧克力块的X位置=GameManager位置的X坐标-大网格长度的一半+行列对应的X坐标
        //实际需要实例化巧克力块的Y位置=GameManager位置的Y坐标+大网格长度的一半-行列对应的Y坐标
        return new Vector3(transform.position.x - xColumn / 2f + x, transform.position.y + yRow / 2f - y);
    }

    //产出甜品的方法
    public GameSweet CreatNewSweet(int x,int y,SweetsType type)
    {
        GameObject newSweet = Instantiate(sweetPrefabDict[type], CorrectPosition(x, y), Quaternion.identity);
        newSweet.transform.parent = transform;

        sweets[x, y] = newSweet.GetComponent<GameSweet>();
        sweets[x, y].Init(x, y, this, type);

        return sweets[x, y];
    }

    //全部填充的方法
    public IEnumerator AllFill()
    {
        bool needRefill = true;
        while (needRefill)
        {
            yield return new WaitForSeconds(fillTime);
            while (Fill())
            {
                yield return new WaitForSeconds(fillTime);
            }

            //清除素有我们已经匹配好的甜品
            needRefill = ClearAllMatchedSweet();
        }
    }

    //分布填充
    public bool Fill()
    {
        //判断本次填充是否完成
        bool filledNoFinished = false;

        for (int y = yRow-2; y >=0; y--)
        {
            for (int x = 0; x < xColumn; x++)
            {
                //得到当前元素的位置的甜品对象
                GameSweet sweet = sweets[x, y];
                //可以移动，则往下填充
                if (sweet.CanMove())    
                {
                    GameSweet sweetBlow = sweets[x, y + 1];

                    //
                    if (sweetBlow.Type == SweetsType.EMPTY)
                    {
                        Destroy(sweetBlow.gameObject);
                        sweet.MovedComponent.Move(x, y + 1,fillTime);
                        sweets[x, y + 1] = sweet;
                        CreatNewSweet(x, y, SweetsType.EMPTY);
                        filledNoFinished = true;
                    }
                    else //斜向填充
                    {
                        for (int down = -1; down <= 1; down++)
                        {
                            if (down != 0)
                            {
                                int downX = x + down;

                                if (downX >= 0 && downX < xColumn)
                                {
                                    GameSweet downSweet = sweets[downX, y + 1];
                                    if (downSweet.Type == SweetsType.EMPTY)
                                    {
                                        //用来判断垂直填充是否可以满足填充要求
                                        bool canFill = true;
                                        for (int aboveY = y; aboveY >= 0; aboveY--)
                                        {
                                            GameSweet sweetAbove = sweets[downX, aboveY];
                                            if (sweetAbove.CanMove())
                                            {
                                                break;
                                            }
                                            else if (!sweetAbove.CanMove() && sweetAbove.Type != SweetsType.EMPTY)
                                            {
                                                canFill = false;
                                                break;
                                            }
                                        }

                                        if (!canFill)
                                        {
                                            Destroy(downSweet.gameObject);
                                            sweet.MovedComponent.Move(downX, y + 1, fillTime);
                                            sweets[downX, y + 1] = sweet;
                                            CreatNewSweet(x, y, SweetsType.EMPTY);

                                            if (sweets[downX, y + 1].Type != SweetsType.EMPTY)
                                            {                                                
                                                if (sweets[downX - 1, y + 1].Type == SweetsType.EMPTY)
                                                {
                                                    Destroy(sweets[downX - 1, y + 1].gameObject);
                                                    sweets[downX, y + 1].MovedComponent.Move(downX - 1, y + 1, fillTime);
                                                    sweets[downX - 1, y + 1] = sweets[downX, y + 1];
                                                    CreatNewSweet(downX, y + 1, SweetsType.EMPTY);
                                                }
                                                if (sweets[downX + 1, y + 1].Type == SweetsType.EMPTY)
                                                {
                                                    Destroy(sweets[downX + 1, y + 1].gameObject);
                                                    sweets[downX, y + 1].MovedComponent.Move(downX + 1, y + 1, fillTime);
                                                    sweets[downX + 1, y + 1] = sweets[downX, y + 1];
                                                    CreatNewSweet(downX, y + 1, SweetsType.EMPTY);
                                                }
                                            }

                                            filledNoFinished = true;
                                            break;
                                        }                                      
                                    }                               
                                }
                            }
                        }
                    }
                }
            }
        }

        //最上排的特殊情况
        for (int x = 0; x < xColumn; x++)
        {
            GameSweet sweet = sweets[x, 0];
            if (sweet.Type == SweetsType.EMPTY)
            {
                GameObject newSweet = Instantiate(sweetPrefabDict[SweetsType.NORMAL], CorrectPosition(x, -1), Quaternion.identity);
                newSweet.transform.parent = transform;

                sweets[x, 0] = newSweet.GetComponent<GameSweet>();
                sweets[x, 0].Init(x, -1, this, SweetsType.NORMAL);
                sweets[x, 0].MovedComponent.Move(x, 0,fillTime);
                sweets[x, 0].ColoredComponent.SetColor((ColorSweet.ColorType)Random.Range(0, sweets[x, 0].ColoredComponent.NumColors));
                filledNoFinished = true;
            }
        }

        return filledNoFinished;
    }

    //判断甜品是否相邻
    private bool IsFriend(GameSweet sweet1,GameSweet sweet2)
    {
        return (sweet1.X == sweet2.X && Mathf.Abs(sweet1.Y - sweet2.Y) == 1) || (sweet1.Y == sweet2.Y && Mathf.Abs(sweet1.X - sweet2.X)==1);
    }

    //交换两个甜品的方法
    private void ExchangeSweets(GameSweet sweet1,GameSweet sweet2)
    {
        if (sweet1.CanMove() && sweet2.CanMove())
        {
            sweets[sweet1.X, sweet1.Y] = sweet2;
            sweets[sweet2.X, sweet2.Y] = sweet1;

            if (MatchSweets(sweet1,sweet2.X,sweet2.Y)!=null||MatchSweets(sweet2,sweet1.X,sweet1.Y)!=null||sweet1.Type==SweetsType.RAINBOWCANDY||sweet2.Type==SweetsType.RAINBOWCANDY)
            {
                int tempX = sweet1.X;
                int tempY = sweet1.Y;

                sweet1.MovedComponent.Move(sweet2.X, sweet2.Y, fillTime);
                sweet2.MovedComponent.Move(tempX, tempY, fillTime);

                if (sweet1.Type == SweetsType.RAINBOWCANDY && sweet1.CanClear() && sweet2.CanClear())
                {
                    ClearColorSweet clearColor = sweet1.GetComponent<ClearColorSweet>();
                    if (clearColor!=null)
                    {
                        clearColor.ClearColor = sweet2.ColoredComponent.Color;
                    }

                    ClearSweet(sweet1.X, sweet1.Y);
                }

                if (sweet2.Type == SweetsType.RAINBOWCANDY && sweet2.CanClear() && sweet1.CanClear())
                {
                    ClearColorSweet clearColor = sweet2.GetComponent<ClearColorSweet>();
                    if (clearColor != null)
                    {
                        clearColor.ClearColor = sweet1.ColoredComponent.Color;
                    }

                    ClearSweet(sweet2.X, sweet2.Y);
                }

                ClearAllMatchedSweet();
                StartCoroutine(AllFill());

                pressedSweet = null;
                enteredSweet = null;
            }
            else
            {
                sweets[sweet1.X, sweet1.Y] = sweet1;
                sweets[sweet2.X, sweet2.Y] = sweet2;
            }
        }
    }

    /// <summary>
    /// 玩家对甜品拖拽的方法
    /// </summary>
    #region 
    public void PressSweet(GameSweet sweet)
    {
        if (gameOver)
        {
            return;
        }
        pressedSweet = sweet;
    }

    public void EnterSweet(GameSweet sweet)
    {
        if (gameOver)
        {
            return;
        }
        enteredSweet = sweet;
    }

    public void ReleaseSweet()
    {
        if (gameOver)
        {
            return;
        }
        if (IsFriend(pressedSweet, enteredSweet))
        {
            ExchangeSweets(pressedSweet, enteredSweet);
        }
    }
    #endregion

    //匹配方法
    public List<GameSweet> MatchSweets(GameSweet sweet,int newX,int newY)
    {
        if (sweet.CanColor())
        {
            ColorSweet.ColorType color = sweet.ColoredComponent.Color;
            List<GameSweet> matchRowSweets = new List<GameSweet>();
            List<GameSweet> matchLineSweets = new List<GameSweet>();
            List<GameSweet> finishedMatchingSweets = new List<GameSweet>();

            //行匹配
            matchRowSweets.Add(sweet);

            //i=0代表往左，i=1代表往右
            for (int i = 0; i <=1; i++)
            {
                for (int xDistance = 1; xDistance < xColumn; xDistance++)
                {
                    int x;
                    if (i == 0)
                    {
                        x = newX - xDistance;
                    }
                    else
                    {
                        x = newX + xDistance;
                    }
                    if (x < 0 || x >= xColumn)
                    {
                        break;
                    }

                    if (sweets[x, newY].CanColor() && sweets[x, newY].ColoredComponent.Color == color)
                    {
                        matchRowSweets.Add(sweets[x, newY]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchRowSweets.Count>=3)
            {
                for (int i = 0; i < matchRowSweets.Count; i++)
                {
                    finishedMatchingSweets.Add(matchRowSweets[i]);
                }
            }

            //L,T型匹配
            //检查一下当前行遍历列表中的元素数量是否大于3
            if (matchRowSweets.Count >= 3)
            {
                for (int i = 0; i < matchRowSweets.Count; i++)
                {
                    //行匹配列表中满足匹配条件的每个元素上下依次进行行列遍历
                    //0代表上方，1下方
                    for (int j = 0; j <=1; j++)
                    {
                        for (int yDistance = 1; yDistance < yRow; yDistance++)
                        {
                            int y;
                            if (j == 0)
                            {
                                y = newY + yDistance;
                            }
                            else
                            {
                                y = newY - yDistance;
                            }
                            if (y < 0 || y >= yRow)
                            {
                                break;
                            }

                            if (sweets[matchRowSweets[i].X, y].CanColor() && sweets[matchRowSweets[i].X, y].ColoredComponent.Color == color)
                            {
                                matchLineSweets.Add(sweets[matchRowSweets[i].X, y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchLineSweets.Count < 2)
                    {
                        matchLineSweets.Clear();
                    }
                    else
                    {
                        for (int j = 0; j < matchLineSweets.Count; j++)
                        {
                            finishedMatchingSweets.Add(matchLineSweets[j]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchingSweets.Count >= 3)
            {
                return finishedMatchingSweets; 
            }

            matchRowSweets.Clear();
            matchLineSweets.Clear();

            //列匹配
            matchLineSweets.Add(sweet);

            //i=0代表往左，i=1代表往右
            for (int i = 0; i <= 1; i++)
            {
                for (int yDistance = 1; yDistance < xColumn; yDistance++)
                {
                    int y;
                    if (i == 0)
                    {
                        y = newY - yDistance;
                    }
                    else
                    {
                        y = newY + yDistance;
                    }
                    if (y < 0 || y >= yRow)
                    {
                        break;
                    }

                    if (sweets[newX, y].CanColor() && sweets[newX, y].ColoredComponent.Color == color)
                    {
                        matchLineSweets.Add(sweets[newX, y]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (matchLineSweets.Count >= 3)
            {
                for (int i = 0; i < matchLineSweets.Count; i++)
                {
                    finishedMatchingSweets.Add(matchLineSweets[i]);
                }
            }

            //L,T型匹配
            //检查一下当前行遍历列表中的元素数量是否大于3
            if (matchLineSweets.Count >= 3)
            {
                for (int i = 0; i < matchLineSweets.Count; i++)
                {
                    //行匹配列表中满足匹配条件的每个元素上下依次进行行列遍历
                    //0代表上方，1下方
                    for (int j = 0; j <= 1; j++)
                    {
                        for (int xDistance = 1; xDistance < xColumn; xDistance++)
                        {
                            int x;
                            if (j == 0)
                            {
                                x = newY - xDistance;
                            }
                            else
                            {
                                x = newY + xDistance;
                            }
                            if (x < 0 || x >= xColumn)
                            {
                                break;
                            }

                            if (sweets[x,matchLineSweets[i].Y].CanColor() && sweets[x, matchLineSweets[i].Y].ColoredComponent.Color == color)
                            {
                                matchRowSweets.Add(sweets[x, matchLineSweets[i].Y]);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    if (matchRowSweets.Count < 2)
                    {
                        matchRowSweets.Clear();
                    }
                    else
                    {
                        for (int j = 0; j < matchRowSweets.Count; j++)
                        {
                            finishedMatchingSweets.Add(matchRowSweets[j]);
                        }
                        break;
                    }
                }
            }

            if (finishedMatchingSweets.Count >= 3)
            {
                return finishedMatchingSweets;
            }


        }

        return null;
    }

    //清除方法
    public bool ClearSweet(int x,int y)
    {
        if (sweets[x, y].CanClear()&&!sweets[x,y].ClearedComponent.IsClearing)
        {
            sweets[x, y].ClearedComponent.Clear();
            CreatNewSweet(x, y, SweetsType.EMPTY);

            ClearBiscuits(x, y);

            return true;
        }

        return false;
    }

    /// <summary>
    /// 清除饼干
    /// </summary>
    /// <param name="x">被消除甜品对象的x坐标</param>
    /// <param name="y">被消除甜品对象的y坐标</param>
    private void ClearBiscuits(int x,int y)
    {
        for (int friendX = x-1; friendX < x+1; friendX++)
        {
            if (friendX != x && friendX >= 0 && friendX < xColumn)
            {
                if (sweets[friendX, y].Type == SweetsType.BARRIER && sweets[friendX, y].CanClear())
                {
                    sweets[friendX, y].ClearedComponent.Clear();
                    CreatNewSweet(friendX, y, SweetsType.EMPTY);
                }
            }
        }

        for (int friendY = y - 1; friendY < y + 1; friendY++)
        {
            if (friendY != y && friendY >=0 && friendY < yRow)
            {
                if (sweets[x,friendY].Type == SweetsType.BARRIER && sweets[x, friendY].CanClear())
                {
                    sweets[x, friendY].ClearedComponent.Clear();
                    CreatNewSweet(x, friendY, SweetsType.EMPTY);
                }
            }
        }
    }

    //清除全部完全匹配的甜品
    private bool ClearAllMatchedSweet()
    {
        bool needRefill = false;

        for (int y = 0; y < yRow; y++)
        {
            for (int x = 0; x < xColumn; x++)
            {
                if (sweets[x, y].CanClear())
                {
                    List<GameSweet> matchList = MatchSweets(sweets[x, y], x, y);

                    if (matchList != null)
                    {
                        //我们是否产生特殊甜品
                        SweetsType specialSweetsType = SweetsType.COUNT;

                        GameSweet randowSweet = matchList[Random.Range(0, matchList.Count)];
                        int specialSweetX = randowSweet.X;
                        int specialSweetY = randowSweet.Y;

                        if (matchList.Count == 4)
                        {
                            specialSweetsType = (SweetsType)Random.Range((int)SweetsType.ROW_CLEAR, (int)SweetsType.COLUMN_CLEAR);
                        }
                        //5个的话产生彩虹糖
                        else if (matchList.Count >= 5)
                        {
                            specialSweetsType = SweetsType.RAINBOWCANDY;
                        }


                        for (int i = 0; i < matchList.Count; i++)
                        {
                            if (ClearSweet(matchList[i].X, matchList[i].Y))
                            {
                                needRefill = true;
                            }
                        }

                        if (specialSweetsType != SweetsType.COUNT)
                        {
                            Destroy(sweets[specialSweetX, specialSweetY]);
                            GameSweet newSweet = CreatNewSweet(specialSweetX, specialSweetY, specialSweetsType);
                            if (specialSweetsType == SweetsType.ROW_CLEAR||specialSweetsType==SweetsType.COLUMN_CLEAR&&newSweet.CanColor()&&matchList[0].CanColor())
                            {
                                newSweet.ColoredComponent.SetColor(matchList[0].ColoredComponent.Color);
                            }
                            //加上彩虹糖的特殊类型的产生
                            else if (specialSweetsType == SweetsType.RAINBOWCANDY && newSweet.CanColor())
                            {
                                newSweet.ColoredComponent.SetColor(ColorSweet.ColorType.ANY);
                            }
                        }
                    }
                }
            }
        }

        return needRefill;
    }

    public void ReturnToMain()
    {
        SceneManager.LoadScene(0);
    }

    public void Replay()
    {
        SceneManager.LoadScene(1);
    }

    //消除行的方法
    public void ClearRow(int row)
    {
        for (int x = 0; x < xColumn; x++)
        {
            ClearSweet(x, row);
        }
    }
    //清除列的方法
    public void ClearColumn(int column)
    {
        for (int y = 0; y < yRow; y++)
        {
            ClearSweet(column, y);
        }
    }

    //清除颜色的方法
    public void ClearColor(ColorSweet.ColorType color)
    {
        for (int x = 0; x < xColumn; x++)
        {
            for (int y = 0; y < yRow; y++)
            {
                if (sweets[x, y].CanColor() && (sweets[x, y].ColoredComponent.Color == color||color==ColorSweet.ColorType.ANY))
                {
                    ClearSweet(x, y);
                }
            }
        }
    }
}
