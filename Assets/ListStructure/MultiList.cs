using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

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

    protected void CreateMultiList(IList datas, Type itemViewType) {
        this.datas = datas;
        this.itemViewType = itemViewType;
        this.scrollRect.onValueChanged.AddListener(OnDrag);
        //set the start pos of grid to the start pos of content at beginning,
        //in order to get whole view of initial list
        grid.SetStartPlacePos(contentStartPos);
        for (startIndex = 0, endIndex = -1; endIndex + 1 < datas.Count;)
            if (!TryAddGroup())
                return;
    }

    protected virtual void OnDrag(Vector2 delta) {
        dragDisDelta.x = content.rect.width * delta.x;
        dragDisDelta.y = content.rect.height * delta.y;

        TryDeleteAllValidInvisibleItem(dragDisDelta);
        TryAddAllValidItem(dragDisDelta);
        UpdateAllGroupLastDragDis(dragDisDelta);
        //TryAddGroupWhenDrag(dragDisDelta);
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
        }
    }

    bool TryAddItemToBoundary(Vector2 deltaDis) {
        for (int i = 0, offset = 0; i < groups.Count; i++) {
            offset = groups[i].TryAppendWhenDrag(deltaDis);
            if (offset == 0)
                continue;
            else {
                if (offset > 0)
                    endIndex += offset;
                else
                    startIndex += offset;
                ResetGroupStartSiblingIndex(i, offset);
                groups[i].RepositionAllUnit();
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
            else {//delete current unit successfully
                if (offset > 0)
                    startIndex += offset;
                else
                    endIndex += offset;
                ResetGroupStartSiblingIndex(i, offset, false);
                //delete current group if it contains no units
                if (!groups[i].hasUnits) {
                    groups.Remove(groups[i]);
                    //Debug.LogError(string.Format("group[{0}] is now deleting...current group count:{1}", i, groups.Count));
                }
                return true;
            }
        }
        return false;
    }

    void ResetGroupStartSiblingIndex(int groupIndex, int offset, bool isAdd = true) {
        if (offset == 0) return;
        offset = Math.Abs(offset);
        for (int i = groupIndex + 1; i < groups.Count; i++) {
            if (isAdd)
                groups[i].SetStartSiblingIndex(groups[i].startSibIndex + offset);
            else
                groups[i].SetStartSiblingIndex(groups[i].startSibIndex - offset);
        }
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
        //do not use currentGroup, cause item can be both added from head or end of datas,
        //however currentGroup only record last group, this may leads to error
        if (isHorizontalFirst) {
            if (isAddFromEnd)
                return currentGroup.startPos.y - currentGroup.curBioDirMax - padding.y >
                    contentStartPos.y - bound.height;
            else
                return firstGroup.startPos.y + padding.y < contentStartPos.y;
        }
        else {
            if (isAddFromEnd)
                return currentGroup.startPos.x + currentGroup.curBioDirMax + padding.x <
                    contentStartPos.x + bound.width;
            else 
                return firstGroup.startPos.x - padding.x > contentStartPos.x;
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
            if (newGroup.TryAddUnit(i, true)) {
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
            if (newGroup.TryAddUnit(i, false)) {
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
            //when append group to the head of grid, reset the start draw position of grid
            grid.SetStartPlacePos(newGroup.startPos);
            groups.Insert(0, newGroup);
        }
        //when append a group,reset the position of all siblings in new group immediately
        grid.RepositionChildsWithStartPos(newGroup.startPos, newGroup.startSibIndex, 
            newGroup.startSibIndex + newGroup.unitCount - 1);
    }

    Vector3 GetNewStartPosWhenAddGroup(MultiUnitGroup newGroup, bool isAddFromHead = false) {
        if (!hasGroups)
            return -content.localPosition;
        else {
            if (isAddFromHead) {
                //when try to add group to head, by calculate how many datas the new group can
                //contain, we get the startPos 
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

    #region steps for get start pos when add new group to head
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
        float lastRectDisplayLen = isHorizontalFirst ? rect.width : rect.height;
        lastRectDisplayLen = lastRectDisplayLen - (GetMaxFullyContainLen(full) - 
            (isHorizontalFirst ? bound.width : bound.height));
        while (true) {
            if (result - 1 < 0)
                return result;
            rect = GetTemplateRectByDataIndex(result - 1);
            lastRectDisplayLen -= isHorizontalFirst ? rect.width : rect.height;
            if (lastRectDisplayLen <= 0)
                return result;
            result--;
        }
    }

    float GetMaxFullyContainLen(int fullContainStartIndex = 0) {
        float result = 0;
        for (int i = fullContainStartIndex; i < startIndex; i++) {
            result += isHorizontalFirst ? GetTemplateRectByDataIndex(i).width :
                GetTemplateRectByDataIndex(i).height;
            if (i != startIndex - 1)
                result += isHorizontalFirst ? padding.x : padding.y;
        }
        return result;
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
            viewPortLen -= isHorizontalFirst ? padding.x : padding.y;
            result--;
        }
    }
    #endregion

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

    #region interfaces for convenience
    public Rect GetTemplateRectByDataIndex(int dataIndex) {
        return (GetTemplateObjByDataIndex(dataIndex).transform as RectTransform).rect;
    }

    public virtual GameObject GetNextDataTemplate() {
        return GetTemplateObjByDataIndex(endIndex + 1);
    }

    public virtual GameObject GetPreDataTemplate() {
        return GetTemplateObjByDataIndex(startIndex - 1);
    }

    public GameObject GetTemplateObjByDataIndex(int dataIndex) {
        if (dataIndex >= datas.Count || dataIndex < 0) return null;
        return GetTemplateObjectByChoiceIdx((datas[dataIndex] as IMultipleChoice).GetChoice());
    }

    public GameObject GetTemplateObjectByChoiceIdx(int choice) {
        return templatesObjects[choice];
    }

    public virtual GameObject CreateListItemByDataIndex(int index) {
        GameObject result = null;
        if (index < datas.Count && index >= 0) {
            result = InstantiateListItemObjectByChoiceIdx((datas[index] as IMultipleChoice).GetChoice());
            result.gameObject.AddComponent(itemViewType);
        }
        return result;
    }

    public virtual GameObject InstantiateListItemObjectByChoiceIdx(int idx = 0) {
        return Instantiate(templatesObjects[idx]);
    }

    public virtual void RemoveListItem(GameObject tempObj) {
        GameObject.Destroy(tempObj);
    }
    #endregion
}
