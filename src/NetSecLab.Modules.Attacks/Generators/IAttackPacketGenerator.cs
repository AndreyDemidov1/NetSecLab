using NetSecLab.Core.Models;

namespace NetSecLab.Modules.Attacks.Generators;

internal interface IAttackPacketGenerator
{
    AttackType AttackType { get; }
    LogicalPacket CreatePacket(AttackRunOptions options, Random random);
}
