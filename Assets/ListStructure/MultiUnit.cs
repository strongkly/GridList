using UnityEngine;

public class MultiUnit {
    public GameObject gameObject {
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
