// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared.MartialArts.Events;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShipbreakerGnashingTeethPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShipbreakerKneeHaulPerformedEvent : EntityEventArgs;

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ShipbreakerCrashingWavesPerformedEvent : EntityEventArgs;

[Serializable,NetSerializable]
public sealed class ShipbreakerSaying(LocId saying) : EntityEventArgs
{
    public LocId Saying = saying;
};