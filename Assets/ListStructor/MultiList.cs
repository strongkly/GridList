using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class TestDatas : IMultipleChoice {
    public int choice {
        get;
        private set;
    }

    public int dataIndex {
        get;
        private set;
    }

    public TestDatas(int choice, int dataIndex) {
        this.choice = choice;
        this.dataIndex = dataIndex;
    }

    public int GetChoice() {
        return choice;
    }
}

public class ViewBase : MonoBehaviour {
    public virtual void FillView(object data) {

    }
}

public class TestListItem : ViewBase {
    Text text;
    public TestDatas data {
        private set;
        get;
    }

    public void Awake() {
        text = transform.GetChild(0).GetComponent<Text>();
    }

    public override void FillView(object data) {
        this.data = data as TestDatas;
        text.text = string.Format("s:{0}  i:{1}", this.data.choice, this.data.dataIndex);
        gameObject.name = this.data.dataIndex.ToString();
    }
}

/// <summary>
/// TODO: 创建销毁太过频繁，需要优化
/// TODO: 列表项目大小不应该完全由RectTransform.Rect 来控制
/// </summary>
public class MultiList : MonoBehaviour {
    #region coorperating components
    ScrollRect scrollRect;
    MultiGrid multiGrid;

    public MultiGrid grid {
        get {
            return multiGrid;
        }
        private set {
            this.multiGrid = value;
        }
    }
    #endregion

    #region inspector fields
    [SerializeField]
    protected List<GameObject> templatesObjects;
    #endregion

    #region data related fields
    public IList datas {
        get;
        private set;
    }
    public int startIndex {
        get;
        private set;
    }
    public int endIndex {
        get;
        private set;
    }
    #endregion

    #region list object manage related
    List<MultiUnitGroup> groups = new List<MultiUnitGroup>();
    public List<MultiUnitGroup> Groups {
        get {
            return groups;
        }
    }
    public MultiUnitGroup firstGroup {
        get {
            return groups[0];
        }
    }
    public MultiUnitGroup currentGroup {
        get {
            return groups[groups.Count - 1];
        }
    }
    #endregion

    #region interfaces for convenience
    public bool isHorizontalFirst {
        get {
            return grid.isHorizontalFirst;
        }
    }
    public Vector2 padding {
        get {
            return grid.Padding;
        }
    }
    public RectTransform content {
        get {
            return grid.transform as RectTransform;
        }
    }
    public bool hasGroups {
        get {
            return groups.Count > 0;
        }
    }
    /// <summary>
    /// 显示区域的起始位置
    /// </summary>
    public Vector3 contentStartPos {
        get {
            return grid.ContentPos;
        }
    }
    public Rect bound {
        get {
            return grid.bound;
        }
    }
    #endregion

    #region for optimistic
    Vector2 dragDisDelta;
    Vector2 lastDragDis;

    Type itemViewType;
    #endregion

    public void Start() {
        OnStart();
    }

    #region initialization
    public void OnStart() {
        HideAllTemplatesObjects();
        GetAllCooperatingComponent();

        List<TestDatas> datas = new List<TestDatas>();
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));
        datas.Add(new TestDatas(0, datas.Count));
        datas.Add(new TestDatas(1, datas.Count));

        CreateMultiList(datas, typeof(TestListItem));
    }

    void GetAllCooperatingComponent() {
        scrollRect = GetComponent<ScrollRect>();
        multiGrid = scrollRect.content.GetComponent<MultiGrid>();
    }

    void HideAllTemplatesObjects() {
        for (int i = 0; i < templatesObjects.Count; i++)
            templatesObjects[i].SetActive(false);
    }
    #endregion

    #region interfaces for convenience
    public Rect GetTemplateRectByDataIndex(int dataIndex) {
        return (GetTemplateObjectByChoiceIdx((datas[dataIndex] as IMultipleChoice).GetChoice())
                .transform as RectTransform).rect;
    }
    public GameObject GetTemplateObjectByChoiceIdx(int choice) {
        return templatesObjects[choice];
    }
    #endregion

    protected void CreateMultiList(IList datas, Type itemViewType) {
        this.datas = datas;
        this.itemViewType = itemViewType;
        this.scrollRect.onValueChanged.AddListener(OnDrag);
        //创建列表时，由于可能内容位置不是0,0，因此绘制位置需要调整
        grid.SetStartPlacePos(contentStartPos);
        for (startIndex = 0, endIndex = -1; endIndex + 1 < datas.Count;) {
            //if (!hasGroups || !currentGroup.TryAddUnit(GetNextDataTemplateObjRec(), endIndex + 1)) {
                if (!TryAddGroup())
                    return;
            //}
            //else {
                //endIndex++;
            //}
        }
    }

    protected virtual void OnDrag(Vector2 delta) {
        dragDisDelta.x = content.rect.width * delta.x;
        dragDisDelta.y = content.rect.height * delta.y;

        TryDeleteAllValidInvisibleItem(dragDisDelta);
        //TryAddAllValidItem(dragDisDelta);
        UpdateAllGroupLastDragDis(dragDisDelta);
        TryAddGroupWhenDrag(dragDisDelta);
    }

    void TryAddGroupWhenDrag(Vector2 dragDisDelta) {
        if (isHorizontalFirst) {
            if (IsTopDrag(dragDisDelta)) { //向上拖拽时，可能在后（底）部显示新组
                while (true)
                    if (!TryAddGroup()) break;
            }
            else {
                while (true) //向下拖拽时，可能在前（顶）部显示新租
                    if (!TryAddGroup(false)) break;
            }
        }
        else {
            if (IsLeftDrag(dragDisDelta)) { //向左拖拽时，可能在后（右）部显示新组
                while (true)
                    if (!TryAddGroup()) break;
            }
            else { //向右拖拽时，可能在前（左）部显示新组
                while (true)
                    if (!TryAddGroup(false)) break;
            }
        }
        lastDragDis.Set(dragDisDelta.x, dragDisDelta.y);
    }

    bool IsTopDrag(Vector2 dragDisDelta) {
        return lastDragDis.y - dragDisDelta.y > 0;
    }

    bool IsLeftDrag(Vector2 dragDisDelta) {
        return lastDragDis.x - dragDisDelta.x < 0;
    }

    void UpdateAllGroupLastDragDis(Vector2 deltaDis) {
        for (int i = 0; i < groups.Count; i++)
            groups[i].SetLastDragDis(deltaDis);
    }

    void TryAddAllValidItem(Vector2 deltaDis) {
        while (true) {
            if (!TryAddItemToBoundary(deltaDis)) return;
            grid.RepositionAllChildPanel();
        }
    }

    bool TryAddItemToBoundary(Vector2 deltaDis) {
        for (int i = 0, offset = 0; i < groups.Count; i++) {
            offset = groups[i].TryAppendWhenDrag(deltaDis);
            if (offset == 0)
                continue;
            else {//尝试添加当前数据成功
                if (offset > 0)
                    endIndex += offset;
                else
                    startIndex += offset;
                return true;
            }
        }
        return false;
    }

    void TryDeleteAllValidInvisibleItem(Vector2 deltaDis) {
        while (true) {
            if (!TryDeleteBoundaryItem(deltaDis)) return;
        }
    }

    bool TryDeleteBoundaryItem(Vector2 deltaDis) {
        for (int i = 0, offset = 0; i < groups.Count; i++) {
            offset = groups[i].TryDeleteWhenDrag(deltaDis);
            if (offset == 0)
                continue;
            else {//成功删除当前unit
                if (offset > 0)
                    startIndex += offset;
                else
                    endIndex += offset;
                //如果当前group 已经没有unit，删除当前group
                if (!groups[i].hasUnits) {
                    groups.Remove(groups[i]);
                    //Debug.LogError(string.Format("group[{0}] is now deleting...current group count:{1}", i, groups.Count));
                }
                return true;
            }
        }
        return false;
    }

    bool TryAddGroup(bool isAddFormEnd = true){
        if (!IsNewGroupVisible(isAddFormEnd))
            return false;
        if (isAddFormEnd && IsShowDataReachEnd())
            return false;
        if (!isAddFormEnd && IsShowDataReachTop())
            return false;
        AddNewGroup(isAddFormEnd);
        //Debug.LogError(string.Format("AddGroupAlready,Current Group Counts: {0}，endIndex:{1}, startIndex:{2}",
        //    groups.Count, endIndex, startIndex));
        return true;
    }

    bool IsNewGroupVisible(bool isAddFromEnd = true) {
        if (!hasGroups) return true;
        if (isHorizontalFirst) {//不能使用currentGroup 因为前后都有可能增加，而currentGroup 只能记录上一次的group 会导致错误
            if (isAddFromEnd)
                return currentGroup.startPos.y - currentGroup.curBioDirMax - padding.y >
                    contentStartPos.y - bound.height;
            else
                return currentGroup.startPos.y + padding.y < contentStartPos.y;
        }
        else {
            if (isAddFromEnd)
                return currentGroup.startPos.x + currentGroup.curBioDirMax + padding.x <
                    contentStartPos.x + bound.width;
            else {
                return firstGroup.startPos.x - padding.x > contentStartPos.x;
            }
        }
    }

    bool IsShowDataReachTop() {
        return startIndex == 0;
    }

    bool IsShowDataReachEnd() {
        return endIndex == datas.Count - 1;
    }

    void AddNewGroupToEnd(MultiUnitGroup newGroup) {
        int i = endIndex + 1;
        while (true) {
            if (newGroup.TryAddUnit(GetIndexDataTemplateObjRec(i), i, true)) {
                endIndex++;
                if (IsShowDataReachEnd())
                    break;
                i++;
            }
            else
                break;
        }
    }

    void AddNewGroupToHead(MultiUnitGroup newGroup) {
        int i = startIndex - 1;
        while (true) {
            if (newGroup.TryAddUnit(GetIndexDataTemplateObjRec(i), i, false)) {
                startIndex--;
                if (IsShowDataReachTop())
                    break;
                i--;
            }
            else
                break;
        }
    }

    void AddNewGroup(bool isAddFromEnd = true){
        MultiUnitGroup newGroup = new MultiUnitGroup(this, isAddFromEnd ? 
            endIndex + 1 - startIndex : 0, isAddFromEnd);
        Vector3 newDrawPos;
        newDrawPos = GetNewStartPosWhenAddGroup(newGroup, !isAddFromEnd);
        newGroup.SetNewGroupStartPos(newDrawPos);
        if (isAddFromEnd)
            AddNewGroupToEnd(newGroup);
        else
            AddNewGroupToHead(newGroup);

        if (isAddFromEnd)
            groups.Add(newGroup);
        else {
            //添加至顶部时，需要重新设置grid绘制的起始位置
            grid.SetStartPlacePos(newGroup.startPos);
            groups.Insert(0, newGroup);
        }
        //增加一个组后，立即重排该组
        grid.RepositionChildsWithStartPos(newGroup.startPos, newGroup.startSibIndex, 
            newGroup.startSibIndex + newGroup.unitCount - 1);
    }

    Vector3 GetNewStartPosWhenAddGroup(MultiUnitGroup newGroup, bool isAddFromHead = false) {
        if (!hasGroups)
            return -content.localPosition;
        else {
            if (isAddFromHead) {
                //在头部新增组时，由于第二方向上最大长度未知，因此仅保证第一轴向位置，重新绘制之前，还需要修正第二轴向
                //在头部新增组时，需要反向计算该组最大能容纳往前多少个数据，从而计算出startPos
                return GetRealNewGroupStartPosWhenAddToHead();
            }
            else {
                if (isHorizontalFirst)
                    return new Vector3(GetLeftTopMostUnitGroup().startPos.x,
                        currentGroup.startPos.y - padding.y - currentGroup.curBioDirMax, 0);
                else
                    return new Vector3(currentGroup.startPos.x + padding.x + currentGroup.curBioDirMax,
                        GetLeftTopMostUnitGroup().startPos.y, 0);
            }
        }
    }

    Vector3 GetRealNewGroupStartPosWhenAddToHead() {
        int realstart = GetRealNewGroupStarIndexWhenAddToHead();
        float maxBio = 0;
        Vector3 startPos = GetLeftTopMostUnitGroup().startPos;
        Rect curRect;
        for (int i = realstart; i < startIndex; i++) {
            curRect = GetTemplateRectByDataIndex(i);
            if (isHorizontalFirst) {
                startPos.x += (curRect.width + padding.x);
                if (curRect.height > maxBio)
                    maxBio = curRect.height;
            }
            else {
                startPos.y -= (GetTemplateRectByDataIndex(i).height + padding.y);
                if (curRect.width > maxBio)
                    maxBio = curRect.width;
            }
        }
        if (isHorizontalFirst) {
            startPos.y += (padding.y + maxBio);
        }
        else {
            startPos.x -= (padding.x + maxBio);
        }
        return startPos;
    }

    int GetRealNewGroupStarIndexWhenAddToHead() {
        int full = GetMaxFullyContainStartIndex();
        int result = full;
        if (full <= 0) return result;
        Rect rect = GetTemplateRectByDataIndex(startIndex - 1);
        float lastRectLen = isHorizontalFirst ? rect.width : rect.height;
        while (true) {
            if (result - 1 < 0)
                return result;
            rect = GetTemplateRectByDataIndex(result - 1);
            lastRectLen -= isHorizontalFirst ? rect.width : rect.height;
            if (lastRectLen <= 0)
                return result - 1;
            result--;
        }
    }

    int GetMaxFullyContainStartIndex() {
        int result = startIndex;
        float viewPortLen = isHorizontalFirst ? bound.width : bound.height;
        Rect rect;
        while (true) {
            if (result - 1 < 0)
                return result;
            rect = GetTemplateRectByDataIndex(result - 1);
            viewPortLen = isHorizontalFirst ? viewPortLen - rect.width : viewPortLen - rect.height;
            if (viewPortLen <= 0)
                return result - 1;
            result--;
        }
    }

    MultiUnitGroup GetLeftTopMostUnitGroup() {
        if (!hasGroups) return null;
        MultiUnitGroup result = groups[0];
        float LeftTopMost = isHorizontalFirst ? groups[0].startPos.x : groups[0].startPos.y;
        for (int i = 1; i < groups.Count; i++) {
            if (isHorizontalFirst) {
                if (groups[i].startPos.x < result.startPos.x) {
                    result = groups[i];
                    LeftTopMost = groups[i].startPos.x;
                }
            }
            else {
                if (groups[i].startPos.y > result.startPos.y) {
                    result = groups[i];
                    LeftTopMost = groups[i].startPos.y;
                }
            }
        }
        return result;
    }

    public virtual GameObject GetNextDataTemplateObjRec(){
        return GetIndexDataTemplateObjRec(endIndex + 1);
    }

    public virtual GameObject GetPreDataTemplateObjRec(){
        return GetIndexDataTemplateObjRec(startIndex - 1);
    }

    public virtual GameObject GetIndexDataTemplateObjRec(int index){
        GameObject result = null;
        if (index < datas.Count && index >= 0) {
            result = GetListItemObjectByChoiceIdx((datas[index] as IMultipleChoice).GetChoice());
            result.gameObject.AddComponent(itemViewType);
        }
        return result;
    }

    public virtual GameObject GetListItemObjectByChoiceIdx(int idx = 0) {
        return Instantiate(templatesObjects[idx]);
    }

    public virtual void RemoveTemplateObject(GameObject tempObj) {
        GameObject.Destroy(tempObj);
    }
}

public class MultiUnitGroup{
    List<MultiUnit> units = new List<MultiUnit>();

    MultiList multiList;

    #region 方便接口
    Rect bound {
        get {
            return multiList.bound;
        }
    }

    RectTransform content {
        get {
            return multiList.content;
        }
    }

    Vector2 padding {
        get {
            return multiList.padding;
        }
    }

    bool isHorizontalFirst {
        get {
            return multiList.isHorizontalFirst;
        }
    }

    Vector2 contentStartPos {
        get {
            return multiList.contentStartPos;
        }
    }

    public RectTransform lastUnitTrans{
        get {
            if (!hasUnits || units.Count == 0)
                return null;
            return units[units.Count - 1].rectTrans;
        }
    }

    public bool hasUnits {
        get {
            return unitCount > 0;
        }
    }

    public int unitCount {
        get {
            return units.Count;
        }
    }

    public int startSibIndex {
        private set;
        get;
    }
    #endregion

    public float curBioDirMax {
        private set;
        get;
    }

    Vector3 startPosition;
    public Vector2 lastDragDis = Vector2.zero;
    public Vector3 startPos {
        private set {
            this.startPosition = value;
        }
        get {
            return startPosition;
        }
    }

    public MultiUnitGroup(MultiList multiList, int startSibIndex, bool isAddFromEnd = true) {
        this.multiList = multiList;
        this.startSibIndex = startSibIndex;
    }

    public void SetNewGroupStartPos(Vector3 startPos) {
        this.startPos = startPos;
    }

    public int TryAppendWhenDrag(Vector2 dragDis) {
        int isAppendSuccess = 0;
        if (IsLeftDrag(dragDis) || IsTopDrag(dragDis)) {
            //可能需要在后部增加unit，显示后续记录;
            isAppendSuccess = TryAppendUnit() ? 1 : 0;
        }
        else {
            //可能需要在前部增加unit, 显示前续记录;
            isAppendSuccess = TryAppendUnit(false) ? -1 : 0;
        }
        //lastDragDis.Set(dragDis.x, dragDis.y);
        return isAppendSuccess;
    }

    public int TryDeleteWhenDrag(Vector2 dragDis) {
        int isDeleteSuccess = 0;
        //删除数据时，必须严格从数据两头（startIndex, endIndex）开始删除
        if (IsLeftDrag(dragDis) || IsTopDrag(dragDis)) {
            if (units[0].dataIndex == multiList.startIndex)
                isDeleteSuccess = TryRemoveUnit() ? 1 : 0;
        }
        else{
            if (units[units.Count - 1].dataIndex == multiList.endIndex)
                isDeleteSuccess = TryRemoveUnit(false) ? -1 : 0;
        }
        return isDeleteSuccess;
    }

    bool IsLeftDrag(Vector2 dragDis) {
        return dragDis.x - lastDragDis.x > 0;
    }

    bool IsTopDrag(Vector2 dragDis) {
        return dragDis.y - lastDragDis.y < 0;
    }

    bool TryAppendUnit(bool isAddFromEnd = true) {
        GameObject obj = isAddFromEnd ? multiList.GetNextDataTemplateObjRec():
            multiList.GetPreDataTemplateObjRec();
        if (obj == null) return false;
        RectTransform trans = obj.transform as RectTransform;
        ///追加unit 时，需要判断当前该组是否可以容纳当前的unit
        if (IsNewTransBioBiggerThanMax(trans)) {
            FailAddNewUnit(obj);
            return false;
        }
        return TryAddUnit(obj, isAddFromEnd ? multiList.endIndex + 1 :
            multiList.startIndex - 1, isAddFromEnd);
    }

    bool TryRemoveUnit(bool isDeleteFromHead = true) {
        MultiUnit unit = isDeleteFromHead ? units[0] : units[units.Count - 1];
        if (IsUnitVisible(unit))
            return false;
        DestroyAndRemoveUnit(unit);
        return true;
    }

    public bool TryAddUnit(GameObject obj, int dataIndex, bool isAddFromEnd = true) {
        if (!IsNewUnitVisible(isAddFromEnd)) {
            FailAddNewUnit(obj);
            return false;
        }
        FillItemView(CreateAndAddUnit(obj, isAddFromEnd), dataIndex);
        return true;
    }

    /// <summary>
    /// 由于不能预知组的第二方向最大显示长度，需要在增加完毕后，修正绘制的起始位置
    /// </summary>
    public void FixStartPosWithCurrentBioMax() {
        if (isHorizontalFirst)
            startPosition.y += curBioDirMax;
        else
            startPosition.x -= curBioDirMax;
    }

    bool IsUnitVisible(MultiUnit unit) {
        if (unit.rectTrans.localPosition.x + unit.width < contentStartPos.x)//左侧隐藏
            return false;
        else if (unit.rectTrans.localPosition.x > contentStartPos.x + bound.width)//右侧隐藏
            return false;
        else if (unit.rectTrans.localPosition.y - unit.height > contentStartPos.y)//上方隐藏
            return false;
        else if (unit.rectTrans.localPosition.y < contentStartPos.y - bound.height)//下方隐藏
            return false;
        else
            return true;
    }

    /// 判断新增Unit 是否可见
    bool IsNewUnitVisible(bool isAddFromEnd = true) {
        if (!hasUnits) return true; //第一个Item 均视为可见
        //TODO: 改进或废除 firstUnitPos 等接口。由于组内增加和删除Unit 时不会立即更新绘制位置，所以不能简单使用firstUnitPos 等接口
        if (isHorizontalFirst) {
            if (isAddFromEnd) {
                return GetLastUnitPosByStartPos().x + lastUnitTrans.rect.width + padding.x < 
                    contentStartPos.x + bound.width;
            }
            else {
                return startPos.x - padding.x > contentStartPos.x;
            }
        }
        else {
            if (isAddFromEnd) {
                return GetLastUnitPosByStartPos().y - lastUnitTrans.rect.height - padding.y > 
                    contentStartPos.y - bound.height;
            }
            else {
                return startPos.y + padding.y < contentStartPos.y;
            }
        }
    }

    public Vector3 GetLastUnitPosByStartPos() {
        Vector3 result = new Vector3(startPos.x, startPos.y, startPos.z);
        for (int i = 0; i < units.Count - 1; i++) {
            if (isHorizontalFirst)
                result.x += units[i].width;
            else
                result.y -= units[i].height;
        }
        if (isHorizontalFirst)
            result.x = result.x + padding.x * (units.Count - 1);
        else
            result.y = result.y - padding.y * (units.Count - 1);
        return result;
    }

    void FailAddNewUnit(GameObject obj) {
        multiList.RemoveTemplateObject(obj);
    }

    bool IsNewTransBioBiggerThanMax(RectTransform trans) {
        if (isHorizontalFirst)
            return trans.rect.height > curBioDirMax;
        else
            return trans.rect.width > curBioDirMax;
    }

    MultiUnit CreateAndAddUnit(GameObject obj, bool isAddFromEnd = true) {
        MultiUnit result = new MultiUnit(obj);
        CreateUnit(obj, isAddFromEnd);

        if (isAddFromEnd)
            units.Add(result);
        else {
            units.Insert(0, result);
            UpdateStartPosWhenAddUnitFromHead(result);
        }
        UpdateCurrentBioDirectionMaxLength(result.rectTrans);

        return result;
    }

    void UpdateStartPosWhenAddUnitFromHead(MultiUnit unit) {
        if (isHorizontalFirst)
            startPosition.x = startPosition.x - padding.x - unit.width;
        else
            startPosition.y = startPosition.y + padding.y + unit.height;
    }

    void DestroyAndRemoveUnit(MultiUnit unit, bool isRemoveFromHead = true) {
        units.Remove(unit);

        if (isRemoveFromHead)
            UpdateStartPosWhenRemoveUnitFromHead(unit);

        DestroyUnit(unit.gameObject);
    }

    void UpdateStartPosWhenRemoveUnitFromHead(MultiUnit unit) {
        if (isHorizontalFirst)
            startPosition.x = startPosition.x + unit.width + padding.x;
        else
            startPosition.y = startPosition.y - unit.height - padding.y; 
    }

    void CreateUnit(GameObject obj, bool isAddFromEnd = true) {
        RectTransform trans = obj.transform as RectTransform;
        trans.SetParent(content, false);
        if (isAddFromEnd)
            trans.SetSiblingIndex(startSibIndex + units.Count);
        else
            trans.SetSiblingIndex(startSibIndex);
        trans.localScale = Vector3.one;
        obj.SetActive(true);
    }

    void DestroyUnit(GameObject obj) {
        multiList.RemoveTemplateObject(obj);
    }

    void FillItemView(MultiUnit unit, int dataIndex){
        unit.SetDataIndex(dataIndex);
        ViewBase view = unit.gameObject.GetComponent<ViewBase>();
        view.FillView(multiList.datas[dataIndex]);
    }

    void UpdateCurrentBioDirectionMaxLength(RectTransform trans) {
        if (isHorizontalFirst) {
            if (trans.rect.height > curBioDirMax)
                curBioDirMax = trans.rect.height;
        }
        else {
            if (trans.rect.width > curBioDirMax)
                curBioDirMax = trans.rect.width;
        }
    }

    public void SetLastDragDis(Vector2 lastDragDis) {
        this.lastDragDis.Set(lastDragDis.x, lastDragDis.y);
    }
}

public class MultiUnit {
    public GameObject gameObject{
        private set;
        get;
    }

    #region for convinence
    public RectTransform rectTrans {
        get {
            return gameObject.transform as RectTransform;
        }
    }

    public float width {
        get {
            return rectTrans.rect.width;
        }
    }

    public float height {
        get {
            return rectTrans.rect.height;
        }
    }

    public int dataIndex {
        private set;
        get;
    }
    #endregion

    public MultiUnit(GameObject obj) {
        this.gameObject = obj;
    }

    public void SetDataIndex(int dataIndex) {
        this.dataIndex = dataIndex;
    }
}
