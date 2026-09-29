// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Shared.MartialArts.Events;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShipbreakerGnashingTeethPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShipbreakerKneeHaulPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShipbreakerCrashingWavesPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShipbreakerSacrificePerformedEvent : EntityEventArgs;

[Serializable,NetSerializable]
public sealed class ShipbreakerSaying(LocId saying) : EntityEventArgs
{
    public LocId Saying = saying;
}