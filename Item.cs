using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ItemType { misc, consumable, throwable, cosmetic, ammo}

//[CreateAssetMenu(fileName = "ItemScriptableObject", menuName = "ScriptableObjects/Item")]
public class Item : ScriptableObject
{
    public Sprite itemSprite;
    public int index;

    private Item sourceItem;

    [SerializeField]
    protected int itemCount;

    [TextArea(1, 3)]
    [SerializeField]
    public string description;

    public virtual ItemType GetItemType() {
        return 0;
    }

    public virtual int GetSubType() {
        return 0;
    }

    #region GettersSetters
    public virtual int GetItemCount() { return itemCount; }

    public virtual void SetItemCount(int i) { itemCount = i; }

    public virtual void IncreaseItemCount(){

        itemCount ++;
        itemCount = Mathf.Clamp(itemCount, 0, 1);

    }

    public virtual void IncreaseItemCount(int increase) {

        itemCount += increase;
        itemCount = Mathf.Clamp(itemCount, 0, 1);

    }

    public virtual void DecreaseItemCount() { 
        
        itemCount--;
        itemCount = Mathf.Clamp(itemCount, 0, 1);
    
    }

    public virtual void DecreaseItemCount(int decrease) {

        itemCount -= decrease;
        itemCount = Mathf.Clamp(itemCount, 0, 1);

    }

    public virtual int GetMaxItemCount() { return 1; }

    public virtual bool IsStackable() { return false; }

    public virtual GameObject GetPrefab() { return null; }

    public virtual Item GetSourceItem() { return sourceItem; }

    public virtual void SetSourceItem(Item source) { sourceItem = source; }

    public virtual string GetDescription() { return description; }

    public void CopyTo(Item item) {
        //take the main info from this item and give to the item passed in
        item.itemSprite = this.itemSprite;
        item.index = this.index;

        item.SetSourceItem(this.GetSourceItem());
        item.SetItemCount(this.GetItemCount());
    }
    #endregion GettersSetters
}
