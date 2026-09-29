// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Common.Grab;
using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.GrabIntent;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared.MartialArts.Events;
using Content.Shared.Weapons.Melee;
using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Movement.Pulling.Components;
using Robust.Shared.Audio;

namespace Content.Goobstation.Shared.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeShipbreaker()
    {
        SubscribeLocalEvent<CanPerformComboComponent, ShipbreakerGnashingTeethPerformedEvent>(OnShipbreakerGnashing);
        SubscribeLocalEvent<CanPerformComboComponent, ShipbreakerKneeHaulPerformedEvent>(OnShipbreakerKneeHaul);
        SubscribeLocalEvent<CanPerformComboComponent, ShipbreakerCrashingWavesPerformedEvent>(OnShipbreakerCrashingWaves);

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
        if (!TryComp<MartialArtsKnowledgeComponent>(user, out var martialArtsKnowledge))
            return;

        if (martialArtsKnowledge.MartialArtsForm != MartialArtsForms.Shipbreaker)
            return;

        if (!TryComp<MeleeWeaponComponent>(args.Wearer, out var meleeWeaponComponent))
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

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage + ent.Comp.ConsecutiveGnashes * 5, out _);
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
}

    #endregion