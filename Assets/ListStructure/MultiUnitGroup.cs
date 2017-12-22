using UnityEngine;
using System.Collections.Generic;

public class MultiUnitGroup {
    List<MultiUnit> units = new List<MultiUnit>();

    MultiList multiList;

    #region interfaces for convenience
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

    public RectTransform lastUnitTrans {
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

    #region for optimistic
    public bool needReposition {
        private set;
        get;
    }
    #endregion

    static int groupIdCur = 0;
    public int groupid;

    public float curBioDirMax {
        private set;
        get;
    }

    //the position of first unit in group drawing on screen 
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
        groupIdCur++;
        groupid = groupIdCur;
    }

    public void SetNewGroupStartPos(Vector3 startPos) {
        this.startPos = startPos;
    }

    public void SetStartSiblingIndex(int index) {
        this.startSibIndex = index;
        //Debug.LogError(string.Format("group{0}'s sibling index set to {1}", groupid, index));
    }

    public int TryAppendWhenDrag(Vector2 dragDis) {
        int offset = 0;
        if (IsLeftDrag(dragDis) || IsTopDrag(dragDis)) {
            //may need append unit to end, display next datas
            offset = TryAppendUnit() ? 1 : 0;
        }
        else {
            //may need append unit to head, display previous datas
            offset = TryAppendUnit(false) ? -1 : 0;
        }
        //lastDragDis.Set(dragDis.x, dragDis.y);
        return offset;
    }

    public int TryDeleteWhenDrag(Vector2 dragDis) {
        int offset = 0;
        //when deleting unit, strict to the principle that deleting 
        //only starts from either side of datas
        if (IsLeftDrag(dragDis) || IsTopDrag(dragDis)) {
            if (units[0].dataIndex == multiList.startIndex)
                offset = TryRemoveUnit() ? 1 : 0;
        }
        else {
            if (units[units.Count - 1].dataIndex == multiList.endIndex)
                offset = TryRemoveUnit(false) ? -1 : 0;
        }
        return offset;
    }

    bool IsLeftDrag(Vector2 dragDis) {
        return dragDis.x - lastDragDis.x > 0;
    }

    bool IsTopDrag(Vector2 dragDis) {
        return dragDis.y - lastDragDis.y < 0;
    }

    bool TryAppendUnit(bool isAddFromEnd = true) {
        GameObject obj = isAddFromEnd ? 
            multiList.GetNextDataTemplate() : multiList.GetPreDataTemplate();
        if (obj == null) return false;
        RectTransform trans = obj.transform as RectTransform;
        ///check if current group is able to append the unit
        if (IsNewTransBioBiggerThanMax(trans))
            return false;
        
        return TryAddUnit(isAddFromEnd ? multiList.endIndex + 1 :
            multiList.startIndex - 1, isAddFromEnd);
    }

    bool TryRemoveUnit(bool isDeleteFromHead = true) {
        MultiUnit unit = isDeleteFromHead ? units[0] : units[units.Count - 1];
        if (IsUnitVisible(unit))
            return false;
        DestroyAndRemoveUnit(unit, isDeleteFromHead);
        return true;
    }

    public bool TryAddUnit(int dataIndex, bool isAddFromEnd = true) {
        if (!IsNewUnitVisible(isAddFromEnd))
            return false;
        FillItemView(CreateAndAddUnit(GetItem(dataIndex),isAddFromEnd), dataIndex);
        return true;
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

    /// determine if newly added unit is visiable 
    bool IsNewUnitVisible(bool isAddFromEnd = true) {
        //first item set to visible
        if (!hasUnits) return true;
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
        //Debug.LogError(string.Format("add unitname:{0} ::: startPos:{1}", unit.gameObject.name, startPos));
    }

    void DestroyAndRemoveUnit(MultiUnit unit, bool isRemoveFromHead = true) {
        units.Remove(unit);

        if (isRemoveFromHead)
            UpdateStartPosWhenRemoveUnitFromHead(unit);

        StoreItem(unit.gameObject, (multiList.datas[unit.dataIndex]
           as IMultipleChoice).GetChoice());
    }

    void UpdateStartPosWhenRemoveUnitFromHead(MultiUnit unit) {
        if (isHorizontalFirst)
            startPosition.x = startPosition.x + unit.width + padding.x;
        else
            startPosition.y = startPosition.y - unit.height - padding.y;
        //Debug.LogError(string.Format("rem unitname:{0} ::: startPos:{1}", unit.gameObject.name, startPos));
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

        obj.name = string.Format("groupid:{0}siblingindex:{1}start:{2}end:{3}",groupid, "000", startSibIndex, startSibIndex + units.Count);
    }

    void FillItemView(MultiUnit unit, int dataIndex) {
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

    public void SetGroupNeedReposition() {
        this.needReposition = true;
    }

    public void RepositionAllUnit() {
        if (needReposition)
            multiList.grid.RepositionChildsWithStartPos(startPos, startSibIndex,
               startSibIndex + unitCount - 1);

        needReposition = false;
        //Debug.LogError(string.Format("groupid:{0},startSibIndex:{1},endSibIndex:{2}", groupid, startSibIndex, startSibIndex + unitCount - 1));
    }

    public GameObject GetItem(int dataIndex) {
        return multiList.poolAdapter.GetObject(
            multiList.GetChoiceIndexByDataIndex(dataIndex));
    }

    public bool StoreItem(GameObject obj, int choice) {
        return multiList.poolAdapter.StoreObject(obj, choice);
    }
}
