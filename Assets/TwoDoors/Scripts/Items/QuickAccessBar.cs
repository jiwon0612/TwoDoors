using TMPro;
using TwoDoors.Characters;
using UnityEngine;

namespace TwoDoors.Items
{
    public sealed class QuickAccessBar : MonoBehaviour
    {
        public PlayerCharacter player;
        public ItemIcon[] icons;
        public UnityEngine.UI.Image[] cards;
        public UnityEngine.UI.Image[] accents;
        public TMP_Text[] counts;
        public static readonly ItemKind[] Kinds = { ItemKind.Knife,ItemKind.Pistol,ItemKind.MachineGun,ItemKind.Shotgun,ItemKind.Machete,ItemKind.Medkit,ItemKind.Flashlight };
        public static readonly string[] Keys = { "1","2","3","4","5","E","F" };
        void Update() => Refresh();
        public void Refresh()
        {
            if (!player || !player.Inventory) return;
            for(int i=0;i<Kinds.Length;i++)
            {
                int count=player.Inventory.Count(Kinds[i]); bool owned=count>0;
                bool selected=owned && (PlayerInventory.IsWeapon(Kinds[i]) ? player.EquippedWeapon==Kinds[i] : Kinds[i]==ItemKind.Flashlight && player.FlashlightOn);
                icons[i].color=owned ? new Color(.85f,.94f,.96f) : new Color(.21f,.26f,.3f);
                cards[i].color=selected ? new Color(.06f,.25f,.27f,.98f) : new Color(.035f,.05f,.075f,.94f);
                accents[i].color=selected ? new Color(.3f,.95f,.8f) : Color.clear;
                counts[i].text=Kinds[i]==ItemKind.Medkit ? "x"+count : Kinds[i]==ItemKind.Flashlight ? (selected ? "ON" : "OFF") : "";
                counts[i].color=owned ? new Color(.6f,.85f,.85f) : new Color(.3f,.35f,.4f);
            }
        }
    }
}
