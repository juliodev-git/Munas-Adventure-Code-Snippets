using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "InventoryScriptableObject", menuName = "ScriptableObjects/InventoryScriptableObject")]
public class InventoryScriptableObject : ScriptableObject
{

    //public List<ItemPair> items;

    //ALERT: LITERALLY HOLDS UR BAG DATA
    //TextAreaAttribute(int minLines, int maxLines);
    [TextArea(15,20)]
    [SerializeField]
    private string savedInventoryData;

    public delegate void UpdateSource();
    public UpdateSource onUpdateSource;

    //Each instance of the inventoryscriptableObject has a local inventory class for processing
    //Gets overwritten often
    protected InventoryClass ic;

    public void OnValidate()
    {
        //lessgo
        //LoadItemsList();
        
    }

    /*
     * Stores an inventoryclass to memory as a JSON, updates visualizer data as well
     */
    public virtual void SetItemsList(InventoryClass ic) {

        //i guess we have to idk save the data cuz it keeps getting overwritten

        //ItemPair[] itemPairArray = new ItemPair[ic.inventory.Count];

        //ic.inventory.CopyTo(itemPairArray);

        //if (items == null) {
        //    items = new List<ItemPair>(ic.inventory.Count);
        //}

        //items.Clear();

        //foreach (ItemPair itemPair in itemPairArray) {
        //    items.Add(itemPair);
        //}

        //ToJSON > makes a string
        
        PlayerPrefs.SetString(this.name, JsonUtility.ToJson(ic, true));
        savedInventoryData = PlayerPrefs.GetString(this.name);
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        //onUpdateSource?.Invoke();
    }

    public string GetSavedBagData() {
        return PlayerPrefs.GetString(this.name);
    }

    public InventoryClass GetInventoryList() {

        InventoryClass ic = JsonUtility.FromJson<InventoryClass>(PlayerPrefs.GetString(this.name));

        //if nothing is saved, load the pre-made list
        //actually, for final release, it should essentially load an empty list
        //this could be creating the bunk stuff
        if (ic != null) {
            //saved info, load it
            return ic;
        }
        else {
            //nothing saved, load nothing really...
            //ic = new InventoryClass();
            //return JsonUtility.FromJson<InventoryClass>(PlayerPrefs.GetString(this.name));
            return new InventoryClass();
        }
            
    }

    /*
     * Debug Function: Loads editor values into inventory data - essentially lets you add items to your bag
     */
    private void LoadItemsList() {

        //InventoryClass ic;

        //if (items != null)
        //{
        //    ic = new InventoryClass(items);
        //}
        //else {
        //    ic = new InventoryClass();
        //}

        //SetItemsList(ic);

    }

    public bool AddItemToList(Item item) {
        ic = GetInventoryList();

        for (int i = 0; i < ic.inventory.Count; i++) {

            ItemPair currentPair = ic.inventory[i];

            //item slot is free
            if (currentPair.Key == null)
            {
                //multi-item requires this slot have a definition
                if ((item.GetItemCount() > 1) && (currentPair.slotDefinition == null))
                {
                    continue;
                }

                ItemPair newItem = new ItemPair(item.GetSourceItem(), item.GetItemCount());

                //item slot has definition
                if (currentPair.slotDefinition)
                {
                    //if main type doesn't match, return false
                    if (currentPair.slotDefinition.GetItemType() != item.GetSourceItem().GetItemType())
                        continue;
                    else
                    {
                        if ((currentPair.slotDefinition.GetSubType() > 0) && (currentPair.slotDefinition.GetSubType() != item.GetSourceItem().GetSubType()))
                        {
                            continue;
                        }
                    }

                    newItem.SetDefinition(currentPair.slotDefinition);
                }

                ic.inventory[i] = newItem;
                SetItemsList(ic);
                return true;
            }
            else {
                //something is here, and the slot is stackable
                //NOTE: Limitation of adding items to bag
                //Can only add an item to stack if there is exactly enough room
                //Technically, an additional item should be spawned containing the remainder of the ammo but we'll cross that bridge later
                if (currentPair.slotDefinition) {
                    if ((item.GetItemCount() + currentPair.Value) <= currentPair.slotDefinition.GetMaxItemCount()) {
                        currentPair.Value += item.GetItemCount();
                        ic.inventory[i] = currentPair;
                        SetItemsList(ic);
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public virtual void ClearItemList() {
        //clear all items and values
        //but don't mess with the slotDefinitions
        InventoryClass ic = JsonUtility.FromJson<InventoryClass>(PlayerPrefs.GetString(this.name));

        foreach (ItemPair ip in ic.inventory) {
            ip.Key = null;
            ip.Value = 0;
        }

        SetItemsList(ic);
    }

    public bool ContainsItem(Item item) { 
        ic = JsonUtility.FromJson<InventoryClass>(savedInventoryData); //get item list has a chance of loading editor data, so let's just load raw

        if (ic != null) {
            foreach (ItemPair itemPair in ic.inventory) {
                if (itemPair.Key && (itemPair.Key == item)) {
                    return true;
                }
            }
        }

        return false;
    }

    private void LoadInventoryClass() { 
    
    }
}
