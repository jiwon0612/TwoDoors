using System.Collections.Generic;
using TwoDoors.Items;
using UnityEngine;

namespace TwoDoors.Events
{
    [CreateAssetMenu(menuName = "TwoDoors/Events/Item Trade")]
    public class ItemTradeEvent : InstantEvent
    {
        [SerializeField] ItemKind payment = ItemKind.Medkit;
        [SerializeField, Min(1)] int price = 1;
        [SerializeField, Min(1)] int missingItemDamage = 35;

        public override void Begin(IRoomContext ctx)
        {
            if (!ctx.SpendItem(payment, price)) ctx.ChangeHealth(-missingItemDamage);
            if (ctx.IsPlayerAlive) base.Begin(ctx);
        }

        protected override void AppendConditionDescription(List<string> lines)
        {
            lines.Add($"{ItemName(payment)} {price}개 지불");
            lines.Add($"부족하면 대신 체력 {missingItemDamage} 감소");
            base.AppendConditionDescription(lines);
        }
    }
}
