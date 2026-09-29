// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Grab;
using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.Emoting;
using Content.Goobstation.Shared.GrabIntent;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared.MartialArts.Events;
using Content.Shared.Weapons.Melee;
using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Audio;
using Content.Goobstation.Shared.Weapons.MeleeVulnerability;
using Content.Goobstation.Shared.Sprinting;
using Content.Shared.Interaction.Events;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Physics.Components;
using Content.Goobstation.Maths.FixedPoint; //omu
using Content.Shared.Clothing.Components; //omu
using Content.Shared.Damage; // omu
using Content.Shared.Damage.Components; // omu
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Content.Shared.EntityTable.EntitySelectors; // omu

namespace Content.Goobstation.Shared.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeShipbreaker()
    {
        SubscribeLocalEvent<CanPerformComboComponent, ShipbreakerGnashingTeethPerformedEvent>(OnShipbreakerGnashing);
        SubscribeLocalEvent<CanPerformComboComponent, ShipbreakerKneeHaulPerformedEvent>(OnShipbreakerKneeHaul);
        SubscribeLocalEvent<CanPerformComboComponent, ShipbreakerCrashingWavesPerformedEvent>(OnShipbreakerCrashingWaves);
        SubscribeLocalEvent<CanPerformComboComponent, ShipbreakerSacrificePerformedEvent>(OnShipbreakerSacrifice);

        SubscribeLocalEvent<GrantShipbreakerComponent, ClothingGotEquippedEvent>(OnGrantShipbreaker);
        SubscribeLocalEvent<GrantShipbreakerComponent, ClothingGotUnequippedEvent>(OnRemoveShipbreaker);
    }

    #region Generic Methods

    private void OnGrantShipbreaker(Entity<GrantShipbreakerComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!_netManager.IsServer)
            return;

        var user = args.Wearer;
        TryGrantMartialArt(user, ent.Comp);
    }
    private void OnRemoveShipbreaker(Entity<GrantShipbreakerComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        var user = args.Wearer;

        // Omu Station
        // Don't proceed if the user has non-removable Martial Arts knowledge
        if (HasManualCqcKnowledge(user))
            return;

        if (!TryComp<MartialArtsKnowledgeComponent>(user, out var martialArtsKnowledge))
            return;

        if (martialArtsKnowledge.MartialArtsForm != MartialArtsForms.Shipbreaker)
            return;

        RemComp<MartialArtsKnowledgeComponent>(user);
        RemComp<CanPerformComboComponent>(user);
    }

    #endregion
    #region Combo Methods

    private void OnShipbreakerGnashing(Entity<CanPerformComboComponent> ent,
        ref ShipbreakerGnashingTeethPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !_proto.TryIndex<MartialArtPrototype>(proto.MartialArtsForm.ToString(), out var martialArtProto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage + ent.Comp.ConsecutiveGnashes, out _);
        ent.Comp.ConsecutiveGnashes++;
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);
        if (!downed)
        {
            var saying =
                Enumerable.ElementAt(martialArtProto.RandomSayings, _random.Next(martialArtProto.RandomSayings.Count));
            var ev = new ShipbreakerSaying(saying);
            RaiseLocalEvent(ent, ev);
        }
        else
        {
            var saying =
                Enumerable.ElementAt(martialArtProto.RandomSayingsDowned, _random.Next(martialArtProto.RandomSayingsDowned.Count));
            var ev = new ShipbreakerSaying(saying);
            RaiseLocalEvent(ent, ev);
        }
        ent.Comp.LastAttacks.Clear();
    }

    private void OnShipbreakerKneeHaul(Entity<CanPerformComboComponent> ent,
        ref ShipbreakerKneeHaulPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        if (!downed)
        {
            DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
            _stamina.TakeStaminaDamage(target, proto.StaminaDamage);
            _stun.TryKnockdown(target, proto.ParalyzeTime, true, true, proto.DropItems);
        }
        else
        {
            DoDamage(ent, target, proto.DamageType, proto.ExtraDamage / 2, out _);
            _stamina.TakeStaminaDamage(target, proto.StaminaDamage - 20);
            _hands.TryDrop(target);
        }
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnShipbreakerCrashingWaves(Entity<CanPerformComboComponent> ent,
        ref ShipbreakerCrashingWavesPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || downed)
            return;

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out var damage);
        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;
        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _grabThrowing.Throw(target, ent, dir, proto.ThrownSpeed, damage, proto.DropItems);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit2.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }
    private void OnShipbreakerSacrifice(Entity<CanPerformComboComponent> ent,
        ref ShipbreakerSacrificePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _)
            || target != ent.Owner)
            return;
        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);
        ApplyMultiplier(ent, 1.2f, 0f, TimeSpan.FromSeconds(10), MartialArtModifierType.MoveSpeed);
        _modifier.RefreshMovementSpeedModifiers(ent);
        ApplyMultiplier(ent, 1.2f, 0f, TimeSpan.FromSeconds(10));
        ent.Comp.LastAttacks.Clear();
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit1.ogg"), target);

    }
}

    #endregion