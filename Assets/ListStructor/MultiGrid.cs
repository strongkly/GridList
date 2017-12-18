using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用于更新子项目的摆放
/// 优先排列方向：子项目优先摆放的方向
/// 第二方向：当优先排列方向超过了边缘，那么会在第二方向上另起一行（或列）继续摆放
/// 组：在优先排列方向上，每一行（或列）所有子项目形成一组
/// </summary>
public class MultiGrid : MonoBehaviour 
{
    //是否优先横向排列
    public bool isHorizontalFirst = true;
    [SerializeField]
    protected Vector2 padding = Vector2.zero;//for hierachy,do not use,use Padding instead
    public Vector2 Padding {
        get {
            return padding;
        }
        protected set {
            padding = value;
        }
    }
    [SerializeField]
    protected RectTransform boundRectTransform;
    public Rect bound {
        get {
            return boundRectTransform.rect;
        }
    }

    Vector2 startPlacePos = Vector2.zero, nowPlacePos = Vector2.zero;
    float curBioDirMax;

    #region 方便接口
    public Vector3 ContentPos {
        get {
            return -transform.localPosition;
        }
    }
    #endregion

    public void Start() {
        OnStart();
    }

    #region 初始化逻辑
    void OnStart() {
        InitFields();
        RepositionAllChildPanel();
    }
    void InitFields() {
        if (boundRectTransform == null)
            boundRectTransform = transform.parent as RectTransform;
    }
    #endregion

    #region 重置逻辑
    /// <summary>
    /// 仅当显示(即activeInHierachy = true)时调用有效
    /// 子项目隐藏时不会被更新位置
    /// </summary>
    public void RepositionAllChildPanel() 
    {
        nowPlacePos = GetRepositionStartPos();
        //由于每个子项目的大小可能不同，每放置一个项目，均需判断当前第二方向在另起一行（列）时的起始位置
        for (int i = 0; i < transform.childCount; i++) 
            RepositionChildPanel(i);
    }

    public void RepositionChildPanel(int idx) {
        RectTransform curChild = null;
        curChild = transform.GetChild(idx) as RectTransform;
        if (!curChild.gameObject.activeInHierarchy) return;

        curChild.localPosition = nowPlacePos;

        TestDatas data = null;
        if (curChild.GetComponent<TestListItem>() != null) {
            data = curChild.GetComponent<TestListItem>().data;
            //Debug.LogError(string.Format("child:{0} localPos:{1} dataIndex:{2}",
            //    idx, nowPlacePos, data.dataIndex));
        }
        ApplyPaddingToNowPos();

        //检查优先排列的方向，放置当前子项目后是否会超过边缘，超过边缘则开始在第二方向上摆放
        if (IsOutOfFirstDirectionBound(curChild))
            StartNewGroup();
        else
            SetNowPosAfterPlaceCurChild(curChild);
    }

    public void RepositionChildsWithStartPos(Vector2 starPos, int startSib, int endSib) {
        //Debug.LogError(string.Format("startSib:{0},endSib:{1}", startSib, endSib));
        nowPlacePos = starPos;
        for (int i = startSib; i <= endSib; i++)
            RepositionChildPanel(i);
    }

    public void SetStartPlacePos(Vector2 pos) {
        startPlacePos = pos;
    }

    protected Vector2 GetRepositionStartPos() {
        return startPlacePos;
    }

    bool IsOutOfFirstDirectionBound(RectTransform curChild) {
        if (isHorizontalFirst)
            return nowPlacePos.x + curChild.rect.width > ContentPos.x + bound.width;
        else
            return nowPlacePos.y - curChild.rect.height < ContentPos.y - bound.height;
    }

    void StartNewGroup() {
        if (isHorizontalFirst) {
            nowPlacePos.y -= GetCurBioDirOffset();
            nowPlacePos.x = startPlacePos.x;
        }
        else {
            nowPlacePos.x += GetCurBioDirOffset();
            nowPlacePos.y = startPlacePos.y;
        }
        curBioDirMax = 0;
    }

    float GetCurBioDirOffset() {
        return curBioDirMax + (isHorizontalFirst ? Padding.y : Padding.x);
    }

    void SetNowPosAfterPlaceCurChild(RectTransform curChild) {
        if (isHorizontalFirst) {
            nowPlacePos.x += curChild.rect.width;
            if (curBioDirMax < curChild.rect.height)
                curBioDirMax = curChild.rect.height;
        }
        else {
            nowPlacePos.y -= curChild.rect.height;
            if (curBioDirMax < curChild.rect.width)
                curBioDirMax = curChild.rect.width;
        }
    }

    void ApplyPaddingToNowPos() {
        if (isHorizontalFirst)
            nowPlacePos.x += padding.x;
        else
            nowPlacePos.y -= padding.y;
    }
    #endregion
}
