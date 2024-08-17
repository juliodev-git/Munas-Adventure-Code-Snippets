using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/*
 * 
 * Item Controller Main Role:   in addition to the functionality of a regular panel, itemController also uses input to:
 *                              move items between valid slot locations across all open slots in the active panel
 *                              also has access to the currently selectable UI object, checking for a SlotController script to verify if it is in fact a slot
 *                              Uses access to SlotPanelControllers (bag and inventory) for swapping between both slotpanelcontrollers (essentially swapping an item from bag to stash instantly)
 *                              also has access to 'ledger' - the underlying data that stores user's bag and stash items
 *                              uses a delegate to fire off an event so all slotPanels (ie bag and stash) update the ledger
 *                              
 */
public class ItemController : PanelController
{

    //an extension of PanelController, using input to dictate when to manipulate item slots
    private GameObject selected;

    [SerializeField]
    private bool placeItem;

    [SerializeField]
    protected GameObject heldItem;

    [SerializeField]
    protected GameObject itemText;

    private List<Button> uiButtons;

    [SerializeField]
    protected SlotPanelController bagPanel;

    [SerializeField]
    protected SlotPanelController inventoryPanel;

    [SerializeField]
    protected SlotController heldSlot;

    [SerializeField]
    protected SlotController slot;

    [SerializeField]
    protected TextMeshProUGUI usePanel;

    [SerializeField]
    protected TextMeshProUGUI itemDescription;

    private Item slotItem;

    //private string consumeText = "CONSUME [X/\u25A1]";
    //private string dropText = "DROP [X/\u25A1]";
    private string baseText  = "      [X/\u25A1]";
    private string splitText = "SPLIT [X/\u25A1]";

    protected Color greyed = new Color(1, 1, 1, 0.25f);

    public delegate void OnItemPlace();
    public OnItemPlace onItemPlace;

    protected override void Awake()
    {
        base.Awake(); //just initializes playerControls (player input class)

        uiButtons = new List<Button>();

        //automatically get panelButtons (like return, accept, apply etc.)
        foreach (Button button in GetComponentsInChildren<Button>())
        {
            if (button.CompareTag("PanelButton"))
            {
                uiButtons.Add(button);
            }
        }

        //automatically grab the HeldItemUI thing too
        //heldItem = this.transform.Find("HeldItem").gameObject;

        if (!heldItem)
            Debug.LogError("MISSING HELDITEM UI PREFAB FOR ITEM CONTROLLER");

        heldSlot = null;

        //ToggleHeldItem(null);
        ChangeItemDescription(null);

        //onItemPlace += bagPanel

        onItemPlace += bagPanel.UpdateDataSource;
        onItemPlace += inventoryPanel.UpdateDataSource;
    }

    protected override void OnEnable()
    {

        base.OnEnable(); //selects the first button upon opening panel

        //heldItem.SetActive(false);
    }

    private void Start()
    {
        //uiButtons = new List<Button>();

        ////automatically get panelButtons (like return, accept, apply etc.)
        //foreach (Button button in GetComponentsInChildren<Button>())
        //{
        //    if (button.CompareTag("PanelButton"))
        //    {
        //        uiButtons.Add(button);
        //    }
        //}

        ////automatically grab the HeldItemUI thing too
        ////heldItem = this.transform.Find("HeldItem").gameObject;

        //if (!heldItem)
        //    Debug.LogError("MISSING HELDITEM UI PREFAB FOR ITEM CONTROLLER");

        //heldSlot = null;

        //ToggleHeldItem(heldSlot);
        //heldItem.SetActive(fa);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        //if Menu get's closed while holding an item, put the item back
        RestoreSlot();
    }

    protected override void Update()
    {
        base.Update();

        //TRACK SELECTABLE
        selected = GameController.instance.GetSelectedGameObject();

        if (usePanel) {
            usePanel.text = baseText;
            usePanel.color = greyed;
        }

        //IF ON SELECTABLE, STORE IT'S SLOTCONTROLLER (if it exists)
        if (selected)
        {
            slot = selected.GetComponent<SlotController>();


            if (slot)
            {
                slotItem = slot.GetItem();
                itemText.SetActive(!heldItem.activeInHierarchy && slotItem);
            }
            else
                itemText.SetActive(false);
            //heldItem has to be OFF
            //slot has to have an item...

            if (slotItem && !heldSlot)
            {
                itemText.GetComponent<ItemTextController>().ChangeText(slotItem.name);
                ChangeItemDescription(slotItem);

                if (usePanel)
                {

                    if (slotItem.GetItemType() == ItemType.ammo)
                    {
                        usePanel.text = splitText;

                        if (slotItem.GetItemCount() > 1)
                            usePanel.color = Color.white;

                    }

                }
            }
            else {
                //On a slotcontroller but no item
                ChangeItemDescription(null);
            }

        }
        else {
            slot = null;

            itemText.SetActive(false);
            ChangeItemDescription(null);
            //heldSlot = null;
            //ToggleHeldItem(heldSlot);
            //return;
        }
            

        #region Input-Priority
        //PRESS
        if (playerControls.UI.Submit.WasPressedThisFrame())
        {
            //pressing submit only important when over a slot
            if (slot && !heldSlot)
            {
                //grabs a reference to that slot, simaltaneously 'disabling' that slot (slot retains item information, though)
                heldSlot = slot.GrabSlot();

                //toggle HeldItemTracker
                ToggleHeldItem(heldSlot);
            }

        }
        //RELEASE
        else if (playerControls.UI.Submit.WasReleasedThisFrame()) //HERE: if the slotpanelcontroller is !container, then placing the item anywhere but it's origin will disable that slot...
        {
            //by default, item is not placed
            placeItem = false;

            #region OldPlaceLogic

            ////only over valid location is it worth attempting a placement
            //if (slot && heldSlot)
            //    placeItem = slot.PlaceItem(heldSlot.GetItem()); //heldSlot item gets passed in, and subseqently saved to ledger
            //else
            //    RestoreSlot();

            //if (!placeItem)
            //    RestoreSlot(); //only restore on a failed placement. turns enables the heldSlot, nothing changes to its item
            //else {
            //    //gets called when placing on same slot, have to like... reconcile this

            //    //AND NOW CAN UPDATE LEDGER...
            //    if (!slot.Equals(heldSlot))
            //    {
            //        if (heldSlot.panel.CompareTag("Inventory"))
            //        {
            //            //grabbed from inventory
            //            if (!inventoryPanel.IsContainer()) //CHECKS inventorypanel and lol this dictates the disabling of the bag haha
            //                heldSlot.DisableSlot();
            //            else
            //                heldSlot.ResetSlot();
            //        }
            //        else
            //            heldSlot.ResetSlot();

            //    }
            //    else
            //        heldSlot.ResetSlot();

            //}
            #endregion OldPlaceLogic

            if (slot && heldSlot)
            {
                if (slot.Equals(heldSlot))
                {
                    //placed onto same spot, restore and move on
                    RestoreSlot();
                }
                else {

                    placeItem = slot.PlaceItem(heldSlot.GetItem());

                    

                    if (!placeItem)
                        heldSlot.RestoreSlot();

                    //always refresh the currentslot
                    slot.RefreshSlot();
                    heldSlot.RefreshSlot();
                }
            }
            else {
                //released button but not over valid slot and not holding item
                //in case an item was held, restore it.
                Debug.Log("slot restored");
                RestoreSlot();
            }

            //NULLIFY, prep for next grab
            heldSlot = null;

            ToggleHeldItem(heldSlot);

            onItemPlace?.Invoke();

            //Invoke some delegate like UpdateList/Ledger
            //Placing an item, being in or out of your bag, should update the bag...
            //Or even, the bag slot itself should be listening per placement right?
            //The difference for pre-raid and mid-raid is that pre-raid updates ledger whereas mid-raid it updates the game list...

        }
        //SWAP/Y-button
        else if (playerControls.UI.Swap.WasPressedThisFrame())
        {
            //swap ONLY when on a valid slot AND not holding something
            if (slot && !heldSlot) {

                //try to place this slot over to the opposing panel
                SlotPanelController slotPanel = null;

                if (slot.panel.CompareTag("Bag"))
                    slotPanel = inventoryPanel;

                if (slot.panel.CompareTag("Inventory"))
                    slotPanel = bagPanel;

                if (slotPanel) {
                    //successful swap

                    if (slotItem) {
                        if (slotPanel.PlaceAtNextAvailableSlot(slotItem))
                        {

                            slot.RefreshSlot();

                            onItemPlace?.Invoke();
                        }
                        else
                        {
                            //failed placement, restore the slot you're holding
                            //so that it displays the original item
                            slot.RestoreSlot();
                        }
                    }

                    
                }

            }
        }
        else if (playerControls.UI.Cancel.WasPressedThisFrame()) {
            //note that the parent class, upon pressing cancel, will have selected the back button....
            //so in this case, might be easier to simply restore the 

            RestoreSlot(); //turn back on original button
            //release held button
            heldSlot = null;
            ToggleHeldItem(heldSlot);
        }
        else if (playerControls.UI.Consume.WasPressedThisFrame() && !heldSlot && !playerControls.UI.Submit.IsPressed())
        {
            //no matter the location, you always have the option of splitting a stack
            //ideally, ONLY NON-CONSUMABLES ARE STACKS so you'll never try to consume AND split
            if (slotItem && (slotItem.GetItemCount() > 1) && (slotItem.GetItemType() == ItemType.ammo)) {

                //we have to create a singular item on next available slot
                //but it stays local...
                SlotPanelController slotPanel = null;
                SlotPanelController otherPanel = null;

                if (slot.panel.CompareTag("Bag")) {
                    slotPanel = bagPanel;
                    otherPanel = inventoryPanel;
                }


                if (slot.panel.CompareTag("Inventory")) {
                    slotPanel = inventoryPanel;
                    otherPanel = bagPanel;
                }
                   

                if (slotPanel.PlaceAtNextFreeSlot(slotItem.GetSourceItem()))
                {
                    //consume over a stack basically
                    slotItem.DecreaseItemCount();
                    slot.RefreshSlot();
                }
                else {
                    if (otherPanel.PlaceAtNextFreeSlot(slotItem.GetSourceItem())) {

                        slotItem.DecreaseItemCount();
                        slot.RefreshSlot();
                        
                    }
                }
                
            }
            
        }
        else
        {
            //nothing pressed, possible in the middle of press and release
            if (playerControls.UI.SnapLeft.WasPressedThisFrame())
            {
                bagPanel.SelectFirstSlot();
                return;
            }

            if (playerControls.UI.SnapRight.WasPressedThisFrame())
            {
                inventoryPanel.SelectFirstSlot();
                return;
            }

        }
        #endregion Input-Priority

    }

    protected void ToggleHeldItem(SlotController slot) {

        if (slot) {
            Item item = slot.GetItem();

            HeldItemController hic = heldItem.GetComponent<HeldItemController>();

            //if you're holding an item, set it's image
            if (item)
            {
                heldItem.GetComponent<Image>().sprite = item.itemSprite; //set sprite image

                //set it's count too
                if (hic)
                {
                    if (item.GetItemCount() > 1)
                    {
                        hic.SetItemCount(item.GetItemCount());
                    }
                    else
                        hic.ResetCount();
                }
            }
            else {
                if (hic) {
                    hic.ResetCount();
                }
            }
                
        }

        heldItem.SetActive(slot != null);
        UIButtonToggle(slot == null); //if heldItem ON, UIButtons OFF

    }

    private void RestoreSlot() {
        if (heldSlot)
            heldSlot.RestoreSlot();       
    }

    private void UIButtonToggle(bool t) {
        foreach (Button button in uiButtons) {
            button.interactable = t;
        }
    }

    private void ChangeItemDescription(Item item) {

        

        if (itemDescription) {
            
            if (item == null) {
                itemDescription.text = "";
                return;
            }

            //check if item has effects
            //maybe all items can have a description
            itemDescription.text = item.name + "\n"
                                    + item.GetDescription();
        }
    }
}
